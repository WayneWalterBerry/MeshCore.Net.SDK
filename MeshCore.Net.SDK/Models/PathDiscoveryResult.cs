// <copyright file="PathDiscoveryResultSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the result of a path discovery operation showing inbound and outbound paths to a contact.
    /// </summary>
    public sealed class PathDiscoveryResult
    {
        /// <summary>
        /// Gets or sets the inbound path as a sequence of hop identifiers from the contact to this node.
        /// Each hop is represented as a 2-character hex string (1 byte).
        /// </summary>
        [JsonPropertyName("in_path")]
        public string InPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the outbound path as a sequence of hop identifiers from this node to the contact.
        /// Each hop is represented as a 2-character hex string (1 byte).
        /// </summary>
        [JsonPropertyName("out_path")]
        public string OutPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets the number of hops in the inbound path.
        /// </summary>
        [JsonIgnore]
        public int InPathLength => InPath.Length / 2;

        /// <summary>
        /// Gets the number of hops in the outbound path.
        /// </summary>
        [JsonIgnore]
        public int OutPathLength => OutPath.Length / 2;

        /// <summary>
        /// Gets a value indicating whether the inbound path is a direct connection (zero hops).
        /// </summary>
        [JsonIgnore]
        public bool IsInPathDirect => InPathLength == 0;

        /// <summary>
        /// Gets a value indicating whether the outbound path is a direct connection (zero hops).
        /// </summary>
        [JsonIgnore]
        public bool IsOutPathDirect => OutPathLength == 0;

        /// <summary>
        /// Gets a value indicating whether both paths are direct connections.
        /// </summary>
        [JsonIgnore]
        public bool IsDirectConnection => IsInPathDirect && IsOutPathDirect;

        /// <summary>
        /// Gets the inbound path as an array of hop identifiers.
        /// Each element represents one hop in the path.
        /// </summary>
        [JsonIgnore]
        public byte[] InPathHops
        {
            get
            {
                if (string.IsNullOrEmpty(InPath) || InPath.Length % 2 != 0)
                    return Array.Empty<byte>();

                var hops = new byte[InPathLength];
                for (int i = 0; i < InPathLength; i++)
                {
                    hops[i] = Convert.ToByte(InPath.Substring(i * 2, 2), 16);
                }
                return hops;
            }
        }

        /// <summary>
        /// Gets the outbound path as an array of hop identifiers.
        /// Each element represents one hop in the path.
        /// </summary>
        [JsonIgnore]
        public byte[] OutPathHops
        {
            get
            {
                if (string.IsNullOrEmpty(OutPath) || OutPath.Length % 2 != 0)
                    return Array.Empty<byte>();

                var hops = new byte[OutPathLength];
                for (int i = 0; i < OutPathLength; i++)
                {
                    hops[i] = Convert.ToByte(OutPath.Substring(i * 2, 2), 16);
                }
                return hops;
            }
        }

        /// <summary>
        /// Returns a JSON representation of the path discovery result.
        /// </summary>
        /// <returns>A JSON string describing the path discovery result.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Returns a human-readable description of the paths.
        /// </summary>
        /// <returns>A formatted string describing the inbound and outbound paths.</returns>
        public string GetPathDescription()
        {
            var inPathDesc = IsInPathDirect ? "direct" : InPath;
            var outPathDesc = IsOutPathDirect ? "direct" : OutPath;

            return $"Outbound: {outPathDesc}, Inbound: {inPathDesc}";
        }
    }
}