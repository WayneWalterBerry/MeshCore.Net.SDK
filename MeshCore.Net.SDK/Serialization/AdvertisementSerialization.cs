// <copyright file="AdvertisementSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using MeshCore.Net.SDK.Models;

namespace MeshCore.Net.SDK.Serialization;

/// <summary>
/// Handles serialization of advertisement commands for CMD_SEND_SELF_ADVERT
/// Based on the MyMesh.cpp implementation
/// </summary>
internal class AdvertisementSerialization : IBinarySerializer<Advertisement>
{
    private static readonly Lazy<AdvertisementSerialization> _instance = new(() => new AdvertisementSerialization());

    /// <summary>
    /// Gets the singleton instance of the AdvertisementSerialization
    /// </summary>
    public static AdvertisementSerialization Instance => _instance.Value;

    /// <summary>
    /// Prevents external instantiation of the singleton
    /// </summary>
    private AdvertisementSerialization()
    {
    }

    /// <summary>
    /// Serializes an Advertisement object to the payload format expected by CMD_SEND_SELF_ADVERT
    /// Based on MyMesh.cpp: CMD_SEND_SELF_ADVERT payload format:
    /// - No payload = zero hop (local broadcast only)  
    /// - Payload with byte 0x01 = flood mode (network-wide via repeaters)
    /// - Payload with byte 0x00 = zero hop (explicit)
    /// </summary>
    /// <param name="obj">The advertisement configuration to serialize</param>
    /// <returns>Serialized byte array for the command payload</returns>
    public byte[] Serialize(Advertisement obj)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        // According to MyMesh.cpp, the CMD_SEND_SELF_ADVERT payload is very simple:
        // - 0x01 for flood mode (network-wide broadcast via repeaters)
        // - 0x00 for zero-hop mode (local broadcast only)
        // 
        // The actual advertisement content (device name, location, node type) is handled
        // by the device firmware based on its current configuration, not passed in the payload.

        return new byte[] { (byte)(obj.UseFloodMode ? 0x01 : 0x00) };
    }
}