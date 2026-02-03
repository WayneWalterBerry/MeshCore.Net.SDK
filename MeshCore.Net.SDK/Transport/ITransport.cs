namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Common interface for all MeshCore transport implementations
/// </summary>
public interface ITransport : IDisposable
{
    /// <summary>
    /// Event fired when a frame is received
    /// </summary>
    event EventHandler<Protocol.MeshCoreFrame>? FrameReceived;
    
    /// <summary>
    /// Event fired when an error occurs
    /// </summary>
    event EventHandler<Exception>? ErrorOccurred;
    
    /// <summary>
    /// Gets whether the transport is currently connected
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets a connection identifier for the transport
    /// </summary>
    string? ConnectionId { get; }
    
    /// <summary>
    /// Connects to the MeshCore device
    /// </summary>
    Task ConnectAsync();
    
    /// <summary>
    /// Disconnects from the MeshCore device
    /// </summary>
    void Disconnect();
    
    /// <summary>
    /// Sends a frame to the MeshCore device
    /// </summary>
    Task SendFrameAsync(Protocol.MeshCoreFrame frame);
    
    /// <summary>
    /// Sends a command and waits for a response
    /// </summary>
    Task<Protocol.MeshCoreFrame> SendCommandAsync(Protocol.MeshCoreCommand command, 
        byte[]? data = null, TimeSpan? timeout = null);
}

/// <summary>
/// Represents a discovered MeshCore device
/// </summary>
public class MeshCoreDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceConnectionType ConnectionType { get; set; }
    public string? Address { get; set; }
    public int? SignalStrength { get; set; }
    public bool IsPaired { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    
    public override string ToString() => 
        $"{Name} ({ConnectionType}) - {Id}";
}

/// <summary>
/// Type of device connection
/// </summary>
public enum DeviceConnectionType
{
    USB,
    Bluetooth,
    BluetoothLE,
    WiFi,
    Unknown
}