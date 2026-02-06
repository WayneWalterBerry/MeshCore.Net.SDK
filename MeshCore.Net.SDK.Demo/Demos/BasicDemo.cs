using Microsoft.Extensions.Logging;
using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Demo.Demos;

/// <summary>
/// Basic demonstration of MeshCore.Net.SDK with transport selection support and proper logging
/// </summary>
public class BasicDemo
{
    public static async Task RunAsync(DeviceConnectionType? preferredTransport = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<BasicDemo>() ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<BasicDemo>.Instance;

        // Show transport preference
        if (preferredTransport.HasValue)
        {
            logger.LogInformation("Looking for {TransportType} devices specifically...", preferredTransport.Value);
        }
        else
        {
            logger.LogInformation("Discovering all available MeshCore devices...");
        }

        try
        {
            List<MeshCoreDevice> devices;

            // Device discovery based on transport preference
            if (preferredTransport == DeviceConnectionType.USB)
            {
                logger.LogInformation("Scanning for USB devices only...");
                devices = await UsbTransport.DiscoverDevicesAsync();
            }
            else if (preferredTransport == DeviceConnectionType.BluetoothLE)
            {
                logger.LogInformation("Scanning for Bluetooth LE devices only...");
                devices = await MeshCoreClient.DiscoverBluetoothDevicesAsync(TimeSpan.FromSeconds(10), loggerFactory);
            }
            else
            {
                logger.LogInformation("Auto-detecting all device types...");
                devices = await MeshCoreClient.DiscoverDevicesAsync(loggerFactory: loggerFactory);
            }

            if (devices.Count == 0)
            {
                ShowNoDevicesFound(preferredTransport, logger);
                return;
            }

            // Filter devices by preferred transport if specified
            if (preferredTransport.HasValue)
            {
                var filteredDevices = devices.Where(d => d.ConnectionType == preferredTransport.Value).ToList();
                if (filteredDevices.Count == 0)
                {
                    logger.LogWarning("No {TransportType} devices found!", preferredTransport.Value);
                    logger.LogInformation("Found {DeviceCount} device(s) of other types:", devices.Count);
                    foreach (var device in devices)
                    {
                        logger.LogInformation("  - {DeviceName} ({ConnectionType})", device.Name, device.ConnectionType);
                    }
                    logger.LogInformation("Tip: Remove --{Transport} flag to use any available device", preferredTransport.Value.ToString().ToLowerInvariant());
                    return;
                }
                devices = filteredDevices;
            }

            logger.LogInformation("Found {DeviceCount} compatible device(s):", devices.Count);
            foreach (var device in devices)
            {
                logger.LogInformation("  - {DeviceName} ({ConnectionType})", device.Name, device.ConnectionType);
            }

            // Connect to the first available device
            var selectedDevice = devices[0];
            logger.LogInformation("Connecting to {DeviceName} via {ConnectionType}...", selectedDevice.Name, selectedDevice.ConnectionType);

            using (var client = await MeshCoreClient.ConnectAsync(selectedDevice, loggerFactory))
            {

                // Set up event handlers for real-time notifications
                client.MessageReceived += (sender, message) =>
                {
                    logger.LogInformation("?? New message from {FromContactId}: {Content}", message.FromContactId, message.Content);
                };

                client.ContactStatusChanged += (sender, contact) =>
                {
                    logger.LogInformation("?? Contact updated: {ContactName}", contact.Name);
                };

                client.NetworkStatusChanged += (sender, status) =>
                {
                    logger.LogInformation("?? Network status: {Status}", status.IsConnected ? "Connected" : "Disconnected");
                };

                client.ErrorOccurred += (sender, error) =>
                {
                    logger.LogError(error, "SDK Error occurred");
                };

                // Connect to the device
                await client.ConnectAsync();
                logger.LogInformation("Connected successfully!");

                // Get device information
                logger.LogInformation("=== Device Information ===");
                var deviceInfo = await client.GetDeviceInfoAsync();
                logger.LogInformation($"Device Info{deviceInfo}");

                // Sync device time
                logger.LogInformation("=== Time Synchronization ===");
                await client.SetDeviceTimeAsync(DateTime.UtcNow);
                var deviceTime = await client.TryGetDeviceTimeAsync();
                logger.LogInformation("Device time: {DeviceTime:yyyy-MM-dd HH:mm:ss} UTC", deviceTime);

                // Get network status
                var networkStatus = await client.GetNetworkStatusAsync();
                logger.LogInformation("=== Network Status ===");
                logger.LogInformation("Network: {NetworkName}", networkStatus.NetworkName ?? "Not connected");
                logger.LogInformation("Signal: {SignalStrength}%", networkStatus.SignalStrength);
                logger.LogInformation("Connected nodes: {ConnectedNodes}", networkStatus.ConnectedNodes);
                logger.LogInformation("Mode: {Mode}", networkStatus.Mode);

                // Get contacts
                logger.LogInformation("=== STARTING DETAILED CONTACT ANALYSIS ===");
                var contacts = (await client.GetContactsAsync(CancellationToken.None)).ToList();
                logger.LogInformation("=== Contacts ({ContactCount}) ===", contacts.Count);
                if (contacts.Any())
                {
                    logger.LogInformation("Displaying all contacts:");
                    for (int i = 0; i < contacts.Count; i++)
                    {
                        var contact = contacts[i];

                        logger.LogInformation("[{ContactIndex:D2}] {ContactName}", i + 1, contact.Name);
                        logger.LogInformation("       PublicKey: {PublicKey}", contact.PublicKey);
                    }
                }
                else
                {
                    logger.LogInformation("  No contacts found");
                }
                logger.LogInformation("=== END CONTACT ANALYSIS ===");

                // Get device configuration
                // Get battery and storage information
                var batteryInfo = await client.GetBatteryAndStorageAsync();
                logger.LogInformation("=== Battery & Storage Information ===");
                logger.LogInformation("Battery Voltage: {BatteryVoltage} mV ({BatteryVolts:F2} V)", batteryInfo.BatteryVoltage, batteryInfo.BatteryVoltage / 1000.0);
                logger.LogInformation("Used Storage: {UsedStorage} KB ({UsedStorageMB:F1} MB)", batteryInfo.UsedStorage, batteryInfo.UsedStorage / 1024.0);
                logger.LogInformation("Total Storage: {TotalStorage} KB ({TotalStorageMB:F1} MB)", batteryInfo.TotalStorage, batteryInfo.TotalStorage / 1024.0);
                logger.LogInformation("Storage Usage: {StoragePercentage:F1}%", batteryInfo.TotalStorage > 0 ? (batteryInfo.UsedStorage * 100.0) / batteryInfo.TotalStorage : 0);
            }
        }
        catch (NotImplementedException ex) when (ex.Message.Contains("Bluetooth"))
        {
            logger.LogWarning("Bluetooth LE support is not yet available in this version");
            logger.LogInformation("Please use --usb flag for USB connections");
            logger.LogInformation("Bluetooth LE support is planned for v2.0");
            logger.LogDebug(ex, "Bluetooth implementation details");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Demo execution failed");
            if (ex.InnerException != null)
                logger.LogError(ex.InnerException, "Inner exception details");
        }
        finally
        {
            logger.LogInformation("Basic demo completed");
        }
    }

    private static void ShowNoDevicesFound(DeviceConnectionType? preferredTransport, ILogger logger)
    {
        logger.LogWarning("No MeshCore devices found!");

        if (preferredTransport == DeviceConnectionType.USB)
        {
            logger.LogInformation("USB-specific search performed. Make sure a MeshCore device is:");
            logger.LogInformation("  - Connected via USB cable");
            logger.LogInformation("  - Powered on and recognized by your system");
            logger.LogInformation("  - Using the correct drivers");
            logger.LogInformation("Tip: Try without --usb flag to search all transport types");
        }
        else if (preferredTransport == DeviceConnectionType.BluetoothLE)
        {
            logger.LogInformation("Bluetooth LE search performed");
            logger.LogInformation("Note: Bluetooth LE support is coming in v2.0");
            logger.LogInformation("Try using --usb flag for USB devices");
        }
        else
        {
            logger.LogInformation("Make sure a MeshCore device is:");
            logger.LogInformation("  - Connected via USB cable (primary method)");
            logger.LogInformation("  - Powered on and recognized by your system");
            logger.LogInformation("  - Using the correct drivers");
            logger.LogInformation("Note: Bluetooth LE support is coming in v2.0");
        }
    }
}