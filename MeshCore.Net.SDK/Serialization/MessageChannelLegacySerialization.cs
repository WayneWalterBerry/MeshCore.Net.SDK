// <copyright file="MessageChannelLegacySerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Handles serialization and deserialization of legacy format MeshCore channel messages
    /// Supports RESP_CODE_CHANNEL_MSG_RECV (8)
    /// Legacy format does not include SNR data or reserved fields
    /// </summary>
    internal class MessageChannelLegacySerialization : IBinaryDeserializer<Message>
    {
        private static readonly Lazy<MessageChannelLegacySerialization> _instance = new(() => new MessageChannelLegacySerialization());

        /// <summary>
        /// Gets the singleton instance of the MessageChannelLegacySerialization
        /// </summary>
        public static MessageChannelLegacySerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private MessageChannelLegacySerialization()
        {
        }

        /// <summary>
        /// Deserializes legacy format channel message data
        /// </summary>
        /// <param name="data">Raw message data from device</param>
        /// <returns>Parsed message object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public Message? Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out Message? result))
            {
                throw new InvalidOperationException("Failed to deserialize legacy channel message from binary data");
            }

            return result;
        }

        /// <summary>
        /// Deserializes channel message in legacy format
        /// Format: channel_idx + path_len + txt_type + timestamp + text
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        public bool TryDeserialize(byte[] data, out Message? result)
        {
            result = null;

            var offset = 0;

            if (data.Length < 1)
            {
                return false;
            }

            // Extract channel index
            var channelIndex = data[offset++];

            if (data.Length < offset + 1)
            {
                return false;
            }
            var pathLen = data[offset++];

            if (data.Length < offset + 1)
            {
                return false;
            }
            var textType = data[offset++];

            if (data.Length < offset + 4)
            {
                return false;
            }
            var timestamp = BitConverter.ToUInt32(data, offset);
            offset += 4;

            // Extract message text (remaining bytes)
            var messageText = "";
            if (offset < data.Length)
            {
                messageText = Encoding.UTF8.GetString(data, offset, data.Length - offset).TrimEnd('\0');
            }

            result = new Message
            {
                Content = messageText, // Channel messages don't have sender name prefix
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                IsTextMessage = textType == 0x00
            };

            return true;
        }
    }
}