// <copyright file="RemoteCommandResponseFrameSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of remote command response frame payloads into
    /// <see cref="RemoteCommandResponseFrame"/> models.
    /// </summary>
    /// <remarks>
    /// Expected payload layout (as produced by firmware for remote CLI responses):
    /// <code>
    /// [0]        = RESP_CODE
    /// [1..7]     = sender_pubkey_prefix (7 bytes)
    /// [8]        = txt_type
    /// [9..12]    = timestamp (uint32, little-endian, Unix seconds)
    /// [13..N-1]  = data (command-specific binary payload)
    /// </code>
    /// </remarks>
    internal sealed class RemoteCommandResponseFrameSerialization : IBinaryDeserializer<RemoteCommandResponseFrame>
    {
        private const int HeaderLength = 13;

        private static readonly Lazy<RemoteCommandResponseFrameSerialization> _instance =
            new(() => new RemoteCommandResponseFrameSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="RemoteCommandResponseFrameSerialization"/> class.
        /// </summary>
        public static RemoteCommandResponseFrameSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private RemoteCommandResponseFrameSerialization()
        {
        }

        /// <summary>
        /// Deserializes the specified payload into a <see cref="RemoteCommandResponseFrame"/>.
        /// </summary>
        /// <param name="data">The raw frame payload bytes.</param>
        /// <returns>A fully populated <see cref="RemoteCommandResponseFrame"/> instance.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the payload cannot be parsed as a valid remote command response frame.
        /// </exception>
        public RemoteCommandResponseFrame Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize remote command response frame from binary data.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize the specified payload into a <see cref="RemoteCommandResponseFrame"/>.
        /// </summary>
        /// <param name="data">The raw frame payload bytes.</param>
        /// <param name="result">
        /// When this method returns, contains the deserialized <see cref="RemoteCommandResponseFrame"/> if the operation
        /// succeeded; otherwise, <c>null</c>.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the payload was successfully deserialized; otherwise, <see langword="false"/>.
        /// </returns>
        public bool TryDeserialize(byte[] data, out RemoteCommandResponseFrame? result)
        {
            result = null;

            if (data == null || data.Length < HeaderLength)
            {
                return false;
            }

            // Offsets:
            // 0      = resp_code
            // 1..7   = sender_pubkey_prefix (7 bytes)
            // 8      = txt_type
            // 9..12  = timestamp (uint32, LE)
            // 13..N  = data
            var responseCode = data[0];

            var prefixBytes = new byte[7];
            Buffer.BlockCopy(data, 1, prefixBytes, 0, prefixBytes.Length);
            var senderPrefix = Convert.ToHexString(prefixBytes).ToLowerInvariant();

            var textType = data[8];

            var timestamp = BitConverter.ToUInt32(data, 9);

            var dataLength = data.Length - HeaderLength;
            var payloadData = new byte[dataLength];
            if (dataLength > 0)
            {
                Buffer.BlockCopy(data, HeaderLength, payloadData, 0, dataLength);
            }

            result = new RemoteCommandResponseFrame
            {
                ResponseCode = responseCode,
                SenderPublicKeyPrefix = senderPrefix,
                TextType = textType,
                Timestamp = timestamp,
                Payload = payloadData
            };

            return true;
        }
    }
}