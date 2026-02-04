// <copyright file="BatteryAndStorageSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization;

using System;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Protocol;

/// <summary>
/// Handles serialization and deserialization of battery and storage information
/// </summary>
internal class BatteryAndStorageSerialization : IBinaryDeserializer<BatteryAndStorage>
{
    /// <summary>
    /// Gets the singleton instance of BatteryAndStorageSerialization
    /// </summary>
    public static BatteryAndStorageSerialization Instance { get; } = new();

    /// <summary>
    /// Deserializes a byte array to BatteryAndStorage object
    /// </summary>
    /// <param name="data">The byte array to deserialize</param>
    /// <returns>The deserialized BatteryAndStorage object</returns>
    /// <exception cref="ArgumentException">Thrown when data format is invalid</exception>
    public BatteryAndStorage Deserialize(byte[] data)
    {
        if (TryDeserialize(data, out var result))
        {
            return result!;
        }

        throw new ArgumentException("Invalid battery and storage data format", nameof(data));
    }

    /// <summary>
    /// Attempts to deserialize battery and storage information from MeshCore device response
    /// Based on C++ implementation: RESP_CODE + battery_millivolts(2) + used(4) + total(4) = 11 bytes
    /// </summary>
    /// <param name="data">The byte array containing the response payload</param>
    /// <param name="result">The deserialized BatteryAndStorage object if successful</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    public bool TryDeserialize(byte[] data, out BatteryAndStorage? result)
    {
        result = null;

        // Validate minimum data length: 1 byte response code + 10 bytes data = 11 bytes
        if (data == null || data.Length < 11)
        {
            return false;
        }

        try
        {
            var offset = 0;

            // Verify response code (first byte should be RESP_CODE_BATT_AND_STORAGE = 12)
            if (data[offset] != (byte)MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE)
            {
                return false;
            }
            offset++;

            // Parse battery voltage (2 bytes, little-endian uint16_t)
            var batteryVoltage = BitConverter.ToUInt16(data, offset);
            offset += 2;

            // Parse used storage (4 bytes, little-endian uint32_t)
            var usedStorage = BitConverter.ToUInt32(data, offset);
            offset += 4;

            // Parse total storage (4 bytes, little-endian uint32_t)
            var totalStorage = BitConverter.ToUInt32(data, offset);

            // Create result object
            result = new BatteryAndStorage
            {
                BatteryVoltage = batteryVoltage,
                UsedStorage = usedStorage,
                TotalStorage = totalStorage
            };

            return true;
        }
        catch (Exception)
        {
            // Handle any parsing errors (IndexOutOfRangeException, etc.)
            result = null;
            return false;
        }
    }
}