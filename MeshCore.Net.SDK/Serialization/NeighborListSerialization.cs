// <copyright file="NeighborListSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of neighbor list response payloads from remote repeater/room server nodes
    /// Based on the meshcore-cli req_neighbours command implementation
    /// </summary>
    internal class NeighborListSerialization : IBinaryDeserializer<NeighborList>
    {
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
        /// </summary>
        /// <param name="data">The byte array containing the neighbor list response data</param>
        /// <returns>The deserialized NeighborList object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public NeighborList? Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize neighbor list from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a neighbor list from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized neighbor list response data</param>
        /// <param name="result">The resulting NeighborList when deserialization succeeds; otherwise, null</param>
        /// <returns>true if the neighbor list was successfully deserialized; otherwise, false</returns>
        /// <remarks>
        /// Expected payload layout for neighbor list binary response:
        /// [0..1]   = Total neighbor count (uint16, little-endian) - total neighbors the repeater has
        /// [2..3]   = Results count (uint16, little-endian) - number of neighbors in this response
        /// [4..]    = Neighbor entries (variable length array)
        /// 
        /// Each neighbor entry contains:
        /// [0..31]  = Public key (32 bytes)
        /// [32..35] = Seconds ago (uint32, little-endian) - time since last seen
        /// [36..39] = SNR (float32, little-endian) - signal-to-noise ratio in dB
        /// 
        /// Total size per entry: 40 bytes (32 + 4 + 4)
        /// 
        /// Based on the Python CLI fetch_all_neighbours() which processes binary neighbor data
        /// from remote repeater/room server CLI commands.
        /// </remarks>
        public bool TryDeserialize(byte[] data, out NeighborList? result)
        {
            result = null;

            if (data == null || data.Length < 4)
            {
                return false;
            }

            var offset = 0;

            // Read total neighbor count (uint16, little-endian)
            var neighboursCount = BitConverter.ToUInt16(data, offset);
            offset += 2;

            // Read results count (uint16, little-endian)
            var resultsCount = BitConverter.ToUInt16(data, offset);
            offset += 2;

            // Validate we have enough data for the claimed number of neighbor entries
            const int neighborEntrySize = 32 + 4 + 4; // pubkey + secs_ago + snr
            var expectedDataSize = 4 + (resultsCount * neighborEntrySize);
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

            // Parse each neighbor entry
            for (int i = 0; i < resultsCount; i++)
            {
                // Validate we have enough data remaining for this entry
                if (data.Length < offset + neighborEntrySize)
                {
                    return false;
                }

                // Read public key (32 bytes)
                var publicKeyBytes = new byte[32];
                Array.Copy(data, offset, publicKeyBytes, 0, 32);
                var publicKey = Convert.ToHexString(publicKeyBytes).ToLowerInvariant();
                offset += 32;

                // Read seconds ago (uint32, little-endian)
                var secondsAgo = BitConverter.ToInt32(data, offset);
                offset += 4;

                // Read SNR (float32, little-endian)
                var snr = BitConverter.ToSingle(data, offset);
                offset += 4;

                result.Neighbours.Add(new NeighborEntry
                {
                    PublicKey = publicKey,
                    SecondsAgo = secondsAgo,
                    Snr = snr
                });
            }

            return true;
        }
    }
}