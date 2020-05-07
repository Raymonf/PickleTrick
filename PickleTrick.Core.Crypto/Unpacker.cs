using System;
using PickleTrick.Core.Common;

namespace PickleTrick.Core.Crypto
{
    public class Unpacker
    {
        /// <summary>
        /// This method is copied directly from the Trickster server.
        /// 
        /// My theory on what this method does is that it makes sure the first packet sent is valid.
        /// If it's not valid, we can exit early as the server will know that it's an invalid client.
        /// Perhaps someone's trying to send some trash data or connect with a non-client.
        /// 
        /// The Header object (at the time of a call to this function) is essentially a bunch 
        /// of fields within the raw packet. 
        /// </summary>
        /// <param name="header">The Header object before decrypting the packet header.</param>
        /// <param name="client">The current client session.</param>
        /// <returns>Primitive packet validity flag</returns>
        private static bool CheckHeader(Header header, CryptoClient client)
        {
            if (!client.IsFirstPacket)
            {
                return true;
            }

            byte packing = header.Packing;
            if (packing > 0xFu && (KeyTable.Table[(ushort)((header.RandKey << 8) + packing)] & 0xF) == 15)
            {
                // If that magic value is 15, we know this is a primitively "valid packet".
                // We can update the session since we know the next packet is no longer the first packet.
                client.IsFirstPacket = false;
                return true;
            }

            return false;
        }
        
        private static Header PacketToHeader(Span<byte> data)
        {
            return new Header()
            {
                Len = BitConverter.ToUInt16(data),
                Cmd = BitConverter.ToUInt16(data[2..]),
                Seq = BitConverter.ToUInt16(data[4..]),
                RandKey = data[6],
                Packing = data[7],
                CheckFlag = data[8]
            };
        }

        /// <summary>
        /// Updates the length in the header to not have the dummy data.
        /// This is helpful for later, as we don't want to decrypt the dummy data.
        /// Otherwise, that would likely(?) affect the tail checksum value calculation.
        /// </summary>
        /// <param name="packet">The packet data to unpack.</param>
        /// <returns>Whether exclusion was successful or not, also indicating the packet's validity</returns>
        private static bool ExcludeDummy(Span<byte> packet)
        {
            byte packing = packet[7];

            // The "second round" value of Packing will let us know if there's any dummy data.
            if ((packing & 8) > 0)
            {
                ushort lenNoDummy = (ushort)(BitConverter.ToUInt16(packet) - (packet[6] % 13));

                if (lenNoDummy < 11) // The length should always be >11 if there's dummy data.
                {
                    return false;
                }

                // Update the packet's length to remove the dummy data.
                ByteUtil.CopyTo(packet, 0, lenNoDummy);

                // Time to update the packing value to be its "third round" value.
                packet[7] = (byte)(packing ^ 8);
            }

            return true;
        }

        /// <summary>
        /// Header decryption logic
        /// </summary>
        /// <param name="client">The current client session.</param>
        /// <param name="packet">The packet data to unpack.</param>
        /// <returns>A header if successfully decoded, or null if unsuccessful</returns>
        private static Header UnpackHeader(Span<byte> packet, CryptoClient client)
        {
            var checkHeader = CheckHeader(PacketToHeader(packet), client);

            if (!checkHeader)
                throw new Exception("Header was invalid");

            var key = client.Key;

            byte packing = packet[7];

            // Don't unpack the header twice.
            if (packing > 0xFu)
            {
                byte packetStatus = KeyTable.Table[(packet[6] << 8) + packing];
                if (packetStatus > 0xFu || (packetStatus & 2) == 0)
                {
                    // Well, something's off.
                    return null;
                }

                // We can start calculating the packet sequence, opcode, and length now that we know we have
                // at least what we _should_ theoretically need to calculate them.
                ushort sequence = (ushort)((KeyTable.Table[(ushort)(((packet[3] ^ packet[7]) << 8) + packet[5])] << 8)
                    + KeyTable.Table[(ushort)(((packet[2] ^ key) << 8) + packet[4])]);

                ushort opcode = (ushort)((KeyTable.Table[(ushort)(((packet[1] ^ key) << 8) + packet[3])] << 8)
                    + KeyTable.Table[(ushort)(((packet[6] ^ packet[0]) << 8) + packet[2])]);

                // The length at this point includes the dummy bytes. We'll get rid of that later.
                var length = (ushort)((KeyTable.Table[((packet[6] ^ packet[7]) << 8) + packet[1]] << 8)
                    + KeyTable.Table[(ushort)(((packet[7] ^ key) << 8) + packet[0])]);

                // Update Packing within the actual packet for later use.
                // This should now be the "second round" value.
                packet[7] = (byte)(packetStatus ^ 2);

                // Now we can overwrite the length, opcode, and sequence of the actual packet.
                // Obviously, we'll use this next.
                ByteUtil.CopyTo(packet, 0, length);
                ByteUtil.CopyTo(packet, 2, opcode);
                ByteUtil.CopyTo(packet, 4, sequence);
            }

            // Let's check some basic lengths and then verify that the checksum was correct.
            if (BitConverter.ToUInt16(packet) >= 0xBu
                && BitConverter.ToInt32(packet) < 0x7FFFFFFFu
                && CryptoCommon.MakeChecksum(packet, key) == packet[8])
            {
                // Since all of these checks have passed, we can create a Header from this data.
                return PacketToHeader(packet);
            }

            // A check failed, so we're going to tell the GetHeader method that it failed.
            // It should throw an exception from there.
            return null;
        }

        /// <summary>
        /// Actual data decryption logic and tail checksum validation logic
        /// </summary>
        /// <param name="client">The current client session.</param>
        /// <param name="packet">The packet data to unpack.</param>
        /// <returns>Whether we were able to successfully unpack the data</returns>
        private static bool UnpackData(CryptoClient client, Span<byte> packet)
        {
            var header = PacketToHeader(packet);
            byte magicKey = (byte)(header.Cmd * header.RandKey % 256);

            // Note: The minimum [encrypted] packet length is 11, but the minimum [packet length] should be 9.
            // This is probably to compensate for the tail checksum, which shouldn't be in the true packet length.
            header.Len -= 2;

            int offset = 0; // This offset will eventually tell us where the tail checksum value is.
            ushort calculatedTail = 0;

            if ((header.Packing & 4) > 0)
            {
                // Possible use: If the packet isn't empty, decrypt the data?
                if (header.Len != 9)
                {
                    do
                    {
                        byte decrypted = KeyTable.Table[(ushort)(((magicKey ^ offset) << 8) + packet[offset + 9])];
                        packet[offset + 9] = decrypted;
                        calculatedTail += decrypted;
                        offset++;
                    }
                    while (offset < header.Len - 9);
                }

                // This is for the tail checksum.
                // We shouldn't add the tail checksum to the data checksum, of course.
                for (int i = 0; i < 2; i++)
                {
                    byte decrypted = KeyTable.Table[(ushort)(((magicKey ^ offset) << 8) + packet[offset + 9])];
                    packet[offset + 9] = decrypted;
                    offset++;
                }

                // In a way, tell the later code (the code below that updates the encryption key)
                // what it should actually do.
                header.Packing ^= 4;
            }
            else
            {
                // The Packing value seems to have been non-optimal.
                // This is probably a fallback so that we don't crash immediately.
                // Regardless, this was in the original packet decryption code. I don't know what it does.
                offset = header.Len - 7;
                calculatedTail = (ushort)(header.Len - 9);
            }

            // We've been calculating a "tail checksum" value.
            // Of course, this should match the packet's given tail checksum value.
            if (calculatedTail != BitConverter.ToUInt16(packet[(offset + 7)..]))
            {
                return false;
            }

            // Packing can be 0x06 or 0x07.
            // We should update the key if the value is 0x07.
            // If it's 0x06, that means the key stays the same.
            // In other words, odd value = update; even value = don't update.
            if ((header.Packing & 1) > 0)
            {
                // We can update the key now. We're done.
                CryptoCommon.UpdateKey(client, calculatedTail);
            }

            return true;
        }

        /// <summary>
        /// Unpacks an encrypted packet from Trickster.
        /// </summary>
        /// <param name="client">The current client session.</param>
        /// <param name="packet">The packet data to unpack.</param>
        /// <returns>The length of the data INCLUDING the dummy data.</returns>
        public static ushort Unpack(CryptoClient client, Span<byte> packet)
        {
            var header = UnpackHeader(packet, client);
            if (header == null)
                throw new Exception("Header could not be unpacked");

            // Store the full length before excluding the dummy
            // We'll need this so we know how many bytes to skip in a merged packet.
            var fullLen = header.Len;

            // If the length is longer than the packet, exit early.
            // We might have a split packet.
            if (fullLen > packet.Length)
                return fullLen;

            bool excludeDummy = ExcludeDummy(packet);
            if (!excludeDummy)
                throw new Exception("Dummy data could not be excluded");

            bool unpackData = UnpackData(client, packet);
            if (!unpackData)
                throw new Exception("Data could not be unpacked");

            return fullLen;
        }
    }
}
