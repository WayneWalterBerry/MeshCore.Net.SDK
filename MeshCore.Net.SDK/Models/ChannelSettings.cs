namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents channel settings for creating or updating a channel
    /// </summary>
    public class ChannelSettings
    {
        /// <summary>
        /// Gets or sets the channel name (up to 31 characters as per MeshCore spec)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the channel description
        /// </summary>
        public string? Description { get; set; }
        
        /// <summary>
        /// Gets or sets the LoRa frequency for this channel in Hz
        /// </summary>
        public long Frequency { get; set; }
        
        /// <summary>
        /// Gets or sets whether the channel should be encrypted
        /// </summary>
        public bool IsEncrypted { get; set; }
        
        /// <summary>
        /// Gets or sets the encryption key (32-byte AES key as hex string)
        /// Only used when IsEncrypted is true
        /// </summary>
        public string? EncryptionKey { get; set; }
        
        /// <summary>
        /// Gets or sets whether this should be the default "All" channel
        /// </summary>
        public bool IsDefaultChannel { get; set; }
    }
}