// <copyright file="Contact.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a contact in the MeshCore network
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Gets or sets the unique contact identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the contact
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MeshCore node identifier
        /// </summary>
        public string? NodeId { get; set; }

        /// <summary>
        /// Gets or sets when the contact was last seen on the network
        /// </summary>
        public DateTime? LastSeen { get; set; }

        /// <summary>
        /// Gets or sets the signal strength for communication with this contact
        /// </summary>
        public int SignalStrength { get; set; }

        /// <summary>
        /// Gets or sets whether the contact is currently online
        /// </summary>
        public bool IsOnline { get; set; }

        /// <summary>
        /// Gets or sets the current status of the contact
        /// </summary>
        public ContactStatus Status { get; set; }
    }
}
