// <copyright file="DeviceInfoSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Provides serialization and deserialization support for <see cref="DeviceInfo"/> objects
    /// using the binary payload format emitted by the MeshCore firmware in response to
    /// <c>CMD_DEVICE_QUERY</c> (<c>RESP_CODE_DEVICE_INFO</c>).
    /// </summary>
    internal sealed class DeviceInfoSerialization : IBinaryDeserializer<DeviceInfo>
    {
        private static readonly Lazy<DeviceInfoSerialization> _instance = new(() => new DeviceInfoSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="DeviceInfoSerialization"/> class.
        /// </summary>
        public static DeviceInfoSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private DeviceInfoSerialization()
        {
        }

        /// <summary>
        /// Deserializes a <see cref="DeviceInfo"/> instance from the specified binary payload.
        /// </summary>
        /// <param name="data">The binary payload returned by the firmware for <c>RESP_CODE_DEVICE_INFO</c>.</param>
        /// <returns>A fully populated <see cref="DeviceInfo"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the payload does not contain a valid device info structure.
        /// </exception>
        public DeviceInfo Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize device info from binary data.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a <see cref="DeviceInfo"/> instance from the specified binary payload.
        /// </summary>
        /// <param name="data">The binary payload returned by the firmware for <c>RESP_CODE_DEVICE_INFO</c>.</param>
        /// <param name="result">
        /// When this method returns, contains the deserialized <see cref="DeviceInfo"/> instance if the operation
        /// succeeded; otherwise, <see langword="null"/>.
        /// </param>
        /// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
        public bool TryDeserialize(byte[] data, out DeviceInfo? result)
        {
            result = default;

            if (data == null)
            {
                return false;
            }

            // Payload layout observed from firmware:
            //   [0]   = RESP_CODE_DEVICE_INFO (13)
            //   [1]   = FIRMWARE_VER_CODE
            //   [2]   = MAX_CONTACTS / 2
            //   [3]   = MAX_GROUP_CHANNELS
            //   [4..7]  = ble_pin (uint32 LE)
            //   [8..19] = build date (12 bytes, C string)
            //   [20..59] = manufacturer (40 bytes, C string)
            //   [60..79] = firmware version (20 bytes, C string)
            //
            // So we treat index 0 as a header byte and parse from index 1 onward.
            const int minimumLength = 1  // RESP_CODE_DEVICE_INFO
                                      + 1  // firmware code
                                      + 1  // maxContactsHalf
                                      + 1  // maxGroupChannels
                                      + 4  // ble_pin
                                      + 12 // build date
                                      + 40 // manufacturer
                                      + 20; // firmware version

            if (data.Length < minimumLength)
            {
                return false;
            }

            try
            {
                // Skip RESP_CODE_DEVICE_INFO at [0]
                var firmwareVerCode = data[1];
                var maxContactsHalf = data[2];
                var maxGroupChannels = data[3];

                var blePin = BitConverter.ToUInt32(data, 4);

                var buildDate = DecodeFixedString(data, 8, 12);
                var manufacturer = DecodeFixedString(data, 20, 40);
                var firmwareVersion = DecodeFixedString(data, 60, 20);

                var deviceInfo = new DeviceInfo
                {
                    DeviceId = manufacturer,
                    FirmwareVersion = firmwareVersion,
                    HardwareVersion = buildDate,
                    SerialNumber = blePin != 0 ? blePin.ToString("D6") : string.Empty,
                    MaxContacts = maxContactsHalf * 2,
                    MaxGroupChannels = maxGroupChannels
                };

                result = deviceInfo;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Decodes a fixed-width C-style string block from the specified buffer, stopping at the first null byte
        /// or at <paramref name="length"/> bytes, whichever comes first, and trimming trailing whitespace.
        /// </summary>
        /// <param name="buffer">The source buffer containing the string data.</param>
        /// <param name="offset">The zero-based offset into the buffer where the string starts.</param>
        /// <param name="length">The maximum number of bytes to read.</param>
        /// <returns>The decoded string, or an empty string if no valid characters were found.</returns>
        private static string DecodeFixedString(byte[] buffer, int offset, int length)
        {
            var max = Math.Min(buffer.Length, offset + length);
            var end = offset;

            while (end < max && buffer[end] != 0)
            {
                end++;
            }

            if (end <= offset)
            {
                return string.Empty;
            }

            var sliceLength = end - offset;
            var value = Encoding.ASCII.GetString(buffer, offset, sliceLength);

            return value.Trim();
        }
    }
}