// <copyright file="ContactFlags.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Defines per-contact flags as provided by the MeshCore firmware.
    /// </summary>
    [Flags]
    public enum ContactFlags : byte
    {
        /// <summary>
        /// No flags are set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that this contact is marked as a favorite.
        /// Corresponds to the least significant bit (bit 0) of contact.flags.
        /// </summary>
        Favourite = 1 << 0,

        /// <summary>
        /// Indicates that base telemetry (eg. battery, basic status) is permitted for this contact.
        /// Maps to the firmware TELEM_PERM_BASE bit, stored starting at bit 1 of contact.flags.
        /// </summary>
        TelemetryBase = 1 << 1,

        /// <summary>
        /// Indicates that location telemetry is permitted for this contact.
        /// Maps to the firmware TELEM_PERM_LOCATION bit, stored starting at bit 1 of contact.flags.
        /// </summary>
        TelemetryLocation = 1 << 2,

        /// <summary>
        /// Indicates that environment telemetry is permitted for this contact.
        /// Maps to the firmware TELEM_PERM_ENVIRONMENT bit, stored starting at bit 1 of contact.flags.
        /// </summary>
        TelemetryEnvironment = 1 << 3,
    }
}