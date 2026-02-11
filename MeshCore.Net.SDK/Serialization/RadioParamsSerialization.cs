// <copyright file="RadioParamsSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles serialization of <see cref="RadioParams"/> into the binary payload
    /// expected by CMD_SET_RADIO_PARAMS (0x0B).
    /// </summary>
    /// <remarks>
    /// Wire format (10 bytes):
    /// <code>
    /// Offset  Type     Field
    /// 0-3     uint32   frequency in kHz (FrequencyMHz × 1000), little-endian
    /// 4-7     uint32   bandwidth in kHz (BandwidthKHz × 1000), little-endian
    /// 8       uint8    spreading factor (6–12)
    /// 9       uint8    coding rate (5–8)
    /// </code>
    /// Matches the Python reference:
    /// <c>int(float(freq) * 1000).to_bytes(4, "little") + int(float(bw) * 1000).to_bytes(4, "little")
    /// + int(sf).to_bytes(1, "little") + int(cr).to_bytes(1, "little")</c>
    /// </remarks>
    internal sealed class RadioParamsSerialization : IBinarySerializer<RadioParams>
    {
        /// <summary>
        /// Total payload length: freq(4) + bw(4) + sf(1) + cr(1).
        /// </summary>
        private const int PAYLOAD_LENGTH = 10;

        private static readonly Lazy<RadioParamsSerialization> _instance = new(() => new RadioParamsSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="RadioParamsSerialization"/> class.
        /// </summary>
        public static RadioParamsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private RadioParamsSerialization()
        {
        }

        /// <summary>
        /// Serializes a <see cref="RadioParams"/> object into the binary payload for CMD_SET_RADIO_PARAMS.
        /// </summary>
        /// <param name="obj">The radio parameters to serialize.</param>
        /// <returns>A 10-byte array containing the serialized radio parameters.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
        public byte[] Serialize(RadioParams obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var freqKhz = (uint)(obj.FrequencyMHz * 1000);
            var bwKhz = (uint)(obj.BandwidthKHz * 1000);

            var payload = new byte[PAYLOAD_LENGTH];
            BitConverter.GetBytes(freqKhz).CopyTo(payload, 0);
            BitConverter.GetBytes(bwKhz).CopyTo(payload, 4);
            payload[8] = (byte)obj.SpreadingFactor;
            payload[9] = (byte)obj.CodingRate;

            return payload;
        }
    }
}
