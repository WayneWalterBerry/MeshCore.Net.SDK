// <copyright file="DeviceInfo.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

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
        public string? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the firmware version
        /// </summary>
        public string? FirmwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the hardware version
        /// </summary>
        public string? HardwareVersion { get; set; }

        /// <summary>
        /// Gets or sets the device serial number
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of contacts that can be stored.
        /// </summary>
        public int MaxContacts { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of group channels that can be created.
        /// </summary>
        public int MaxGroupChannels { get; set; }
    }
}
