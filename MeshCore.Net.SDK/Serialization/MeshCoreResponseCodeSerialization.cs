// <copyright file="MeshCoreResponseCodeSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Protocol;

    /// <summary>
    /// Deserializes a MeshCore payload into a <see cref="MeshCoreResponseCode"/> by
    /// interpreting the first byte as the response code and returning it as the result.
    /// </summary>
    public sealed class MeshCoreResponseCodeSerialization : IBinaryDeserializer<MeshCoreResponseCode>
    {
        /// <summary>
        /// Singleton instance of the <see cref="MeshCoreResponseCodeSerialization"/>.
        /// </summary>
        public static MeshCoreResponseCodeSerialization Instance { get; } = new MeshCoreResponseCodeSerialization();

        /// <summary>
        /// Prevents a default instance of the <see cref="MeshCoreResponseCodeSerialization"/> class from being created.
        /// </summary>
        private MeshCoreResponseCodeSerialization()
        {
        }

        /// <summary>
        /// Attempts to deserialize the specified payload into a <see cref="MeshCoreResponseCode"/>.
        /// The first byte of <paramref name="data"/> is interpreted as the response code and
        /// returned as the result; the remaining bytes are ignored.
        /// </summary>
        /// <param name="data">The raw payload bytes received from the device.</param>
        /// <param name="result">
        /// When this method returns, contains the <see cref="MeshCoreResponseCode"/> value
        /// corresponding to the first byte of <paramref name="data"/> if deserialization
        /// succeeded; otherwise, the default value.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="data"/> is non-null and has at least one
        /// byte; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryDeserialize(byte[] data, out MeshCoreResponseCode result)
        {
            if (data == null || data.Length == 0)
            {
                result = default;
                return false;
            }

            // First byte is the MeshCoreResponseCode; remaining bytes are ignored.
            result = (MeshCoreResponseCode)data[0];
            return true;
        }

        /// <summary>
        /// Deserializes the specified byte array into a MeshCoreResponseCode instance.
        /// </summary>
        /// <param name="data">The byte array containing the serialized MeshCoreResponseCode data to deserialize. Cannot be null.</param>
        /// <returns>A MeshCoreResponseCode instance deserialized from the provided data.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the data cannot be deserialized into a valid MeshCoreResponseCode.</exception>
        public MeshCoreResponseCode Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize response code");
            }

            return result;
        }
    }
}