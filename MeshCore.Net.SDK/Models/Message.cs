// <copyright file="Message.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a message in the MeshCore network
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Gets or sets the unique message identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact identifier of the sender
        /// </summary>
        public string FromContactId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact identifier of the recipient
        /// </summary>
        public string ToContactId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the message content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the message was created
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the type of message
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the current status of the message
        /// </summary>
        public MessageStatus Status { get; set; }

        /// <summary>
        /// Gets or sets whether the message has been read
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Gets or sets the number of delivery attempts made for this message
        /// </summary>
        public int? DeliveryAttempts { get; set; }
    }
}
