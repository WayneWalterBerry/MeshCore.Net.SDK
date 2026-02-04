// <copyright file="IBinarySerializer.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    internal interface IBinarySerializer<T>
    {
        /// <summary>
        /// Serializes an object to a byte array
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>The serialized object as a byte array</returns>
        byte[] Serialize(T obj);
    }
}
