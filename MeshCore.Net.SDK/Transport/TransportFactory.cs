namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Factory for creating transport instances
/// </summary>
public static class TransportFactory
{
    /// <summary>
    /// Creates a transport instance for the specified device
    /// </summary>
    public static ITransport CreateTransport(MeshCoreDevice device)
    {
        return device.ConnectionType switch
        {
            DeviceConnectionType.USB => new UsbTransport(device.Id),
            DeviceConnectionType.BluetoothLE => new BluetoothTransport(device.Id),
            DeviceConnectionType.Bluetooth => throw new NotSupportedException("Classic Bluetooth not yet implemented"),
            DeviceConnectionType.WiFi => throw new NotSupportedException("WiFi transport not yet implemented"),
            _ => throw new ArgumentException($"Unsupported connection type: {device.ConnectionType}")
        };
    }

    /// <summary>
    /// Creates a transport instance from a connection string
    /// </summary>
    public static ITransport CreateTransport(string connectionString)
    {
        if (connectionString.StartsWith("COM") || connectionString.StartsWith("/dev/"))
        {
            return new UsbTransport(connectionString);
        }
        
        if (connectionString.Contains(":") && connectionString.Length > 10)
        {
            // Looks like a Bluetooth device ID
            return new BluetoothTransport(connectionString);
        }
        
        // Default to USB for backward compatibility
        return new UsbTransport(connectionString);
    }

    /// <summary>
    /// Discovers all available MeshCore devices across all transport types
    /// </summary>
    public static async Task<List<MeshCoreDevice>> DiscoverAllDevicesAsync(TimeSpan? timeout = null)
    {
        var devices = new List<MeshCoreDevice>();

        // Discover USB devices
        try
        {
            var usbDevices = await UsbTransport.DiscoverDevicesAsync();
            devices.AddRange(usbDevices);
        }
        catch (Exception)
        {
            // USB discovery failed, continue with other transports
        }

        // Discover Bluetooth LE devices
        try
        {
            var bleDevices = await BluetoothTransport.DiscoverDevicesAsync(timeout);
            devices.AddRange(bleDevices);
        }
        catch (Exception)
        {
            // BLE discovery failed, continue
        }

        return devices;
    }
}