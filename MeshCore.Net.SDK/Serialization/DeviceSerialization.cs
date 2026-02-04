// <copyright file="DeviceSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;

    internal class DeviceSerialization : IBinaryDeserializer<DeviceConfiguration>
    {
        private static readonly Lazy<DeviceSerialization> _instance = new(() => new DeviceSerialization());

        /// <summary>
        /// Gets the singleton instance of the DeviceSerialization
        /// </summary>
        public static DeviceSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private DeviceSerialization()
        {
        }

        public DeviceConfiguration Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize contact from binary data");
            }

            return result;
        }

        public bool TryDeserialize(byte[] data, out DeviceConfiguration? result)
        {
            var text = Encoding.UTF8.GetString(data);
            var parts = text.Split('\0');

            result = new DeviceConfiguration
            {
                DeviceName = parts.Length > 0 ? parts[0] : null,
                TransmitPower = parts.Length > 1 && int.TryParse(parts[1], out var power) ? power : 100,
                Channel = parts.Length > 2 && int.TryParse(parts[2], out var channel) ? channel : 1,
                AutoRelay = parts.Length > 3 && parts[3] == "1",
                HeartbeatInterval = TimeSpan.FromSeconds(30),
                MessageTimeout = TimeSpan.FromMinutes(5)
            };

            return true;
        }
    }
}
