// <copyright file="Channel.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a MeshCore channel configuration
    /// Based on MeshCore channel architecture research
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Gets or sets the unique channel index
        /// </summary>
        public byte Index { get; set; }

        /// <summary>
        /// Gets or sets the channel name (up to 31 characters as per MeshCore spec)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the LoRa frequency for this channel in Hz
        /// </summary>
        public long Frequency { get; set; }

        /// <summary>
        /// Gets or sets whether the channel is encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the encryption key (32-byte AES key as hex string)
        /// Only used when IsEncrypted is true
        /// </summary>
        public string? EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets whether this is the default "All" channel
        /// </summary>
        public bool IsDefaultChannel
        {
            get { return this.Index == 0x00; }
        }
    }
}
