// <copyright file="Contact.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a contact in the MeshCore network
    /// Contains only data that actually comes from the radio payload
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Gets or sets the unique contact identifier (derived from public key)
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name of the contact (from radio payload)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MeshCore node identifier (from radio payload)
        /// </summary>
        public string? NodeId { get; set; }
    }
}
