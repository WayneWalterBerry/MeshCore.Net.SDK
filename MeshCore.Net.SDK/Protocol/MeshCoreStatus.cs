// <copyright file="MeshCoreResponseCode.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Protocol
{
    /// <summary>
    /// MeshCore response status codes
    /// </summary>
    public enum MeshCoreStatus : byte
    {
        /// <summary>
        /// Operation completed successfully
        /// </summary>
        Success = 0x00,

        /// <summary>
        /// Command is not recognized or invalid
        /// </summary>
        InvalidCommand = 0x01,

        /// <summary>
        /// Parameter provided is invalid
        /// </summary>
        InvalidParameter = 0x02,

        /// <summary>
        /// Device encountered an error
        /// </summary>
        DeviceError = 0x03,

        /// <summary>
        /// Network communication error occurred
        /// </summary>
        NetworkError = 0x04,

        /// <summary>
        /// Operation timed out
        /// </summary>
        TimeoutError = 0x05,

        /// <summary>
        /// Unknown or unspecified error
        /// </summary>
        UnknownError = 0xFF
    }
}