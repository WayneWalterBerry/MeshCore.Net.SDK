// <copyright file="Advertisement.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models;

/// <summary>
/// Represents a self-advertisement configuration for announcing device presence on the mesh network
/// </summary>
public class Advertisement
{
    /// <summary>
    /// Gets or sets whether to use flood mode (network-wide) or zero-hop mode (local only)
    /// </summary>
    public bool UseFloodMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the device name to advertise
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets whether to include GPS coordinates in the advertisement
    /// </summary>
    public bool IncludeLocation { get; set; } = false;

    /// <summary>
    /// Gets or sets the GPS latitude to include in the advertisement (if IncludeLocation is true)
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the GPS longitude to include in the advertisement (if IncludeLocation is true)
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Gets or sets the node type to advertise
    /// </summary>
    public NodeType NodeType { get; set; } = NodeType.Chat;
}
