// <copyright file="ChannelDeserializer.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;

    internal class ChannelParamsSerialization : 
        IBinarySerializer<ChannelParams>
    {
        private static readonly Lazy<ChannelParamsSerialization> _instance = new(() => new ChannelParamsSerialization());

        /// <summary>
        /// Gets the singleton instance of the ChannelDeserializer
        /// </summary>
        public static ChannelParamsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private ChannelParamsSerialization()
        {
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
        public byte[] Serialize(ChannelParams obj)
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
            else if (obj.Name != null && obj.Name.StartsWith('#'))
            {
                // Hashtag channel: derive key from SHA-256 of channel name (first 16 bytes)
                // This matches the Python CLI: sha256(channel_name.encode("utf-8")).digest()[0:16]
                secret = ChannelSecret.FromChannelName(obj.Name).ToByteArray();
            }
            else
            {
                // No key and not a hashtag channel — send all zeros (unencrypted)
                secret = new byte[16];
            }

            Array.Copy(secret, 0, payload, 33, 16);

            return payload;
        }
    }
}
