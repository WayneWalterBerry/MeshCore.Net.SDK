// <copyright file="OutboundRoute.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents an outbound route consisting of a sequence of path bytes.
    /// </summary>
    public class OutboundRoute
    {
        /// <summary>
        /// Gets or sets the path as a byte array.
        /// </summary>
        public byte[] Path { get; set; } = Array.Empty<byte>();

        /// <inheritdoc/>
        public override string ToString()
        {
            var length = this.Path?.Length ?? 0;
            if (length == 0)
            {
                return $"AdvertPath: length=0, path=";
            }

            const int maxHopsToShow = 16;
            var hopsToShow = length < maxHopsToShow ? length : maxHopsToShow;

            // Build "AA,BB,CC,..." style list for the first N hops
            var parts = new string[hopsToShow];
            for (var i = 0; i < hopsToShow; i++)
            {
                parts[i] = this.Path![i].ToString("X2");
            }

            var preview = string.Join(",", parts);
            var suffix = length > maxHopsToShow ? ",…" : string.Empty;

            return $"AdvertPath: length={length}, path={preview}{suffix}";
        }
    }
}
