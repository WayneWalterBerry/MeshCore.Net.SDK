// <copyright file="MessageV3Serialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Handles serialization and deserialization of V3 format MeshCore messages
    /// Supports RESP_CODE_CONTACT_MSG_RECV_V3 (16) and RESP_CODE_CHANNEL_MSG_RECV_V3 (17)
    /// V3 format includes SNR data and reserved fields for future enhancements
    /// </summary>
    internal class MessageV3Serialization : IBinaryDeserializer<Message>
    {
        private static readonly Lazy<MessageV3Serialization> _instance = new(() => new MessageV3Serialization());

        /// <summary>
        /// Gets the singleton instance of the MessageV3Serialization
        /// </summary>
        public static MessageV3Serialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private MessageV3Serialization()
        {
        }

        /// <summary>
        /// Deserializes V3 format message data
        /// </summary>
        /// <param name="data">Raw message data from device</param>
        /// <returns>Parsed message object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public Message Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out Message? result))
            {
                throw new InvalidOperationException("Failed to deserialize V3 message from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize V3 format message data
        /// </summary>
        /// <param name="data">Raw message data from device</param>
        /// <param name="result">The parsed message if successful</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        public bool TryDeserialize(byte[] data, out Message? result)
        {
            result = null;

            if (data == null || data.Length == 0)
            {
                return false;
            }

            try
            {
                return TryDeserializeContactMessage(data, out result);
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Deserializes contact message in V3 format
        /// Format: SNR + reserved1 + reserved2 + pub_key(6) + path_len + txt_type + timestamp + text
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        public bool TryDeserializeContactMessage(byte[] data, out Message? result)
        {
            result = null;

            var offset = 0;

            if (data.Length < 4)
            {
                return false;
            }

            // Parse V3 header: SNR + 2 reserved bytes
            var snr = (sbyte)data[offset++] / 4.0f; // SNR scaled by 4
            var reserved1 = data[offset++];
            var reserved2 = data[offset++];

            // Continue with standard contact message parsing
            if (data.Length < offset + 6)
            {
                return false;
            }

            // Extract 6-byte contact public key prefix
            var contactKeyPrefix = new byte[6];
            Array.Copy(data, offset, contactKeyPrefix, 0, 6);
            offset += 6;

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
                Id = Guid.NewGuid().ToString(),
                FromContactId = Convert.ToHexString(contactKeyPrefix),
                ToContactId = "self",
                Content = messageText,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                Type = textType == 0x00 ? MessageType.Text : MessageType.Binary,
                Status = MessageStatus.Delivered,
                IsRead = false
            };

            return true;
        }

        /// <summary>
        /// Deserializes channel message in V3 format
        /// Format: SNR + reserved1 + reserved2 + channel_idx + path_len + txt_type + timestamp + text
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        public bool TryDeserializeChannelMessage(byte[] data, out Message? result)
        {
            result = null;

            var offset = 0;

            if (data.Length < 4)
            {
                return false;
            }

            // Parse V3 header: SNR + 2 reserved bytes
            var snr = (sbyte)data[offset++] / 4.0f;
            var reserved1 = data[offset++];
            var reserved2 = data[offset++];

            // Continue with standard channel message parsing
            if (data.Length < offset + 1)
            {
                return false;
            }
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
                Id = Guid.NewGuid().ToString(),
                FromContactId = $"Channel_{channelIndex}",
                ToContactId = "channel",
                Content = messageText,
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                Type = textType == 0x00 ? MessageType.Text : MessageType.Binary,
                Status = MessageStatus.Delivered,
                IsRead = false
            };

            return true;
        }
    }
}