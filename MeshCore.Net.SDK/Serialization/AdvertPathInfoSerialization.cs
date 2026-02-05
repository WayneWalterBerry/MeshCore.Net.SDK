// <copyright file="AdvertPathSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Binary serializer for <see cref="OutboundRoute"/> instances using the MeshCore advert path wire format.
    /// </summary>
    public sealed class AdvertPathInfoSerialization : IBinarySerializer<AdvertPathInfo>, IBinaryDeserializer<AdvertPathInfo>
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

            if (data.Length < 5)
            {
                return false;
            }

            var timestamp = BitConverter.ToUInt32(data, 0);
            var pathLen = data[4];

            if (data.Length < 5 + pathLen)
            {
                throw new ArgumentException("Advert path payload is shorter than indicated path length.", nameof(data));
            }

            var path = new byte[pathLen];
            Buffer.BlockCopy(data, 5, path, 0, pathLen);

            result = new AdvertPathInfo
            {
                ReceivedTimestamp = timestamp,
                Path = path
            };

            return true;
        }

        /// <inheritdoc />
        public byte[] Serialize(AdvertPathInfo value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var path = value.Path ?? Array.Empty<byte>();
            if (path.Length > byte.MaxValue)
            {
                throw new ArgumentException("Advert path length cannot exceed 255 bytes.", nameof(value));
            }

            var buffer = new byte[5 + path.Length];
            var timestampBytes = BitConverter.GetBytes(value.ReceivedTimestamp);
            Buffer.BlockCopy(timestampBytes, 0, buffer, 0, 4);
            buffer[4] = (byte)path.Length;
            if (path.Length > 0)
            {
                Buffer.BlockCopy(path, 0, buffer, 5, path.Length);
            }

            return buffer;
        }
    }
}