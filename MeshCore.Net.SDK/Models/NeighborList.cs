// <copyright file="NeighborList.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a neighbor list retrieved from a remote repeater or room server node.
    /// Contains information about nodes that are directly reachable (zero-hop) from the queried node.
    /// </summary>
    public sealed class NeighborList
    {
        /// <summary>
        /// Gets or sets the number of neighbors returned in this response.
        /// This may be less than <see cref="NeighboursCount"/> if the response was truncated.
        /// </summary>
        [JsonPropertyName("results_count")]
        public int ResultsCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of neighbors the repeater has.
        /// This represents the complete neighbor count, even if not all are returned.
        /// </summary>
        [JsonPropertyName("neighbours_count")]
        public int NeighboursCount { get; set; }

        /// <summary>
        /// Gets or sets the list of neighbor entries containing detailed information about each neighbor.
        /// </summary>
        [JsonPropertyName("neighbours")]
        public List<NeighborEntry> Neighbours { get; set; } = new List<NeighborEntry>();

        /// <summary>
        /// Gets a value indicating whether all neighbors were returned in this response.
        /// </summary>
        [JsonIgnore]
        public bool IsComplete => ResultsCount >= NeighboursCount;

        /// <summary>
        /// Gets a value indicating whether the neighbor list is empty.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => Neighbours.Count == 0;

        /// <summary>
        /// Gets the number of neighbors that were seen recently (within the last 60 seconds).
        /// </summary>
        [JsonIgnore]
        public int RecentNeighborsCount => Neighbours.Count(n => n.SecondsAgo <= 60);

        /// <summary>
        /// Gets the average signal-to-noise ratio across all neighbors.
        /// </summary>
        [JsonIgnore]
        public double AverageSnr => Neighbours.Count > 0
            ? Neighbours.Average(n => n.Snr)
            : 0.0;

        /// <summary>
        /// Returns a JSON representation of the neighbor list.
        /// </summary>
        /// <returns>A JSON string describing the neighbor list.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }

    /// <summary>
    /// Represents a single neighbor entry containing information about a directly reachable node.
    /// </summary>
    public sealed class NeighborEntry
    {
        /// <summary>
        /// Gets or sets the public key of the neighbor node as a hexadecimal string.
        /// This uniquely identifies the neighbor in the mesh network.
        /// </summary>
        [JsonPropertyName("pubkey")]
        public string PublicKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the number of seconds since the neighbor was last seen.
        /// Lower values indicate more recent contact with the neighbor.
        /// </summary>
        [JsonPropertyName("secs_ago")]
        public int SecondsAgo { get; set; }

        /// <summary>
        /// Gets or sets the signal-to-noise ratio in decibels (dB) for communications with this neighbor.
        /// Higher values indicate better signal quality.
        /// </summary>
        [JsonPropertyName("snr")]
        public double Snr { get; set; }

        /// <summary>
        /// Gets the public key prefix (first 8 characters) for display purposes.
        /// </summary>
        [JsonIgnore]
        public string PublicKeyPrefix => PublicKey.Length >= 8
            ? PublicKey.Substring(0, 8)
            : PublicKey;

        /// <summary>
        /// Gets a value indicating whether this neighbor has been seen recently (within the last 60 seconds).
        /// </summary>
        [JsonIgnore]
        public bool IsRecent => SecondsAgo <= 60;

        /// <summary>
        /// Gets a value indicating whether the signal quality is good (SNR &gt;= 10 dB).
        /// </summary>
        [JsonIgnore]
        public bool HasGoodSignal => Snr >= 10.0;

        /// <summary>
        /// Gets a value indicating whether the signal quality is poor (SNR &lt;= 0 dB).
        /// </summary>
        [JsonIgnore]
        public bool HasPoorSignal => Snr <= 0.0;

        /// <summary>
        /// Gets a human-readable time description for when the neighbor was last seen.
        /// </summary>
        [JsonIgnore]
        public string TimeAgoDescription
        {
            get
            {
                if (SecondsAgo < 60)
                {
                    return $"{SecondsAgo}s ago";
                }
                else if (SecondsAgo < 3600)
                {
                    return $"{SecondsAgo / 60}m ago";
                }
                else if (SecondsAgo < 86400)
                {
                    return $"{SecondsAgo / 3600}h ago";
                }
                else
                {
                    return $"{SecondsAgo / 86400}d ago";
                }
            }
        }

        /// <summary>
        /// Returns a JSON representation of the neighbor entry.
        /// </summary>
        /// <returns>A JSON string describing the neighbor entry.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Returns a human-readable description of the neighbor.
        /// </summary>
        /// <returns>A formatted string describing the neighbor.</returns>
        public string GetDescription()
        {
            return $"[{PublicKeyPrefix}] {TimeAgoDescription}, {Snr:F2} dB SNR";
        }
    }
}