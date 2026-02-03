using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Bluetooth Low Energy transport for MeshCore devices (Future Implementation)
/// This is a placeholder implementation - full BLE support will be added in future releases
/// </summary>
public class BluetoothTransport : ITransport
{
    private readonly string _deviceId;
    private bool _disposed;
    
    public event EventHandler<MeshCoreFrame>? FrameReceived;
    public event EventHandler<Exception>? ErrorOccurred;
    
    public bool IsConnected => false; // Not implemented yet
    public string? ConnectionId => _deviceId;
    
    public BluetoothTransport(string deviceId)
    {
        _deviceId = deviceId;
    }
    
    /// <summary>
    /// Connects to the MeshCore BLE device
    /// </summary>
    public Task ConnectAsync()
    {
        throw new NotImplementedException("Bluetooth LE support is planned for a future release. " +
            "Please use USB connection for now or check for SDK updates.");
    }
    
    /// <summary>
    /// Disconnects from the MeshCore BLE device
    /// </summary>
    public void Disconnect()
    {
        // No-op for now
    }
    
    /// <summary>
    /// Sends a frame to the MeshCore device via BLE
    /// </summary>
    public Task SendFrameAsync(MeshCoreFrame frame)
    {
        throw new NotImplementedException("Bluetooth LE support is planned for a future release. " +
            "Please use USB connection for now or check for SDK updates.");
    }
    
    /// <summary>
    /// Sends a command and waits for a response
    /// </summary>
    public Task<MeshCoreFrame> SendCommandAsync(MeshCoreCommand command, byte[]? data = null, 
        TimeSpan? timeout = null)
    {
        throw new NotImplementedException("Bluetooth LE support is planned for a future release. " +
            "Please use USB connection for now or check for SDK updates.");
    }
    
    /// <summary>
    /// Discovers MeshCore BLE devices (Future Implementation)
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverDevicesAsync(TimeSpan? scanTimeout = null)
    {
        // Return empty list for now
        return Task.FromResult(new List<MeshCoreDevice>());
    }
    
    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _disposed = true;
        }
    }
}