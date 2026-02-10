// <copyright file="LogRxDataFrame.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MeshCore.Net.SDK.Json;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Represents an RF log data frame received from the MeshCore device
    /// Contains signal quality metrics and the raw payload data
    /// </summary>
    /// <remarks>
    /// RESP_CODE_LOG_RX_DATA Frame:
    /// ├─ Byte 0: 0x88 (RESP_CODE_LOG_RX_DATA marker)
    /// ├─ Byte 1: SNR(Signal-to-Noise Ratio)
    /// ├─ Byte 2: RSSI(Received Signal Strength Indicator)
    /// └─ Bytes 3+: RF Packet Payload
    ///             ├─ Byte 0: PACKET HEADER(not a response code!)
    ///             │          ├─ Bits 0-1: Route Type
    ///             │          ├─ Bits 2-5: Payload Type
    ///             │          └─ Bits 6-7: Payload Version
    ///             ├─ Transport Code(if route_type needs it)
    ///             ├─ Path Length
    ///             ├─ Path bytes
    ///             └─ Actual packet payload
    /// </remarks>
    public class LogRxDataFrame
    {
        /// <summary>
        /// Gets or sets the Signal-to-Noise Ratio (SNR) in decibels
        /// Higher values indicate better signal quality
        /// </summary>
        [JsonPropertyName("snr")]
        public sbyte SignalToNoiseRatio { get; set; }

        /// <summary>
        /// Gets or sets the Received Signal Strength Indicator (RSSI) in dBm
        /// Values closer to 0 indicate stronger signal strength
        /// </summary>
        [JsonPropertyName("rssi")]
        public sbyte ReceivedSignalStrength { get; set; }

        /// <summary>
        /// Gets or sets the raw RF packet payload following the SNR/RSSI headers
        /// This contains the actual RF packet that was received over the air
        /// </summary>
        [JsonPropertyName("payload")]
        [JsonConverter(typeof(ByteArrayHexConverter))]
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets the packet header byte containing route type, payload type, and version
        /// This is the FIRST byte of the RF packet payload, NOT a MeshCore response code
        /// </summary>
        [JsonIgnore]
        public byte? PacketHeader => Payload.Length > 0 ? Payload[0] : null;

        /// <summary>
        /// Gets the route type from the packet header (bits 0-1)
        /// </summary>
        [JsonIgnore]
        public RfRouteType? RouteType => PacketHeader.HasValue
            ? (RfRouteType)(PacketHeader.Value & 0x03)
            : null;

        /// <summary>
        /// Gets the payload type from the packet header (bits 2-5)
        /// </summary>
        [JsonIgnore]
        public RfPayloadType? PayloadType => PacketHeader.HasValue
            ? (RfPayloadType)((PacketHeader.Value & 0x3C) >> 2)
            : null;

        /// <summary>
        /// Gets the payload version from the packet header (bits 6-7)
        /// </summary>
        [JsonIgnore]
        public byte? PayloadVersion => PacketHeader.HasValue
            ? (byte)((PacketHeader.Value & 0xC0) >> 6)
            : null;

        /// <summary>
        /// Gets whether this route type includes a transport code (4 bytes after header)
        /// TC_FLOOD (0x00) and TC_DIRECT (0x03) include transport codes
        /// </summary>
        [JsonIgnore]
        public bool HasTransportCode => RouteType == RfRouteType.TcFlood || RouteType == RfRouteType.TcDirect;

        /// <summary>
        /// Gets the total length of the RF log frame including headers
        /// </summary>
        [JsonPropertyName("frame_length")]
        public int FrameLength => 3 + Payload.Length; // RX_LOG_DATA marker + SNR + RSSI + payload

        /// <summary>
        /// Returns a descriptive string representation of the RF log frame
        /// </summary>
        /// <returns>A string describing the log frame</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}