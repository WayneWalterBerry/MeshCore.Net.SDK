// <copyright file="NeighborListSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of neighbor list response payloads from remote repeater/room server nodes
    /// via <c>CMD_SEND_BINARY_REQ</c> with <c>BinaryReqType.NEIGHBOURS (0x06)</c>.
    /// </summary>
    /// <remarks>
    /// Binary wire format (response_data portion after stripping BINARY_RESPONSE header):
    /// <code>
    /// Offset  Type    Field
    /// 0-1     int16   neighbours_count (total neighbours the repeater knows, LE)
    /// 2-3     int16   results_count (number of entries in this response, LE)
    /// 4..     entries (variable length, repeated results_count times)
    ///
    /// Each entry:
    ///   [pubkey: pk_plen bytes][secs_ago: int32 LE][snr: int8 signed, value / 4.0]
    /// </code>
    /// Matches the Python reference <c>reader.py</c> NEIGHBOURS handler.
    /// </remarks>
    internal class NeighborListSerialization : IBinaryDeserializer<NeighborList>
    {
        /// <summary>
        /// Default public key prefix length when not specified.
        /// </summary>
        private const int DEFAULT_PUBKEY_PREFIX_LENGTH = 4;

        private static readonly Lazy<NeighborListSerialization> _instance = new(() => new NeighborListSerialization());

        /// <summary>
        /// Gets the singleton instance of the NeighborListSerialization
        /// </summary>
        public static NeighborListSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private NeighborListSerialization()
        {
        }

        /// <summary>
        /// Deserializes a neighbor list response payload to a NeighborList object
        /// using the default public key prefix length of 4.
        /// </summary>
        /// <param name="data">The byte array containing the neighbor list response data</param>
        /// <returns>The deserialized NeighborList object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public NeighborList? Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, DEFAULT_PUBKEY_PREFIX_LENGTH, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize neighbor list from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a neighbor list from the specified byte array
        /// using the default public key prefix length of 4.
        /// </summary>
        /// <param name="data">The byte array containing the serialized neighbor list response data</param>
        /// <param name="result">The resulting NeighborList when deserialization succeeds; otherwise, null</param>
        /// <returns>true if the neighbor list was successfully deserialized; otherwise, false</returns>
        public bool TryDeserialize(byte[] data, out NeighborList? result)
        {
            return TryDeserialize(data, DEFAULT_PUBKEY_PREFIX_LENGTH, out result);
        }

        /// <summary>
        /// Attempts to deserialize a neighbor list from the specified byte array
        /// using the given public key prefix length.
        /// </summary>
        /// <param name="data">The byte array containing the serialized neighbor list response data</param>
        /// <param name="pubkeyPrefixLength">Number of bytes per public key prefix in each entry</param>
        /// <param name="result">The resulting NeighborList when deserialization succeeds; otherwise, null</param>
        /// <returns>true if the neighbor list was successfully deserialized; otherwise, false</returns>
        /// <remarks>
        /// Binary layout per the Python reference (reader.py NEIGHBOURS handler):
        /// <code>
        /// [0-1]  neighbours_count (int16 LE) — total neighbours the repeater knows
        /// [2-3]  results_count (int16 LE) — entries in this response
        /// [4..]  entries: pubkey(pk_plen) + secs_ago(int32 LE) + snr(int8 signed, /4.0)
        /// </code>
        /// </remarks>
        public bool TryDeserialize(byte[] data, int pubkeyPrefixLength, out NeighborList? result)
        {
            result = null;

            if (data == null || data.Length < 4)
            {
                return false;
            }

            var offset = 0;

            // Read total neighbor count (int16, little-endian)
            var neighboursCount = BitConverter.ToInt16(data, offset);
            offset += 2;

            // Read results count (int16, little-endian)
            var resultsCount = BitConverter.ToInt16(data, offset);
            offset += 2;

            if (resultsCount < 0 || neighboursCount < 0)
            {
                return false;
            }

            // Each entry: pubkey(pk_plen) + secs_ago(4) + snr(1)
            var entrySize = pubkeyPrefixLength + 4 + 1;
            var expectedDataSize = 4 + (resultsCount * entrySize);
            if (data.Length < expectedDataSize)
            {
                return false;
            }

            result = new NeighborList
            {
                NeighboursCount = neighboursCount,
                ResultsCount = resultsCount,
                Neighbours = new List<NeighborEntry>(resultsCount)
            };

            for (int i = 0; i < resultsCount; i++)
            {
                if (data.Length < offset + entrySize)
                {
                    return false;
                }

                // Read public key prefix (pk_plen bytes)
                var publicKey = Convert.ToHexString(data, offset, pubkeyPrefixLength).ToLowerInvariant();
                offset += pubkeyPrefixLength;

                // Read seconds ago (int32, little-endian)
                var secondsAgo = BitConverter.ToInt32(data, offset);
                offset += 4;

                // Read SNR (signed int8, divide by 4.0 to get dB)
                var snrRaw = (sbyte)data[offset];
                offset += 1;

                result.Neighbours.Add(new NeighborEntry
                {
                    PublicKey = publicKey,
                    SecondsAgo = secondsAgo,
                    Snr = snrRaw / 4.0
                });
            }

            return true;
        }
    }
}