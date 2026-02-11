// <copyright file="RadioParams.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents LoRa radio parameters for a MeshCore device.
    /// Set via CMD_SET_RADIO_PARAMS (0x0B). A device reboot is required for changes to take effect.
    /// </summary>
    /// <remarks>
    /// Binary wire format (10 bytes total):
    /// <code>
    /// Offset  Type     Field
    /// 0-3     uint32   frequency in kHz (FrequencyMHz × 1000), little-endian
    /// 4-7     uint32   bandwidth in kHz (BandwidthKHz × 1000), little-endian
    /// 8       uint8    spreading factor (6–12)
    /// 9       uint8    coding rate (5–8)
    /// </code>
    /// </remarks>
    public sealed class RadioParams
    {
        /// <summary>
        /// Gets or sets the radio frequency in MHz (e.g., 910.525).
        /// </summary>
        [JsonPropertyName("radio_freq")]
        public double FrequencyMHz { get; set; }

        /// <summary>
        /// Gets or sets the bandwidth in kHz (e.g., 62.5, 125.0, 250.0, 500.0).
        /// </summary>
        [JsonPropertyName("radio_bw")]
        public double BandwidthKHz { get; set; }

        /// <summary>
        /// Gets or sets the LoRa spreading factor (typically 6–12).
        /// </summary>
        [JsonPropertyName("radio_sf")]
        public int SpreadingFactor { get; set; }

        /// <summary>
        /// Gets or sets the LoRa coding rate denominator (typically 5–8, representing 4/5 through 4/8).
        /// </summary>
        [JsonPropertyName("radio_cr")]
        public int CodingRate { get; set; }

        /// <summary>
        /// Returns a JSON representation of the radio parameters.
        /// </summary>
        /// <returns>A JSON string describing the radio parameters.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
