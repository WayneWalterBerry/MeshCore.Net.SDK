// <copyright file="RfPayloadType.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Protocol
{
    /// <summary>
    /// Represents the route type within an RF log data frame
    /// </summary>
    public enum RfRouteType : byte
    {
        /// <summary>Transport code flood</summary>
        TcFlood = 0x00,

        /// <summary>Standard flood</summary>
        Flood = 0x01,

        /// <summary>Direct routing</summary>
        Direct = 0x02,

        /// <summary>Transport code direct</summary>
        TcDirect = 0x03
    }
}
