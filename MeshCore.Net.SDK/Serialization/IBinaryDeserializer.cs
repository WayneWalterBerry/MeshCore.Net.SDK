// <copyright file="IBinaryDeserializer.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    /// <summary>
    /// Interface for deserializing byte arrays to strongly typed objects
    /// </summary>
    public interface IBinaryDeserializer<T>
    {
        /// <summary>
        /// Deserializes a byte array to the specified type
        /// </summary>
        /// <param name="data">The byte array to deserialize</param>
        /// <returns>The deserialized object, or null if deserialization fails</returns>
        T Deserialize(byte[] data);

        /// <summary>
        /// Attempts to deserialize a byte array to the specified type
        /// </summary>
        /// <param name="data">The byte array to deserialize</param>
        /// <param name="result">The deserialized object if successful</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        bool TryDeserialize(byte[] data, out T result);
    }
}