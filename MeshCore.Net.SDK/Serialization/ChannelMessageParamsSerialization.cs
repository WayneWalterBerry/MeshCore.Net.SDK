// <copyright file="ChannelMessageParamsSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using System.Text;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles serialization of <see cref="ChannelMessageParams"/> into the binary payload
    /// expected by CMD_SEND_CHANNEL_TXT_MSG (0x03) with txt_type=0x00 (plain text).
    /// </summary>
    /// <remarks>
    /// Wire format (variable length):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    txt_type   (0x00 = plain text)
    /// 1       uint8    channel_idx
    /// 2-5     uint32   timestamp  (Unix epoch seconds, little-endian)
    /// 6+      bytes    message content (UTF-8 encoded)
    /// N       uint8    null terminator (0x00)
    /// </code>
    /// </remarks>
    internal sealed class ChannelMessageParamsSerialization : IBinarySerializer<ChannelMessageParams>
    {
        /// <summary>
        /// The txt_type byte value for plain text messages.
        /// </summary>
        private const byte TXT_TYPE_PLAIN = 0x00;

        private static readonly Lazy<ChannelMessageParamsSerialization> _instance = new(() => new ChannelMessageParamsSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="ChannelMessageParamsSerialization"/> class.
        /// </summary>
        public static ChannelMessageParamsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private ChannelMessageParamsSerialization()
        {
        }

        /// <summary>
        /// Serializes a <see cref="ChannelMessageParams"/> object into the binary payload
        /// for CMD_SEND_CHANNEL_TXT_MSG with txt_type=0x00.
        /// </summary>
        /// <param name="obj">The channel message parameters to serialize.</param>
        /// <returns>A byte array containing the serialized message payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the parameters have empty content.</exception>
        public byte[] Serialize(ChannelMessageParams obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (obj.IsEmpty)
            {
                throw new InvalidOperationException("Cannot serialize message with empty content.");
            }

            var messageBytes = Encoding.UTF8.GetBytes(obj.Content);

            var payload = new byte[obj.EstimatedPayloadSize];
            var offset = 0;

            // 1. txt_type = 0x00 (plain text)
            payload[offset++] = TXT_TYPE_PLAIN;

            // 2. channel index
            payload[offset++] = obj.ChannelIndex;

            // 3. timestamp (4 bytes, little-endian uint32) - convert DateTime UTC to Unix epoch
            var unixTimestamp = (uint)new DateTimeOffset(obj.Timestamp, TimeSpan.Zero).ToUnixTimeSeconds();
            BitConverter.GetBytes(unixTimestamp).CopyTo(payload, offset);
            offset += 4;

            // 4. message content (UTF-8)
            Buffer.BlockCopy(messageBytes, 0, payload, offset, messageBytes.Length);
            offset += messageBytes.Length;

            // 5. null terminator
            payload[offset] = 0x00;

            return payload;
        }
    }
}
