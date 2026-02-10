// <copyright file="StatusInfoSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using System.Text.Json;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of status response payloads from remote repeater or room server nodes
    /// into <see cref="StatusInfo"/> objects.
    /// </summary>
    internal sealed class StatusInfoSerialization : IBinaryDeserializer<StatusInfo>
    {
        private static readonly Lazy<StatusInfoSerialization> _instance = new(() => new StatusInfoSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="StatusInfoSerialization"/> class.
        /// </summary>
        public static StatusInfoSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private StatusInfoSerialization()
        {
        }

        /// <summary>
        /// Deserializes a status response payload to a <see cref="StatusInfo"/> object.
        /// </summary>
        /// <param name="data">The byte array containing the status response data.</param>
        /// <returns>The deserialized <see cref="StatusInfo"/> object.</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails.</exception>
        public StatusInfo Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize status info from binary data.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a status snapshot from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized status response data.</param>
        /// <param name="result">The resulting <see cref="StatusInfo"/> when deserialization succeeds; otherwise, null.</param>
        /// <returns><see langword="true"/> if the status was successfully deserialized; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// This implementation assumes that status responses are encoded as UTF‑8 JSON with an optional
        /// leading response code byte. The typical layouts are:
        /// <code>
        /// [0]      = RESP_CODE_* (optional protocol response code)
        /// [1..N-1] = UTF‑8 JSON document describing status fields
        /// </code>
        /// or, for some firmwares:
        /// <code>
        /// [0..N-1] = UTF‑8 JSON document (no leading response code)
        /// </code>
        /// The JSON is expected to contain fields that map directly to <see cref="StatusInfo"/> properties
        /// via their <see cref="System.Text.Json.Serialization.JsonPropertyNameAttribute"/> values.
        /// </remarks>
        public bool TryDeserialize(byte[] data, out StatusInfo? result)
        {
            result = null;

            if (data == null || data.Length == 0)
            {
                return false;
            }

            try
            {
                // Heuristic: if the first byte is printable JSON ('{' or '['), treat the whole buffer as JSON.
                // Otherwise, assume the first byte is a response code and parse JSON from the remaining bytes.
                int jsonOffset = 0;
                if (data[0] != (byte)'{' && data[0] != (byte)'[')
                {
                    jsonOffset = 1;
                    if (data.Length <= jsonOffset)
                    {
                        return false;
                    }
                }

                var jsonBytes = new byte[data.Length - jsonOffset];
                Buffer.BlockCopy(data, jsonOffset, jsonBytes, 0, jsonBytes.Length);

                var json = Encoding.UTF8.GetString(jsonBytes);

                // System.Text.Json will bind by JsonPropertyName attributes defined on StatusInfo.
                var status = JsonSerializer.Deserialize<StatusInfo>(json);
                if (status == null)
                {
                    return false;
                }

                result = status;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}