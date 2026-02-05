// <copyright file="Message.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

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
        public string FromContactId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the channel index on which the message was sent/received
        /// </summary>
        public uint ChannelIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets the message content (from radio payload)
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was created (from radio payload)
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets whether this is a text message (true) or binary data (false)
        /// Based on txt_type field in radio payload
        /// </summary>
        public bool IsTextMessage { get; set; } = true;
    }
}
