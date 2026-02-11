// <copyright file="RadioStats.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents radio performance statistics from a MeshCore companion device.
    /// Retrieved via CMD_GET_STATS with STATS_TYPE_RADIO (0x01).
    /// </summary>
    /// <remarks>
    /// Binary wire format (14 bytes total):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    RESP_CODE_STATS (0x18)
    /// 1       uint8    stats_type (0x01 = radio)
    /// 2-3     int16    noise_floor (dBm, little-endian)
    /// 4       int8     last_rssi (dBm)
    /// 5       int8     last_snr_scaled (SNR × 4)
    /// 6-9     uint32   tx_air_secs (little-endian)
    /// 10-13   uint32   rx_air_secs (little-endian)
    /// </code>
    /// </remarks>
    public sealed class RadioStats
    {
        /// <summary>
        /// Gets or sets the most recently measured noise floor in dBm.
        /// </summary>
        [JsonPropertyName("noise_floor")]
        public short NoiseFloor { get; set; }

        /// <summary>
        /// Gets or sets the RSSI of the last received packet in dBm.
        /// </summary>
        [JsonPropertyName("last_rssi")]
        public sbyte LastRssi { get; set; }

        /// <summary>
        /// Gets or sets the SNR of the last received packet in dB.
        /// The firmware transmits this value multiplied by 4; this property stores the unscaled value.
        /// </summary>
        [JsonPropertyName("last_snr")]
        public double LastSnr { get; set; }

        /// <summary>
        /// Gets or sets the cumulative number of seconds the radio has spent transmitting.
        /// </summary>
        [JsonPropertyName("tx_air_secs")]
        public uint TxAirSeconds { get; set; }

        /// <summary>
        /// Gets or sets the cumulative number of seconds the radio has spent receiving.
        /// </summary>
        [JsonPropertyName("rx_air_secs")]
        public uint RxAirSeconds { get; set; }

        /// <summary>
        /// Returns a JSON representation of the radio statistics.
        /// </summary>
        /// <returns>A JSON string describing the radio statistics.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
