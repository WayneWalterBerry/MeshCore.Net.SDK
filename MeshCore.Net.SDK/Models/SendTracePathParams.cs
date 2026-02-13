// <copyright file="SendTracePathParams.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the parameters for a CMD_SEND_TRACE_PATH (0x24) command.
    /// A trace packet is sent through the specified path of repeater nodes,
    /// collecting hop-by-hop SNR measurements along the way.
    /// </summary>
    /// <remarks>
    /// Binary wire format (9 + path_len bytes):
    /// <code>
    /// Offset   Type     Field
    /// 0-3      uint32   tag – random identifier for correlating the response (little-endian)
    /// 4-7      uint32   auth_code – authentication code (little-endian)
    /// 8        uint8    flags
    /// 9..N     bytes    path – one byte per hop (first byte of each repeater's public key)
    /// </code>
    /// Matches the Python reference:
    /// <c>tag.to_bytes(4, "little") + auth_code.to_bytes(4, "little") + flags.to_bytes(1, "little") + path_bytes</c>
    /// </remarks>
    public sealed class SendTracePathParams
    {
        /// <summary>
        /// Gets or sets the 32-bit tag used to correlate the trace request with its
        /// <c>PUSH_CODE_TRACE_DATA (0x89)</c> response.
        /// </summary>
        [JsonPropertyName("tag")]
        public uint Tag { get; set; }

        /// <summary>
        /// Gets or sets the 32-bit authentication code sent with the trace packet.
        /// Set to 0 when authentication is not required.
        /// </summary>
        [JsonPropertyName("auth_code")]
        public uint AuthCode { get; set; }

        /// <summary>
        /// Gets or sets the flags byte for the trace packet.
        /// </summary>
        [JsonPropertyName("flags")]
        public byte Flags { get; set; }

        /// <summary>
        /// Gets or sets the path bytes representing the repeater hops.
        /// Each byte is the first byte of a repeater's public key, defining
        /// the route the trace packet should follow.
        /// </summary>
        [JsonPropertyName("path")]
        public byte[] Path { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Returns a JSON representation of the trace path parameters.
        /// </summary>
        /// <returns>A JSON string describing the trace path parameters.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
