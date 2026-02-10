// <copyright file="RemoteCommandResponseFrame.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a parsed view of a remote command response frame payload received via
    /// <c>CMD_SEND_TXT_MSG</c> from a repeater or room server node.
    /// </summary>
    /// <remarks>
    /// The underlying frame payload layout used by the firmware is:
    /// <code>
    /// [0]        = RESP_CODE
    /// [1..7]     = sender_pubkey_prefix (7 bytes)
    /// [8]        = txt_type
    /// [9..12]    = timestamp (uint32, little-endian, Unix seconds)
    /// [13..N-1]  = data (command-specific binary payload)
    /// </code>
    /// This model exposes the decoded header fields and the remaining binary data region.
    /// </remarks>
    public sealed class RemoteCommandResponseFrame
    {
        /// <summary>
        /// Gets or sets the MeshCore protocol response code byte at offset 0 of the payload.
        /// </summary>
        [JsonPropertyName("resp_code")]
        public byte ResponseCode { get; set; }

        /// <summary>
        /// Gets or sets the 7-byte sender public key prefix as a lowercase hexadecimal string.
        /// </summary>
        [JsonPropertyName("sender_pubkey_prefix")]
        public string SenderPublicKeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the text type byte from the payload (for example, 0x01 for CLI command responses).
        /// </summary>
        [JsonPropertyName("txt_type")]
        public byte TextType { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp (seconds since epoch) embedded in the frame header.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public uint Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the raw binary data portion following the header.
        /// </summary>
        [JsonPropertyName("data")]
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets a value indicating whether the frame contains any command-specific data bytes.
        /// </summary>
        [JsonIgnore]
        public bool HasData => Payload.Length > 0;

        /// <summary>
        /// Returns a JSON representation of this remote command response frame.
        /// </summary>
        /// <returns>A JSON string describing the frame.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}