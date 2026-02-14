// <copyright file="ChannelDeserializer.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;

    internal class ChannelSerialization :
        IBinaryDeserializer<Channel>,
        IBinarySerializer<Channel>
    {
        private static readonly Lazy<ChannelSerialization> _instance = new(() => new ChannelSerialization());

        /// <summary>
        /// Gets the singleton instance of the ChannelDeserializer
        /// </summary>
        public static ChannelSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private ChannelSerialization()
        {
        }

        public Channel? Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out Channel? result))
            {
                throw new InvalidOperationException("Failed to deserialize channel from binary data");
            }

            return result;
        }

        public bool TryDeserialize(byte[] data, out Channel? result)
        {
            result = default(Channel);

            if (data.Length != 50)
            {
                return false;
            }

            result = new Channel();

            var offset = 0;

            // Byte 0: Response code (should be RESP_CODE_CHANNEL_INFO = 18)
            var responseCode = data[offset++];

            // Byte 1: Channel index/ID
            var channelIndex = data[offset++];
            result.Index = channelIndex;

            // Bytes 2-33: Channel name (32 bytes, null-terminated)
            var nameBytes = new byte[32];
            Array.Copy(data, offset, nameBytes, 0, 32);

            // Find the null terminator
            var nullIndex = Array.IndexOf(nameBytes, (byte)0);
            var nameLength = nullIndex >= 0 ? nullIndex : 32;

            result.Name = Encoding.UTF8.GetString(nameBytes, 0, nameLength).Trim();

            if (string.IsNullOrWhiteSpace(result.Name))
            {
                return false;
            }

            offset += 32;

            // Bytes 34-49: Channel secret/key (16 bytes)
            if (offset + 16 <= data.Length)
            {
                var keyBytes = new byte[16];
                Array.Copy(data, offset, keyBytes, 0, 16);

                // Check if the key is all zeros (unencrypted channel)
                bool isAllZeros = keyBytes.All(b => b == 0);

                if (!isAllZeros)
                {
                    result.EncryptionKey = ChannelSecret.FromBytes(keyBytes);
                }

                offset += 16;
            }

            return true;
        }

        /// <summary>
        /// Serializes a <see cref="Channel"/> into the binary wire format expected by
        /// the MeshCore firmware's CMD_SET_CHANNEL (0x20) command.
        /// </summary>
        /// <remarks>
        /// Wire format (49 bytes, command byte added by transport):
        /// <code>
        /// [index: 1 byte][name: 32 bytes UTF-8 zero-padded][secret: 16 bytes]
        /// </code>
        /// For hashtag channels (names starting with '#'), the secret is derived as
        /// <c>SHA256(name)[0:16]</c>, matching the Python CLI behavior:
        /// <c>channel_secret = sha256(channel_name.encode("utf-8")).digest()[0:16]</c>
        /// </remarks>
        public byte[] Serialize(Channel obj)
        {
            // Wire format: [index(1)][name(32)][secret(16)] = 49 bytes
            var payload = new byte[49];

            // Byte 0: Channel index
            payload[0] = (byte)obj.Index;

            // Bytes 1-32: Channel name (UTF-8 encoded, zero-padded to 32 bytes)
            var nameBytes = Encoding.UTF8.GetBytes(obj.Name ?? "All");
            var copyLen = Math.Min(nameBytes.Length, 32);
            Array.Copy(nameBytes, 0, payload, 1, copyLen);
            // Remaining bytes are already zero from array initialization

            // Bytes 33-48: Channel secret (16 bytes)
            byte[] secret;

            if (obj.EncryptionKey != null && !obj.EncryptionKey.IsEmpty)
            {
                // Explicit key provided
                secret = obj.EncryptionKey.ToByteArray();
            }
            else
            {
                // No key — send all zeros (unencrypted)
                secret = new byte[16];
            }

            Array.Copy(secret, 0, payload, 33, 16);

            return payload;
        }
    }
}
