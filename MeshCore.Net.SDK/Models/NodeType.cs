// <copyright file="Advertisement.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Defines the possible node types for MeshCore advertisements
    /// Based on the ADV_TYPE values from the C++ code
    /// </summary>
    public enum NodeType : byte
    {
        /// <summary>
        /// Indicates that the value is unknown or has not been specified.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Chat/Companion device (regular user device)
        /// </summary>
        Chat = 1,

        /// <summary>
        /// Repeater device (extends network range)
        /// </summary>
        Repeater = 2,

        /// <summary>
        /// Room server (provides chat room services)
        /// </summary>
        RoomServer = 3,

        /// <summary>
        /// Sensor device (collects and reports sensor data)
        /// </summary>
        Sensor = 4
    }
}    
