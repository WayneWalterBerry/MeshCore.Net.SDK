// <copyright file="MessageChannelV3Serialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Handles serialization and deserialization of V3 format MeshCore channel messages
    /// Supports RESP_CODE_CHANNEL_MSG_RECV_V3 (17)
    /// V3 format includes SNR data and reserved fields for future enhancements
    /// </summary>
    internal class MessageChannelV3Serialization : IBinaryDeserializer<Message>
    {
        private static readonly Lazy<MessageChannelV3Serialization> _instance = new(() => new MessageChannelV3Serialization());

        /// <summary>
        /// Gets the singleton instance of the MessageChannelV3Serialization
        /// </summary>
        public static MessageChannelV3Serialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private MessageChannelV3Serialization()
        {
        }

        /// <summary>
        /// Deserializes V3 format channel message data
        /// </summary>
        /// <param name="data">Raw message data from device</param>
        /// <returns>Parsed message object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public Message? Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out Message? result))
            {
                throw new InvalidOperationException("Failed to deserialize V3 channel message from binary data");
            }

            return result;
        }

        /// <summary>
        /// Deserializes channel message in V3 format
        /// </summary>
        /// <param name="data">Raw message data</param>
        /// <param name="result">Parsed message</param>
        /// <returns>True if successful</returns>
        /// <remarks>
        /// Frame layout for RESP_CODE_CHANNEL_MSG_RECV_V3 as produced by MyMesh::onChannelMessageRecv:
        ///
        /// Byte | Field                          | Notes
        /// -----+--------------------------------+-----------------------------------------------------
        ///  0   | Response code                  | RESP_CODE_CHANNEL_MSG_RECV_V3 (0x11)
        ///  1   | SNR                            | int8, SNR * 4 (0.25 dB steps)
        ///  2   | Reserved1                      | Currently 0
        ///  3   | Reserved2                      | Currently 0
        ///  4   | Channel index                  | uint8, index into device channel table
        ///  5   | Path length                    | pkt->path_len or 0xFF if not route flood
        ///  6   | txt_type                       | TXT_TYPE_* (0x00 = plain text, etc.)
        ///  7-10| Sender timestamp               | uint32, UNIX seconds (little-endian)
        /// 11+  | Text                           | C string in firmware (ASCII/UTF-8), e.g. "Hello"/// 
        /// </remarks>
        public bool TryDeserialize(byte[] data, out Message? result)
        {
            result = null;

            // Initial offset after response code
            var offset = 1;

            if (data.Length < 4)
            {
                return false;
            }

            // Parse V3 header: SNR + 2 reserved bytes
            var snr = (sbyte)data[offset++] / 4.0f; // SNR scaled by 4
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

            // Parse sender name and message content from channel message text
            // Channel messages may include sender prefix: "SenderName: MessageContent"
            string senderName = string.Empty; // Default to empty if no sender in payload
            string messageContent = messageText;

            var colonIndex = messageText.IndexOf(": ");
            if (colonIndex > 0 && colonIndex < messageText.Length - 2)
            {
                // Extract sender name from the payload text
                senderName = messageText.Substring(0, colonIndex);
                messageContent = messageText.Substring(colonIndex + 2); // Skip ": "
            }

            result = new Message
            {
                FromContactId = senderName, // From parsed text payload or empty
                ChannelIndex = channelIndex,
                Content = messageContent, // Clean message content from payload
                Timestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime,
                IsTextMessage = textType == 0x00
            };

            return true;
        }
    }
}