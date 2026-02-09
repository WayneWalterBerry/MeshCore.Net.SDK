// <copyright file="Contact.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MeshCore.Net.SDK.Json;

    /// <summary>
    /// Represents a contact in the MeshCore network
    /// Contains only data that actually comes from the radio payload
    /// </summary>
    public class Contact
    {
        /// <summary>
        /// Gets or sets the display name of the contact (from radio payload)
        /// </summary>
        [JsonPropertyName("adv_name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MeshCore node identifier (from radio payload)
        /// </summary>
        [JsonPropertyName("public_key")]
        [JsonConverter(typeof(ContactPublicKeyJsonConverter))]
        public required ContactPublicKey PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the type of the node represented by this instance.
        /// </summary>
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonNumberEnumConverter<NodeType>))]
        public NodeType NodeType { get; set; } = NodeType.Unknown;

        /// <summary>
        /// Gets or sets information about the path to the advertisement content associated with this instance.
        /// </summary>
        [JsonIgnore]
        public OutboundRoute? OutboundRoute { get; set; }

        /// <summary>
        /// Gets or sets the flags that specify additional information or options for the contact.
        /// </summary>
        [JsonPropertyName("flags")]
        [JsonConverter(typeof(JsonNumberEnumConverter<ContactFlags>))]
        public ContactFlags ContactFlags { get; set; } = ContactFlags.None;

        /// <summary>
        /// Gets or sets the latitude component of the geographic coordinate.
        /// </summary>
        [JsonPropertyName("adv_lat")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the geographic longitude coordinate in degrees.
        /// </summary>
        [JsonPropertyName("adv_lon")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the last advertisement was received or sent.
        /// </summary>
        [JsonPropertyName("last_advert")]
        [JsonConverter(typeof(UnixTimestampJsonConverter))]
        public DateTime LastAdvert { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the object was last modified.
        /// </summary>
        [JsonPropertyName("lastmod")]
        [JsonConverter(typeof(UnixTimestampJsonConverter))]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets the outbound route path length for JSON serialization
        /// </summary>
        [JsonPropertyName("out_path_len")]
        public int OutPathLength => OutboundRoute?.Path?.Length ?? -1;

        /// <summary>
        /// Gets the outbound route path as hex string for JSON serialization
        /// </summary>
        [JsonPropertyName("out_path")]
        public string OutPath => OutboundRoute?.Path is { Length: > 0 } pathBytes
            ? Convert.ToHexString(pathBytes).ToLowerInvariant()
            : string.Empty;

        /// <summary>
        /// Returns a JSON representation matching the MeshCore CLI output format.
        /// </summary>
        /// <returns>A JSON string describing the contact.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}