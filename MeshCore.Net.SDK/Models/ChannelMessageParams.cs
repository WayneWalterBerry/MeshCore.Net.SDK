// <copyright file="ChannelMessageParams.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the parameters for a plain-text message sent to a channel via
    /// CMD_SEND_CHANNEL_TXT_MSG with txt_type=0x00.
    /// </summary>
    /// <remarks>
    /// Wire format (variable length):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    txt_type (0x00 = plain text)
    /// 1       uint8    channel_idx (channel index)
    /// 2-5     uint32   timestamp (Unix epoch seconds, little-endian)
    /// 6+      bytes    message content (UTF-8 encoded)
    /// N       uint8    null terminator (0x00)
    /// </code>
    /// Matches the Python reference <c>send_channel_msg</c>.
    /// </remarks>
    public sealed class ChannelMessageParams
    {
        /// <summary>
        /// Gets or sets the target channel index.
        /// </summary>
        [JsonPropertyName("channel_index")]
        public byte ChannelIndex { get; set; }

        /// <summary>
        /// Gets or sets the message content to send.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the message was created.
        /// Defaults to the current UTC time. Serialized as a Unix epoch uint32 on the wire.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a value indicating whether the message content is empty.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Content);

        /// <summary>
        /// Gets the message content length in bytes when UTF-8 encoded.
        /// </summary>
        [JsonIgnore]
        public int ContentLength => string.IsNullOrEmpty(Content) ? 0 : Encoding.UTF8.GetByteCount(Content);

        /// <summary>
        /// Gets the estimated total payload size in bytes.
        /// Format: [txt_type(1)][channel_idx(1)][timestamp(4)][message][null(1)]
        /// </summary>
        [JsonIgnore]
        public int EstimatedPayloadSize => 1 + 1 + 4 + ContentLength + 1;

        /// <summary>
        /// Creates a new <see cref="ChannelMessageParams"/> for the specified channel and message content.
        /// </summary>
        /// <param name="channelIndex">The channel index to send the message to.</param>
        /// <param name="content">The message text to send.</param>
        /// <returns>A new <see cref="ChannelMessageParams"/> instance ready for serialization.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or empty.</exception>
        public static ChannelMessageParams Create(byte channelIndex, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }

            return new ChannelMessageParams
            {
                ChannelIndex = channelIndex,
                Content = content,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Creates a new <see cref="ChannelMessageParams"/> with a custom timestamp.
        /// </summary>
        /// <param name="channelIndex">The channel index to send the message to.</param>
        /// <param name="content">The message text to send.</param>
        /// <param name="timestamp">The UTC timestamp for the message. Converted to Unix epoch on the wire.</param>
        /// <returns>A new <see cref="ChannelMessageParams"/> instance ready for serialization.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or empty.</exception>
        public static ChannelMessageParams Create(byte channelIndex, string content, DateTime timestamp)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }

            return new ChannelMessageParams
            {
                ChannelIndex = channelIndex,
                Content = content,
                Timestamp = timestamp
            };
        }

        /// <summary>
        /// Returns a JSON representation of the channel message parameters.
        /// </summary>
        /// <returns>A JSON string describing the channel message parameters.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
