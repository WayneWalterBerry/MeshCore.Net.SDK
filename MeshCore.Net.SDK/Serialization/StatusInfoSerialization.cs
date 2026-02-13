// <copyright file="StatusInfoSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of binary status response payloads from remote repeater or
    /// room server nodes into <see cref="StatusInfo"/> objects.
    /// </summary>
    /// <remarks>
    /// Binary wire format (PUSH_CODE_STATUS_RESPONSE 0x87):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    PUSH_CODE_STATUS_RESPONSE (0x87)
    /// 1       uint8    reserved
    /// 2-7     bytes    pubkey_prefix (6 bytes)
    /// ── fields begin at offset 8 ──
    /// 8-9     uint16   battery (mV, little-endian)
    /// 10-11   int16    tx_queue_len (little-endian)
    /// 12-13   int16    noise_floor (dBm, little-endian, signed)
    /// 14-15   int16    last_rssi (dBm, little-endian, signed)
    /// 16-19   uint32   nb_recv (little-endian)
    /// 20-23   uint32   nb_sent (little-endian)
    /// 24-27   uint32   airtime / tx_air_secs (little-endian)
    /// 28-31   uint32   uptime (seconds, little-endian)
    /// 32-35   uint32   sent_flood (little-endian)
    /// 36-39   uint32   sent_direct (little-endian)
    /// 40-43   uint32   recv_flood (little-endian)
    /// 44-47   uint32   recv_direct (little-endian)
    /// 48-49   uint16   full_evts (little-endian)
    /// 50-51   int16    last_snr_scaled (SNR × 4, little-endian, signed)
    /// 52-53   uint16   direct_dups (little-endian)
    /// 54-55   uint16   flood_dups (little-endian)
    /// 56-59   uint32   rx_airtime / rx_air_secs (little-endian)
    /// </code>
    /// Matches the Python reference <c>parsing.py:parse_status()</c>.
    /// </remarks>
    internal sealed class StatusInfoSerialization : IBinaryDeserializer<StatusInfo>
    {
        /// <summary>
        /// Minimum payload length: 8 header bytes + 52 field bytes.
        /// </summary>
        private const int MIN_PAYLOAD_LENGTH = 60;

        /// <summary>
        /// Offset where the binary fields begin (after response code, reserved byte, and pubkey prefix).
        /// </summary>
        private const int FIELD_OFFSET = 8;

        private static readonly Lazy<StatusInfoSerialization> _instance = new(() => new StatusInfoSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="StatusInfoSerialization"/> class.
        /// </summary>
        public static StatusInfoSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private StatusInfoSerialization()
        {
        }

        /// <summary>
        /// Deserializes a status response payload to a <see cref="StatusInfo"/> object.
        /// </summary>
        /// <param name="data">The byte array containing the status response data.</param>
        /// <returns>The deserialized <see cref="StatusInfo"/> object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
        public StatusInfo Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize status info from binary data.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a binary status snapshot from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized status response data.</param>
        /// <param name="result">The resulting <see cref="StatusInfo"/> when deserialization succeeds; otherwise, null.</param>
        /// <returns><see langword="true"/> if the status was successfully deserialized; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The payload follows the binary format defined by the MeshCore firmware for
        /// PUSH_CODE_STATUS_RESPONSE (0x87). See the class remarks for the full wire layout.
        /// </remarks>
        public bool TryDeserialize(byte[] data, out StatusInfo? result)
        {
            result = null;

            if (data == null || data.Length < MIN_PAYLOAD_LENGTH)
            {
                return false;
            }

            try
            {
                // Extract pubkey prefix (bytes 2-7)
                var pubkeyPrefix = Convert.ToHexString(data, 2, 6);

                int o = FIELD_OFFSET;

                var status = new StatusInfo
                {
                    PublicKeyPrefix = pubkeyPrefix,
                    BatteryMillivolts = BitConverter.ToUInt16(data, o),          // offset+0
                    TxQueueDepth = BitConverter.ToInt16(data, o + 2),            // offset+2
                    NoiseFloorDb = BitConverter.ToInt16(data, o + 4),            // offset+4
                    AverageSnrDb = BitConverter.ToInt16(data, o + 42) / 4.0,     // offset+42 (last_snr_scaled)
                    RxPackets = BitConverter.ToUInt32(data, o + 8),              // offset+8
                    TxPackets = BitConverter.ToUInt32(data, o + 12),             // offset+12
                    UptimeSeconds = BitConverter.ToUInt32(data, o + 20),         // offset+20
                };

                result = status;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}