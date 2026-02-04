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
    /// <summary>
    /// Gets or sets the unique identifier for the device
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the display name of the device
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the type of connection for this device
    /// </summary>
    public DeviceConnectionType ConnectionType { get; set; }
    
    /// <summary>
    /// Gets or sets the device address (e.g., COM port, Bluetooth address)
    /// </summary>
    public string? Address { get; set; }
    
    /// <summary>
    /// Gets or sets the signal strength for wireless connections
    /// </summary>
    public int? SignalStrength { get; set; }
    
    /// <summary>
    /// Gets or sets whether the device is paired (for Bluetooth connections)
    /// </summary>
    public bool IsPaired { get; set; }
    
    /// <summary>
    /// Gets or sets additional device properties
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Returns a string representation of the device
    /// </summary>
    /// <returns>A formatted string containing device name, connection type, and ID</returns>
    public override string ToString() => 
        $"{Name} ({ConnectionType}) - {Id}";
}

/// <summary>
/// Defines the type of device connection
/// </summary>
public enum DeviceConnectionType
{
    /// <summary>
    /// USB serial connection
    /// </summary>
    USB,
    
    /// <summary>
    /// Classic Bluetooth connection
    /// </summary>
    Bluetooth,
    
    /// <summary>
    /// Bluetooth Low Energy connection
    /// </summary>
    BluetoothLE,
    
    /// <summary>
    /// WiFi network connection
    /// </summary>
    WiFi,
    
    /// <summary>
    /// Unknown or unspecified connection type
    /// </summary>
    Unknown
}