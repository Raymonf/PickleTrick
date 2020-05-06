using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using PickleTrick.Core.Crypto;
using PickleTrick.Core.Server.Events;

namespace PickleTrick.Core.Server
{
    public abstract class ServerApp
    {
        public event OnPacketDelegate OnPacket;
        public abstract void PrivateInit();

        // Thread signal.  
        public ManualResetEvent allDone = new ManualResetEvent(false);

        public ServerApp(int port)
        {
            Initialize(port);
        }

        private void Initialize(int port)
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Any, port));
                listener.Listen(100);

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
                Console.WriteLine(e.ToString());

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
            Client state = new Client
            {
                Socket = handler
            };

            handler.BeginReceive(state.CurrentBuffer, 0, state.CurrentBuffer.Length, 0,
                new AsyncCallback(ReadCallback), state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            // Retrieve the client and the socket from the asynchronous state.
            Client client = (Client)ar.AsyncState;
            Socket handler = client.Socket;

            // Read data from the socket.
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
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
                            if (stored.Count + buffer.Length > stored.Capacity)
                                throw new Exception("Packet is too large!");

                            // https://github.com/dotnet/runtime/issues/1530
                            // Unnecessary allocation. Check again when .NET 5.0 comes out.
                            client.StoredPacketBuffer.AddRange(buffer[offset..].ToArray());

                            break; // Continue receiving data.
                        }

                        // Handle a part of the packet, but don't get too far ahead of ourselves.
                        // In other words, fire the event that lets the actual server handle the packet.
                        OnPacket(client, buffer.Slice(offset, fullLen));

                        offset += fullLen;
                    }

                    handler.BeginReceive(client.CurrentBuffer, 0, client.CurrentBuffer.Length, 0,
                        new AsyncCallback(ReadCallback), client);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    if (System.Diagnostics.Debugger.IsAttached)
                        throw e;
                    else
                    {
                        // Try to close the socket, but don't freak out if it errors.
                        try
                        {
                            handler.Close();
                        }
                        catch { }
                    }
                }
            }
        }

        private void Send(Socket handler, byte[] data)
        {
            // Begin sending the data to the remote device.  
            handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int bytesSent = socket.EndSend(ar);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                if (System.Diagnostics.Debugger.IsAttached)
                    throw e;
                else
                {
                    // Try to close the socket, but don't freak out if it errors.
                    try
                    {
                        ((Socket)ar.AsyncState).Close();
                    }
                    catch { }
                }
            }
        }
    }
}
