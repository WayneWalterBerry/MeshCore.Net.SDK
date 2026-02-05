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
        /// Gets or sets the display name of the contact (from radio payload)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MeshCore node identifier (from radio payload)
        /// </summary>
        public required byte[] PublicKey { get; set; }

        /// <summary>
        /// Gets the 6-byte public key prefix used by the MeshCore firmware for
        /// advert path and message routing operations. If the full public key
        /// is not available or shorter than 6 bytes, an empty array is returned.
        /// </summary>
        public byte[] PublicKeyPrefix
        {
            get
            {
                if (this.PublicKey == null || this.PublicKey.Length < 6)
                {
                    return Array.Empty<byte>();
                }

                var prefix = new byte[6];
                Buffer.BlockCopy(this.PublicKey, 0, prefix, 0, 6);
                return prefix;
            }
        }

        /// <summary>
        /// Gets or sets the type of the node represented by this instance.
        /// </summary>
        public NodeType NodeType { get; set; } = NodeType.Unknown;

        /// <summary>
        /// Gets or sets information about the path to the advertisement content associated with this instance.
        /// </summary>
        public AdvertPathInfo? AdvertPath { get; set; }

        /// <summary>
        /// Gets or sets the flags that specify additional information or options for the contact.
        /// </summary>
        public ContactFlags ContactFlags { get; set; } = ContactFlags.None;
    }
}
