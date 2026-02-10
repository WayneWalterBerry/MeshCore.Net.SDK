// <copyright file="LogRxResponsePayload.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MeshCore.Net.SDK.Json;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Represents a MeshCore response payload embedded within an RF log data frame
    /// This occurs when RF packets contain MeshCore protocol responses
    /// </summary>
    /// <remarks>
    /// Response Payload Structure:
    /// ├─ Byte 0: MeshCore Response Code (e.g., 0x8D for PATH_RESPONSE)
    /// └─ Bytes 1+: Response-specific data (varies by response code)
    /// 
    /// Common response codes seen in RF logs:
    /// - 0x8D: RESP_CODE_PATH_RESPONSE (path discovery responses)
    /// - 0x04: RESP_CODE_ADVERT (advertisement packets)
    /// - 0x05: Channel/group messages
    /// </remarks>
    public class LogRxResponsePayload
    {
        /// <summary>
        /// Gets or sets the MeshCore response code
        /// This is the first byte of the response payload
        /// </summary>
        [JsonPropertyName("response_code")]
        public byte ResponseCodeByte { get; set; }

        /// <summary>
        /// Gets the MeshCore response code as a strongly-typed enum
        /// </summary>
        [JsonIgnore]
        public MeshCoreResponseCode? ResponseCode => Enum.IsDefined(typeof(MeshCoreResponseCode), ResponseCodeByte)
            ? (MeshCoreResponseCode)ResponseCodeByte
            : null;

        /// <summary>
        /// Gets or sets the response-specific data following the response code
        /// The structure and meaning of this data varies by response code
        /// </summary>
        [JsonPropertyName("data")]
        [JsonConverter(typeof(ByteArrayHexConverter))]
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets the total length of the response payload
        /// </summary>
        [JsonPropertyName("payload_length")]
        public int PayloadLength => 1 + Payload.Length; // Response code + data

        /// <summary>
        /// Gets whether this response code is recognized as a valid MeshCore response
        /// </summary>
        [JsonIgnore]
        public bool IsKnownResponseCode => ResponseCode.HasValue;

        /// <summary>
        /// Returns a descriptive string representation of the response payload
        /// </summary>
        /// <returns>A string describing the response payload</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}