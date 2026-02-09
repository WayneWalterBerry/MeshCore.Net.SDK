// <copyright file="PathDiscoveryResultSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles deserialization of path discovery response payloads from PATH_RESPONSE events
    /// Based on the meshcore-cli implementation and CMD_SEND_PATH_DISCOVERY_REQ protocol
    /// </summary>
    internal class PathDiscoveryResultSerialization : IBinaryDeserializer<PathDiscoveryResult>
    {
        private static readonly Lazy<PathDiscoveryResultSerialization> _instance = new(() => new PathDiscoveryResultSerialization());

        /// <summary>
        /// Gets the singleton instance of the PathDiscoveryResultSerialization
        /// </summary>
        public static PathDiscoveryResultSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private PathDiscoveryResultSerialization()
        {
        }

        /// <summary>
        /// Deserializes a PATH_RESPONSE event payload to a PathDiscoveryResult object
        /// </summary>
        /// <param name="data">The byte array containing the path discovery response data</param>
        /// <returns>The deserialized PathDiscoveryResult object</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public PathDiscoveryResult? Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize path discovery result from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a path discovery result from the specified byte array.
        /// </summary>
        /// <param name="data">The byte array containing the serialized path discovery response data</param>
        /// <param name="result">The resulting PathDiscoveryResult when deserialization succeeds; otherwise, null</param>
        /// <returns>true if the path discovery result was successfully deserialized; otherwise, false</returns>
        /// <remarks>
        /// Expected payload layout for PATH_RESPONSE event:
        /// [0]     = Response code (optional - may be stripped before deserialization)
        /// [1]     = Outbound path length (number of hops from this node to target)
        /// [2..x]  = Outbound path data (variable length, each hop is 1 byte)
        /// [x+1]   = Inbound path length (number of hops from target to this node)
        /// [x+2..] = Inbound path data (variable length, each hop is 1 byte)
        /// 
        /// Based on the Python CLI discover_path() function which receives:
        /// - res.payload["in_path"] as hex string
        /// - res.payload["out_path"] as hex string
        /// </remarks>
        public bool TryDeserialize(byte[] data, out PathDiscoveryResult? result)
        {
            result = null;

            if (data == null || data.Length == 0)
            {
                return false;
            }

            var payloadStart = 0;

            // Allow caller to pass either raw path payload or full response frame
            // Check if first byte is a known response code for path discovery
            if (data.Length > 1 && data[0] > 0x80) // Response codes are typically > 0x80 for events
            {
                payloadStart = 1;
                if (data.Length <= payloadStart)
                {
                    return false;
                }
            }

            var pathData = data.AsSpan(payloadStart);
            if (pathData.Length < 2) // Need at least 2 bytes for the two length fields
            {
                return false;
            }

            var offset = 0;

            // Read outbound path length
            var outboundPathLength = pathData[offset++];

            // Validate we have enough data for the outbound path
            if (pathData.Length < offset + outboundPathLength)
            {
                return false;
            }

            // Read outbound path data
            var outboundPathBytes = new byte[outboundPathLength];
            if (outboundPathLength > 0)
            {
                pathData.Slice(offset, outboundPathLength).CopyTo(outboundPathBytes);
                offset += outboundPathLength;
            }

            // Validate we have at least one more byte for inbound path length
            if (pathData.Length <= offset)
            {
                return false;
            }

            // Read inbound path length
            var inboundPathLength = pathData[offset++];

            // Validate we have enough data for the inbound path
            if (pathData.Length < offset + inboundPathLength)
            {
                return false;
            }

            // Read inbound path data
            var inboundPathBytes = new byte[inboundPathLength];
            if (inboundPathLength > 0)
            {
                pathData.Slice(offset, inboundPathLength).CopyTo(inboundPathBytes);
            }

            // Convert byte arrays to hex strings (matching Python CLI format)
            var outboundPathHex = outboundPathLength > 0 ? Convert.ToHexString(outboundPathBytes).ToLowerInvariant() : string.Empty;
            var inboundPathHex = inboundPathLength > 0 ? Convert.ToHexString(inboundPathBytes).ToLowerInvariant() : string.Empty;

            result = new PathDiscoveryResult
            {
                OutPath = outboundPathHex,
                InPath = inboundPathHex
            };

            return true;
        }
    }
}