// <copyright file="DeviceInfo.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a MeshCore device information
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// Gets or sets the unique device identifier
        /// </summary>
        [JsonPropertyName("device_id")]
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the firmware version
        /// </summary>
        [JsonPropertyName("firmware_version")]
        public string? FirmwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the hardware version
        /// </summary>
        [JsonPropertyName("hardware_version")]
        public string? HardwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the device serial number
        /// </summary>
        [JsonPropertyName("serial_number")]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of contacts that can be stored.
        /// </summary>
        [JsonPropertyName("max_contacts")]
        public int MaxContacts { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of group channels that can be created.
        /// </summary>
        [JsonPropertyName("max_group_channels")]
        public int MaxGroupChannels { get; set; }

        /// <summary>
        /// Returns a JSON representation of the device information.
        /// </summary>
        /// <returns>A JSON string describing the device.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}