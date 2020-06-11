using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using PickleTrick.Core.Crypto;
using PickleTrick.Core.Server.Events;
using Tomlyn;
using System.IO;
using Tomlyn.Model;
using Serilog;
using PickleTrick.Core.Server.Attributes;
using System.Linq;
using System.Collections.Generic;
using PickleTrick.Core.Server.Interfaces;
using System.Buffers.Binary;
using Nito.AsyncEx;
using PickleTrick.Core.Server.Data;
using System.Text;
using PickleTrick.Core.Server.Exceptions;

namespace PickleTrick.Core.Server
{
    public abstract class ServerApp
    {
        /// <summary>
        /// An abstract method to implement in order to let server apps
        /// initialize server-specific things, like attaching to the OnPacket event.
        /// </summary>
        public abstract void PrivateInit();
        public abstract void PrivateConfigure();
        public abstract void Preconfigure();
        public abstract string GetServerName();

        protected int port = -1;
        protected Dictionary<ushort, IPacketHandler> serverPackets = new Dictionary<ushort, IPacketHandler>();

        public ServerConfiguration Configuration = new ServerConfiguration();
        public event OnPacketDelegate OnPacket;

        public event OnConnectDelegate OnConnect;
        public event OnDisconnectDelegate OnDisconnect;

        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

        /// <summary>
        /// Non-explicit ServerApp constuctor
        /// Assume port will be determined and set from PrivateConfigure.
        /// </summary>
        public ServerApp()
        {
            Configure();
            PrivateConfigure();
            if (port == -1)
                throw new Exception("Port was not set with the non-explicit ServerApp constructor.");
            Initialize();
        }

        /// <summary>
        /// Non-explicit ServerApp constuctor
        /// Assume port will be a constant.
        /// </summary>
        public ServerApp(int port)
        {
            Configure();
            PrivateConfigure();
            this.port = port;
            Initialize();
        }

        /// <summary>
        /// The pre-read packet hook. When overridden, the server can determine whether to exit early or not.
        /// In other words, if the overridden hook returns true, it means the hook has handled the packet.
        /// From there, you can decide to close the socket (<code>client.Socket.Close()</code>), or just keep processing.
        /// 
        /// In general, you should return true if the packet is handled by the hook, and false if not.
        /// If you don't close the socket, the server will keep processing the packet received.
        /// </summary>
        /// <param name="client">Client state</param>
        /// <param name="bytesRead">Amount of bytes we can read at most</param>
        /// <returns>true if the hook handles the packet</returns>
        public virtual bool PreReadPacketHook(Client client, int bytesRead)
        {
            // Pass the packet on.
            return false;
        }

        /// <summary>
        /// We will configure the database here.
        /// Server apps will get to configure themselves in PrivateConfig.
        /// </summary>
        public void Configure()
        {
            // Set up the logger first.
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.log", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Console.Title = GetServerName();
            Log.Information(GetServerName()
                + Environment.NewLine
                + new string('-', GetServerName().Length + 24));

            var db = Toml.Parse(File.ReadAllText("db.toml")).ToModel();
            var table = (TomlTable)db["db"];
            Configuration.Database.Host = (string)table["host"];
            Configuration.Database.Database = (string)table["database"];
            Configuration.Database.Username = (string)table["username"];
            Configuration.Database.Password = (string)table["password"];

            // We can initialize the database first in Preconfigure if needed.
            Preconfigure();

            if (!Database.Setup(DatabaseType.MySql, Configuration.Database))
            {
                // We've failed and already sent the stack trace to the logs.
                // We can just wait for a keypress and exit.
                Console.ReadKey();
                Environment.Exit(1);
            }

            PopulatePacketHandlers();
        }

        private void PopulatePacketHandlers()
        {
            var handlers = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(t => t.GetTypes())
                        .Where(i => typeof(IPacketHandler).IsAssignableFrom(i))
                        .ToArray();

            foreach (var handler in handlers)
            {
                foreach (var attr in handler.GetCustomAttributes(true))
                {
                    if (attr is HandlesPacket)
                    {
                        serverPackets.Add(((HandlesPacket)attr).opcode, (IPacketHandler)Activator.CreateInstance(handler));
                        break;
                    }
                }
            }

            Log.Information("Handled opcodes: {0}", string.Join(", ", serverPackets.Keys.Select(x => "0x" + x.ToString("X2"))));
        }

        private void HandlePacket(Client client, byte[] packet)
        {
            var opcode = BinaryPrimitives.ReadUInt16LittleEndian(packet[2..4]);
            if (serverPackets.ContainsKey(opcode))
            {
                AsyncContext.Run(() => serverPackets[opcode].Handle(client, packet[9..]));
            }
            OnPacket(client, packet);
        }

        /// <summary>
        /// Beginning listening.
        /// </summary>
        private void Initialize()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(100);

                Log.Information("Listening for clients on port {0}.", port);

                PrivateInit();

                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        listener);
                    allDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                Log.Error(e.ToString());

                if (System.Diagnostics.Debugger.IsAttached)
                    throw e;
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            allDone.Set();

            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the client and determine the buffer to use.
            Client client = new Client
            {
                Socket = handler
            };

            OnConnect(client);

            Log.Information("New connection received from {0}.", handler.RemoteEndPoint);

            try
            {
                handler.BeginReceive(client.CurrentBuffer, 0, client.CurrentBuffer.Length, 0,
                    new AsyncCallback(ReadCallback), client);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred during BeginReceive. Attempting to close socket for {0}.", handler.RemoteEndPoint);
                try
                {
                    client.DestroyBuffers();
                    handler.Close();
                }
                catch { }
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the client and the socket from the asynchronous state.
            Client client = (Client)ar.AsyncState;
            Socket handler = client.Socket;

            // Read data from the socket.
            int bytesRead = 0;

            try
            {
                bytesRead = handler.EndReceive(ar);
            }
            catch (Exception ex) when (
                // The user probably forcefully disconnected.
                ex is SocketException || ex is ObjectDisposedException
            )
            {
                // Try to close the socket but don't freak out if it fails.
                try
                {
                    OnDisconnect(client);
                    handler.Close();
                }
                catch { }

                // Don't try to read any more data. We're out of here.
                return;
            }

            if (!handler.Connected || bytesRead == 0)
            {
                // Connection is terminated, either by force or willingly
                OnDisconnect(client);
                try
                {
                    handler.Close();
                }
                catch { }
                return;
            }

            if (bytesRead > 0)
            {
                try
                {
                    if (PreReadPacketHook(client, bytesRead))
                    {
                        if (!client.Socket.Connected)
                            return;
                    }
                }
                catch { }

                /*
                 * Trickster has two types of special packets, which I call the following:
                 * 1. Merged packets, where multiple packets are sent as one packet.
                 * 2. Split packets, likely an artifact of it using TCP for communication.
                 * 
                 * For merged packets, we have to determine the end of a packet and start reading the next
                 * packet whenever we finish a packet. We do this by checking if there's still data to read.
                 * 
                 * For split packets we have to keep a ~64kb packet buffer for the server to store data in.
                 * Whenever we get a new packet, we'll simply create a new buffer that appends the old data
                 * (aka the stored data) to the beginning of the buffer, followed by the new ("current") data.
                 * 
                 * Sometimes, a packet can be both merged _and_ split. This code should hopefully handle that.
                 * Unfortunately, it's not easily tested even with unit tests. As such, I'll leave this as a TODO.
                 */
                try
                {
                    Span<byte> buffer = client.CurrentBuffer;

                    var stored = client.StoredPacketBuffer;
                    if (stored.Count > 0)
                    {
                        // We'll make a new buffer append on top.
                        var newData = new byte[stored.Count + bytesRead];
                        stored.CopyTo(newData);
                        Buffer.BlockCopy(client.CurrentBuffer, 0, newData, stored.Count, bytesRead);
                        stored.Clear();

                        // We'll be working from this _new_ buffer now.
                        buffer = newData;
                    }

                    var offset = 0;
                    while (offset < bytesRead)
                    {
                        var fullLen = -1;
                        var hasHeader = (bytesRead - offset) >= 11;

                        if (hasHeader)
                        {
                            // We can try to unpack.
                            fullLen = Unpacker.Unpack(client.Crypto, buffer[offset..]);
                        }

                        if (!hasHeader || offset + fullLen >= buffer.Length)
                        {
                            // Expand the capacity only if needed.
                            if (stored.Count + buffer.Length >= stored.Capacity &&
                                stored.Capacity < 65535 &&
                                stored.Count + buffer.Length <= 65535)
                                stored.Capacity = 65535;

                            if (stored.Count + buffer.Length > stored.Capacity)
                                throw new Exception("Packet is too large!");

                            // https://github.com/dotnet/runtime/issues/1530
                            // Unnecessary allocation. Check again when .NET 5.0 comes out.
                            client.StoredPacketBuffer.AddRange(buffer[offset..].ToArray());

                            break; // Continue receiving data.
                        }

                        client.HasValidPacket = true;

                        // Handle a part of the packet, but don't get too far ahead of ourselves.
                        // In other words, fire the event that lets the actual server handle the packet.
                        try
                        {
                            HandlePacket(client, buffer.Slice(offset, fullLen).ToArray());
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Packet handler error. Disconnecting client {0}.", client.Socket.RemoteEndPoint);
                            throw ex; // Catch it in the exception handler outside.
                        }

                        offset += fullLen;
                    }

                    handler.BeginReceive(client.CurrentBuffer, 0, client.CurrentBuffer.Length, 0,
                        new AsyncCallback(ReadCallback), client);
                }
                catch (PacketHeaderException)
                {
                    Log.Error("{0} sent us a broken packet. Disconnecting.", ((IPEndPoint)client.Socket.RemoteEndPoint).Address);

                    try
                    {
                        OnDisconnect(client);
                        handler.Close();
                    }
                    catch { }
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());

                    if (System.Diagnostics.Debugger.IsAttached)
                        throw e;
                    else
                    {
                        // Try to close the socket, but don't freak out if it errors.
                        try
                        {
                            OnDisconnect(client);
                            handler.Close();
                        }
                        catch { }
                    }
                }
            }
        }
    }
}
