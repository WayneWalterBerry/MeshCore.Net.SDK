// <copyright file="Message.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a message in the MeshCore network
    /// Contains only data that actually comes from the radio payload
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the contact identifier of the sender (from radio payload)
        /// </summary>
        [JsonPropertyName("from_contact_id")]
        public string FromContactId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the channel index on which the message was sent/received
        /// </summary>
        [JsonPropertyName("channel_index")]
        public uint ChannelIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the message content (from radio payload)
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was created (from radio payload)
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets whether this is a text message (true) or binary data (false)
        /// Based on txt_type field in radio payload
        /// </summary>
        [JsonPropertyName("is_text_message")]
        public bool IsTextMessage { get; set; } = true;

        /// <summary>
        /// Returns a JSON representation of the message.
        /// </summary>
        /// <returns>A JSON string describing the message.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
