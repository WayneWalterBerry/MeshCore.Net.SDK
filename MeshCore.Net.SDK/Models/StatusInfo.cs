// <copyright file="StatusInfo.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a high-level status snapshot for a MeshCore node (typically a repeater or room server).
    /// This aggregates key health metrics such as uptime, battery level, queue depth, and basic radio information.
    /// </summary>
    public sealed class StatusInfo
    {
        /// <summary>
        /// Gets or sets the node name as reported by the remote device.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the public key prefix (hex) of the node reporting this status, if available.
        /// </summary>
        [JsonPropertyName("pubkey_prefix")]
        public string PublicKeyPrefix { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the firmware version string reported by the device.
        /// </summary>
        [JsonPropertyName("fw_ver")]
        public string FirmwareVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the total uptime in seconds since the device last rebooted.
        /// </summary>
        [JsonPropertyName("uptime_secs")]
        public uint UptimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the battery level in millivolts, if provided by the device.
        /// </summary>
        [JsonPropertyName("battery_mv")]
        public int BatteryMillivolts { get; set; }

        /// <summary>
        /// Gets or sets the number of messages currently queued for transmission on the device.
        /// </summary>
        [JsonPropertyName("tx_queue_depth")]
        public int TxQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the total number of packets transmitted by the device.
        /// </summary>
        [JsonPropertyName("tx_packets")]
        public uint TxPackets { get; set; }

        /// <summary>
        /// Gets or sets the total number of packets received by the device.
        /// </summary>
        [JsonPropertyName("rx_packets")]
        public uint RxPackets { get; set; }

        /// <summary>
        /// Gets or sets the most recently measured noise floor in dB, if available.
        /// </summary>
        [JsonPropertyName("noise_floor_db")]
        public double NoiseFloorDb { get; set; }

        /// <summary>
        /// Gets or sets the most recently measured average SNR in dB, if available.
        /// </summary>
        [JsonPropertyName("avg_snr_db")]
        public double AverageSnrDb { get; set; }

        /// <summary>
        /// Gets or sets the current radio frequency in Hz.
        /// </summary>
        [JsonPropertyName("radio_freq_hz")]
        public uint RadioFrequencyHz { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the device is currently repeating traffic (repeater role enabled).
        /// </summary>
        [JsonPropertyName("repeat_enabled")]
        public bool RepeatEnabled { get; set; }

        /// <summary>
        /// Gets a value indicating whether the transmit queue is considered healthy (depth == 0).
        /// </summary>
        [JsonIgnore]
        public bool IsQueueHealthy => TxQueueDepth == 0;

        /// <summary>
        /// Gets a value indicating whether the node has been up for less than one hour.
        /// </summary>
        [JsonIgnore]
        public bool IsRecentlyBooted => UptimeSeconds < 3600;

        /// <summary>
        /// Gets a value indicating whether the battery level is considered low (below 3.4 V).
        /// </summary>
        [JsonIgnore]
        public bool IsBatteryLow => BatteryMillivolts > 0 && BatteryMillivolts < 3400;

        /// <summary>
        /// Gets a human-readable description of the device uptime.
        /// </summary>
        [JsonIgnore]
        public string UptimeDescription
        {
            get
            {
                if (UptimeSeconds < 60)
                {
                    return $"{UptimeSeconds}s";
                }

                if (UptimeSeconds < 3600)
                {
                    return $"{UptimeSeconds / 60}m";
                }

                if (UptimeSeconds < 86400)
                {
                    return $"{UptimeSeconds / 3600}h";
                }

                return $"{UptimeSeconds / 86400}d";
            }
        }

        /// <summary>
        /// Returns a JSON representation of the status information.
        /// </summary>
        /// <returns>A JSON string describing the status information.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}