// <copyright file="AutoAddConfigFlags.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents the firmware <c>_prefs.autoadd_config</c> bitmask that controls
    /// how newly discovered contacts are automatically added and how capacity
    /// exhaustion is handled.
    /// </summary>
    [Flags]
    public enum AutoAddConfigFlags : byte
    {
        /// <summary>
        /// No auto-add configuration flags are set.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// If set, the device will overwrite the oldest non-favourite contact when
        /// the contacts table is full in order to store new auto-added contacts.
        /// Maps to <c>AUTO_ADD_OVERWRITE_OLDEST</c> in firmware.
        /// </summary>
        OverwriteOldest = 0x01,

        /// <summary>
        /// Auto-add Chat (Companion) contacts when they are discovered.
        /// Maps to <c>AUTO_ADD_CHAT</c> in firmware.
        /// </summary>
        Chat = 0x02,

        /// <summary>
        /// Auto-add Repeater contacts when they are discovered.
        /// Maps to <c>AUTO_ADD_REPEATER</c> in firmware.
        /// </summary>
        Repeater = 0x04,

        /// <summary>
        /// Auto-add Room Server contacts when they are discovered.
        /// Maps to <c>AUTO_ADD_ROOM_SERVER</c> in firmware.
        /// </summary>
        RoomServer = 0x08,

        /// <summary>
        /// Auto-add Sensor contacts when they are discovered.
        /// Maps to <c>AUTO_ADD_SENSOR</c> in firmware.
        /// </summary>
        Sensor = 0x10
    }
}