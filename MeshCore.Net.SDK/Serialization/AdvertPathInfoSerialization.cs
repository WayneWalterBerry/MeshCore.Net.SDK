// <copyright file="AdvertPathSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Binary serializer for <see cref="OutboundRoute"/> instances using the MeshCore advert path wire format.
    /// </summary>
    public sealed class AdvertPathInfoSerialization : IBinaryDeserializer<AdvertPathInfo>
    {
        private static readonly Lazy<AdvertPathInfoSerialization> _instance = new(() => new AdvertPathInfoSerialization());

        /// <summary>
        /// Gets the singleton instance of the AdvertPathSerialization
        /// </summary>
        public static AdvertPathInfoSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private AdvertPathInfoSerialization()
        {
        }

        /// <inheritdoc />
        public AdvertPathInfo? Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize advert path info from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize the specified byte array into an <see cref="OutboundRoute"/> instance.
        /// </summary>
        /// <param name="data">The byte array containing the serialized advert path data. Must be at least 5 bytes long and match the indicated
        /// path length.</param>
        /// <param name="result">When this method returns, contains the deserialized <see cref="OutboundRoute"/> if the operation succeeds;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>true if the data was successfully deserialized; otherwise, false.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is shorter than the indicated path length.</exception>
        public bool TryDeserialize(byte[] data, out AdvertPathInfo? result)
        {
            result = default(AdvertPathInfo?);

            if (data == null)
            {
                return false;
            }

            // Expected format from firmware: [RESP_CODE][recv_timestamp:4][path_len:1][path_data:path_len]
            // Minimum length: response code + timestamp + path_len = 6 bytes
            if (data.Length < 6)
            {
                return false;
            }

            // Read timestamp from bytes 1-4 (little-endian)
            var timestamp = BitConverter.ToUInt32(data, 1);
            
            // Read path length from byte 5
            var pathLen = data[5];

            // Validate we have enough data: response code + timestamp + path_len + actual path data
            if (data.Length < 6 + pathLen)
            {
                throw new ArgumentException($"Advert path payload is shorter than indicated path length. Expected at least {6 + pathLen} bytes, got {data.Length} bytes.", nameof(data));
            }

            // Extract path data starting from byte 6
            var path = new byte[pathLen];
            if (pathLen > 0)
            {
                Buffer.BlockCopy(data, 6, path, 0, pathLen);
            }

            DateTime receivedTimestamp = DateTime.UnixEpoch;
            if (timestamp != 0)
            {
                receivedTimestamp = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            }

            result = new AdvertPathInfo
            {
                ReceivedTimestamp = receivedTimestamp,
                Path = path
            };

            return true;
        }
    }
}