using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Placeholder for Bluetooth LE transport implementation (coming in v2.0)
/// </summary>
public class BluetoothTransport : ITransport
{
    private readonly string _deviceId;
    private readonly ILogger<BluetoothTransport> _logger;
    private bool _disposed;

    public event EventHandler<MeshCoreFrame>? FrameReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected { get; private set; }
    public string? ConnectionId => _deviceId;

    public BluetoothTransport(string deviceId, ILoggerFactory? loggerFactory = null)
    {
        _deviceId = deviceId;
        _logger = loggerFactory?.CreateLogger<BluetoothTransport>() ?? NullLogger<BluetoothTransport>.Instance;
        
        _logger.LogDebug("Bluetooth LE Transport created for device {DeviceId} (not yet implemented)", deviceId);
    }

    public Task ConnectAsync()
    {
        _logger.LogInformation("Bluetooth LE transport is not yet implemented");
        throw new NotImplementedException("Bluetooth LE transport will be implemented in v2.0. Please use USB transport for now.");
    }

    public void Disconnect()
    {
        IsConnected = false;
        _logger.LogDebug("Bluetooth LE transport disconnected");
    }

    public Task SendFrameAsync(MeshCoreFrame frame)
    {
        throw new NotImplementedException("Bluetooth LE transport will be implemented in v2.0");
    }

    public Task<MeshCoreFrame> SendCommandAsync(MeshCoreCommand command, byte[]? data = null, TimeSpan? timeout = null)
    {
        throw new NotImplementedException("Bluetooth LE transport will be implemented in v2.0");
    }

    /// <summary>
    /// Discovers Bluetooth LE MeshCore devices (placeholder implementation)
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverDevicesAsync(TimeSpan? timeout = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<BluetoothTransport>() ?? NullLogger<BluetoothTransport>.Instance;
        logger.LogInformation("Bluetooth LE device discovery is not yet implemented");
        
        // Return empty list for now - actual implementation will discover BLE devices
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