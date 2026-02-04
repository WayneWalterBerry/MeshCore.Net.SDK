// <copyright file="BatteryAndStorage.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents battery voltage and storage usage information for a device.
    /// </summary>
    /// <remarks>This class provides properties to monitor the current battery voltage and track both used and
    /// total storage capacity. It can be used to assess device health or resource availability in scenarios such as
    /// embedded systems or IoT devices.</remarks>
    public class BatteryAndStorage
    {
        /// <summary>
        /// Gets or sets the battery voltage in millivolts
        /// </summary>
        public ushort BatteryVoltage { get; set; }

        /// <summary>
        /// Gets or sets the used storage space in kilobytes
        /// </summary>
        public uint UsedStorage { get; set; }

        /// <summary>
        /// Gets or sets the total storage capacity in kilobytes
        /// </summary>
        public uint TotalStorage { get; set; }
    }
}
