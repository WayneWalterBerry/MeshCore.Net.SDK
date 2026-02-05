// <copyright file="OutboundRouteSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    internal class OutboundRouteSerialization : IBinaryDeserializer<OutboundRoute>
    {
        private static readonly Lazy<OutboundRouteSerialization> _instance = new(() => new OutboundRouteSerialization());

        /// <summary>
        /// Gets the singleton instance of the OutboundRouteSerialization
        /// </summary>
        public static OutboundRouteSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private OutboundRouteSerialization()
        {
        }

        public OutboundRoute? Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize outbound route info from binary data");
            }

            return result;
        }

        public bool TryDeserialize(byte[] data, out OutboundRoute? result)
        {
            result = new OutboundRoute
            {
                Path = data.ToArray()
            };

            return true;
        }
    }
}
