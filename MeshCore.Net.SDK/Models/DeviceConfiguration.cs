// <copyright file="DeviceConfiguration.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents device configuration
    /// </summary>
    public class DeviceConfiguration
    {
        /// <summary>
        /// Gets or sets the device name
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the transmit power level
        /// </summary>
        public int TransmitPower { get; set; }

        /// <summary>
        /// Gets or sets the communication channel number
        /// </summary>
        public int Channel { get; set; }

        /// <summary>
        /// Gets or sets whether automatic message relaying is enabled
        /// </summary>
        public bool AutoRelay { get; set; }

        /// <summary>
        /// Gets or sets the interval between heartbeat messages
        /// </summary>
        public TimeSpan HeartbeatInterval { get; set; }

        /// <summary>
        /// Gets or sets the timeout for message delivery
        /// </summary>
        public TimeSpan MessageTimeout { get; set; }

        /// <summary>
        /// Gets or sets custom configuration settings
        /// </summary>
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }
}
