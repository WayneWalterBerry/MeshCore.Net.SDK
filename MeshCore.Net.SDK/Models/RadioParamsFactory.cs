// <copyright file="Channel.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Provides factory methods for creating standard radio parameter configurations.
    /// </summary>
    public static class RadioParamsFactory
    {
        /// <summary>
        /// Returns Puget Mesh radio parameters based on the standard configuration used by the Puget Mesh community.
        /// </summary>
        /// <remarks>
        /// Puget Mesh is a volunteer-led community dedicated to the deployment and support of off-grid communication
        /// networks—including AREDN, MeshCore, and Meshtastic across the Puget Sound region. Our mission is to provide 
        /// community-owned resilient digital infrastructure that serves as a vital tool for disaster 
        /// preparedness and emergency response.
        /// </remarks>
        /// <returns></returns>
        public static RadioParams PugetMesh()
        {
            return new RadioParams
            {
                FrequencyMHz = 910.525,
                BandwidthKHz = 62.5,
                SpreadingFactor = 7,
                CodingRate = 5
            };
        }
    }
}
