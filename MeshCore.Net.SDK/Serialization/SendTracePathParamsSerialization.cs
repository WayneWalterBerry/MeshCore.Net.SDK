// <copyright file="SendTracePathParamsSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles serialization of <see cref="SendTracePathParams"/> into the binary payload
    /// expected by CMD_SEND_TRACE_PATH (0x24).
    /// </summary>
    /// <remarks>
    /// Wire format (9 + path_len bytes, transport layer adds the CMD byte):
    /// <code>
    /// Offset   Type     Field
    /// 0-3      uint32   tag (little-endian)
    /// 4-7      uint32   auth_code (little-endian)
    /// 8        uint8    flags
    /// 9..N     bytes    path – one byte per hop (first byte of each repeater's public key)
    /// </code>
    /// Matches the Python reference:
    /// <c>tag.to_bytes(4, "little") + auth_code.to_bytes(4, "little") + flags.to_bytes(1, "little") + path_bytes</c>
    /// </remarks>
    internal sealed class SendTracePathParamsSerialization : IBinarySerializer<SendTracePathParams>
    {
        /// <summary>
        /// Fixed header length: tag(4) + auth_code(4) + flags(1).
        /// </summary>
        private const int HEADER_LENGTH = 9;

        private static readonly Lazy<SendTracePathParamsSerialization> _instance = new(() => new SendTracePathParamsSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="SendTracePathParamsSerialization"/> class.
        /// </summary>
        public static SendTracePathParamsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private SendTracePathParamsSerialization()
        {
        }

        /// <summary>
        /// Serializes a <see cref="SendTracePathParams"/> object into the binary payload for CMD_SEND_TRACE_PATH.
        /// </summary>
        /// <param name="obj">The trace path parameters to serialize.</param>
        /// <returns>A byte array containing the serialized trace path parameters (9 + path length bytes).</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
        public byte[] Serialize(SendTracePathParams obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var pathLength = obj.Path?.Length ?? 0;
            var payload = new byte[HEADER_LENGTH + pathLength];

            BitConverter.GetBytes(obj.Tag).CopyTo(payload, 0);
            BitConverter.GetBytes(obj.AuthCode).CopyTo(payload, 4);
            payload[8] = obj.Flags;

            if (pathLength > 0)
            {
                Buffer.BlockCopy(obj.Path!, 0, payload, HEADER_LENGTH, pathLength);
            }

            return payload;
        }
    }
}
