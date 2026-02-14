// <copyright file="Channel.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("index")]
        public byte Index { get; set; }

        /// <summary>
        /// Gets or sets the channel name (up to 31 characters as per MeshCore spec)
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the channel encryption secret (16-byte key).
        /// Null when the channel has no encryption key (all-zero key from device).
        /// </summary>
        [JsonIgnore]
        public ChannelSecret? EncryptionKey { get; set; }

        /// <summary>
        /// Gets or sets whether this is the default "All" channel
        /// </summary>
        [JsonPropertyName("is_default_channel")]
        public bool IsDefaultChannel
        {
            get { return this.Index == 0x00; }
        }

        /// <summary>
        /// Returns a JSON representation of the channel configuration.
        /// </summary>
        /// <returns>A JSON string describing the channel configuration.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
