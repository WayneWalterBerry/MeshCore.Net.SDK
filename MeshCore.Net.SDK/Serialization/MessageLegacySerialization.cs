// <copyright file="MessageLegacySerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Handles serialization and deserialization of legacy format MeshCore messages
    /// Supports RESP_CODE_CONTACT_MSG_RECV (7) and RESP_CODE_CHANNEL_MSG_RECV (8)
    /// Legacy format does not include SNR data or reserved fields
    /// </summary>
    internal class MessageLegacySerialization : IBinaryDeserializer<Message>
    {
        private static readonly Lazy<MessageLegacySerialization> _instance = new(() => new MessageLegacySerialization());

        /// <summary>
        /// Gets the singleton instance of the MessageLegacySerialization
        /// </summary>
        public static MessageLegacySerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private MessageLegacySerialization()
        {
        }

        /// <summary>
        /// Deserializes legacy format message data
        /// </summary>
        /// <param name="data">Raw message data from device</param>
        /// <returns>Parsed message object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public Message Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out Message? result))
            {
                throw new InvalidOperationException("Failed to deserialize legacy message from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize legacy format message data
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
        /// Deserializes contact message in legacy format
        /// Format: pub_key(6) + path_len + txt_type + timestamp + text
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        public bool TryDeserializeContactMessage(byte[] data, out Message? result)
        {
            result = null;

            try
            {
                var offset = 0;

                if (data.Length < 6)
                {
                    result = CreateErrorMessage("Message too short for contact data");
                    return true; // Return error message rather than failing
                }

                // Extract 6-byte contact public key prefix
                var contactKeyPrefix = new byte[6];
                Array.Copy(data, offset, contactKeyPrefix, 0, 6);
                offset += 6;

                if (data.Length < offset + 1)
                {
                    result = CreateErrorMessage("Missing path length");
                    return true;
                }
                var pathLen = data[offset++];

                if (data.Length < offset + 1)
                {
                    result = CreateErrorMessage("Missing text type");
                    return true;
                }
                var textType = data[offset++];

                if (data.Length < offset + 4)
                {
                    result = CreateErrorMessage("Missing timestamp");
                    return true;
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
            catch (Exception ex)
            {
                result = CreateErrorMessage($"Failed to parse legacy contact message: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Deserializes channel message in legacy format
        /// Format: channel_idx + path_len + txt_type + timestamp + text
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        public bool TryDeserializeChannelMessage(byte[] data, out Message? result)
        {
            result = null;

            try
            {
                var offset = 0;

                if (data.Length < 1)
                {
                    result = CreateErrorMessage("Missing channel index");
                    return true;
                }
                var channelIndex = data[offset++];

                if (data.Length < offset + 1)
                {
                    result = CreateErrorMessage("Missing path length");
                    return true;
                }
                var pathLen = data[offset++];

                if (data.Length < offset + 1)
                {
                    result = CreateErrorMessage("Missing text type");
                    return true;
                }
                var textType = data[offset++];

                if (data.Length < offset + 4)
                {
                    result = CreateErrorMessage("Missing timestamp");
                    return true;
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
            catch (Exception ex)
            {
                result = CreateErrorMessage($"Failed to parse legacy channel message: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Creates an error message for parsing failures
        /// </summary>
        private static Message CreateErrorMessage(string error)
        {
            return new Message
            {
                Id = Guid.NewGuid().ToString(),
                FromContactId = "system",
                ToContactId = "self",
                Content = $"Parse error: {error}",
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Text,
                Status = MessageStatus.Failed,
                IsRead = false
            };
        }
    }
}