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

        public Channel Deserialize(byte[] data)
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
                result.IsEncrypted = !isAllZeros;

                if (result.IsEncrypted)
                {
                    result.EncryptionKey = Convert.ToHexString(keyBytes);
                }

                offset += 16;
            }

            // Set default values
            result.Frequency = 433000000; // Default frequency

            return true;
        }

        public byte[] Serialize(Channel obj)
        {
            var parts = new List<string>();

            // Channel ID should be first - if not provided, generate one for new channels
            var channelId = obj.Index.ToString();
            parts.Add(channelId);

            // Then add other properties
            parts.Add(obj.Name ?? "All");
            parts.Add(obj.Frequency.ToString());
            parts.Add(obj.IsEncrypted ? "1" : "0");

            if (obj.IsEncrypted && !string.IsNullOrWhiteSpace(obj.EncryptionKey))
            {
                parts.Add(obj.EncryptionKey);
            }
            else if (obj.IsEncrypted)
            {
                // Generate a random key if encryption is enabled but no key provided
                var random = new Random();
                var key = new byte[32]; // 32 bytes for AES-256
                random.NextBytes(key);
                parts.Add(Convert.ToHexString(key));
            }

            var configString = string.Join("\0", parts);
            return Encoding.UTF8.GetBytes(configString);
        }
    }
}
