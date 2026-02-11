// <copyright file="RadioStatsSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Handles deserialization of CMD_GET_STATS radio statistics responses (STATS_TYPE_RADIO = 0x01)
    /// into <see cref="RadioStats"/> objects.
    /// </summary>
    /// <remarks>
    /// Expected binary wire format (14 bytes):
    /// <code>
    /// [0]      RESP_CODE_STATS (0x18)
    /// [1]      stats_type      (0x01 = radio)
    /// [2-3]    noise_floor     (int16, little-endian, dBm)
    /// [4]      last_rssi       (int8, dBm)
    /// [5]      last_snr_scaled (int8, SNR × 4)
    /// [6-9]    tx_air_secs     (uint32, little-endian)
    /// [10-13]  rx_air_secs     (uint32, little-endian)
    /// </code>
    /// </remarks>
    internal sealed class RadioStatsSerialization : IBinaryDeserializer<RadioStats>
    {
        /// <summary>
        /// The expected stats sub-type value for radio statistics.
        /// </summary>
        private const byte STATS_TYPE_RADIO = 0x01;

        /// <summary>
        /// Minimum payload length required for a valid radio stats response.
        /// </summary>
        private const int MIN_PAYLOAD_LENGTH = 14;

        private static readonly Lazy<RadioStatsSerialization> _instance = new(() => new RadioStatsSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="RadioStatsSerialization"/> class.
        /// </summary>
        public static RadioStatsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private RadioStatsSerialization()
        {
        }

        /// <summary>
        /// Deserializes a radio stats response payload to a <see cref="RadioStats"/> object.
        /// </summary>
        /// <param name="data">The byte array containing the radio stats response data.</param>
        /// <returns>The deserialized <see cref="RadioStats"/> object.</returns>
        /// <exception cref="ArgumentException">Thrown when the data format is invalid.</exception>
        public RadioStats Deserialize(byte[] data)
        {
            if (TryDeserialize(data, out var result) && result != null)
            {
                return result;
            }

            throw new ArgumentException("Invalid radio stats data format", nameof(data));
        }

        /// <summary>
        /// Attempts to deserialize a radio stats response from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized radio stats response data.</param>
        /// <param name="result">The resulting <see cref="RadioStats"/> when deserialization succeeds; otherwise, null.</param>
        /// <returns><see langword="true"/> if the radio stats were successfully deserialized; otherwise, <see langword="false"/>.</returns>
        public bool TryDeserialize(byte[] data, out RadioStats? result)
        {
            result = null;

            if (data == null || data.Length < MIN_PAYLOAD_LENGTH)
            {
                return false;
            }

            try
            {
                var offset = 0;

                // Verify response code
                if (data[offset] != (byte)MeshCoreResponseCode.RESP_CODE_STATS)
                {
                    return false;
                }

                offset++;

                // Verify stats sub-type
                if (data[offset] != STATS_TYPE_RADIO)
                {
                    return false;
                }

                offset++;

                // Parse noise_floor (int16, little-endian)
                var noiseFloor = BitConverter.ToInt16(data, offset);
                offset += 2;

                // Parse last_rssi (int8)
                var lastRssi = (sbyte)data[offset];
                offset++;

                // Parse last_snr_scaled (int8, value is SNR × 4)
                var lastSnrScaled = (sbyte)data[offset];
                offset++;

                // Parse tx_air_secs (uint32, little-endian)
                var txAirSecs = BitConverter.ToUInt32(data, offset);
                offset += 4;

                // Parse rx_air_secs (uint32, little-endian)
                var rxAirSecs = BitConverter.ToUInt32(data, offset);

                result = new RadioStats
                {
                    NoiseFloor = noiseFloor,
                    LastRssi = lastRssi,
                    LastSnr = lastSnrScaled / 4.0,
                    TxAirSeconds = txAirSecs,
                    RxAirSeconds = rxAirSecs
                };

                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
    }
}
