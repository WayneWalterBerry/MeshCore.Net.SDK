// <copyright file="AdvertPathInfo.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents the most recently observed advert path for a contact.
    /// </summary>
    public sealed class AdvertPathInfo
    {
        /// <summary>
        /// Gets or sets the Unix timestamp (seconds since epoch) when the advert was received.
        /// </summary>
        [JsonPropertyName("received_timestamp")]
        public DateTime ReceivedTimestamp { get; set; }

        /// <summary>
        /// Gets or sets the raw hop path as a sequence of hop identifiers from this node to the contact.
        /// </summary>
        [JsonPropertyName("path")]
        public byte[] Path { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Returns a JSON representation of the advert path information.
        /// </summary>
        /// <returns>A JSON string describing the advert path.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}