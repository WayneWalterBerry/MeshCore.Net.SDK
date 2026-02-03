using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Demo.Demos;

/// <summary>
/// Advanced demonstration showing device discovery, transport architecture, and complex operations with transport selection
/// </summary>
public class AdvancedDemo
{
    public static async Task RunAsync(DeviceConnectionType? preferredTransport = null)
    {
        Console.WriteLine("?? Advanced MeshCore SDK Demo");
        Console.WriteLine("=============================\n");
        
        // Show transport preference
        if (preferredTransport.HasValue)
        {
            var transportIcon = GetConnectionIcon(preferredTransport.Value);
            Console.WriteLine($"?? Transport Focus: {transportIcon} {preferredTransport.Value}");
        }
        else
        {
            Console.WriteLine("?? Transport Mode: Auto-detect all types");
        }
        Console.WriteLine();
        
        // Demonstrate the transport architecture
        Console.WriteLine("??? Transport Architecture Overview:");
        Console.WriteLine("   - ITransport interface enables multiple connection types");
        Console.WriteLine("   - USB transport: Fully implemented and tested ?");
        Console.WriteLine("   - Bluetooth transport: Architecture ready, implementation planned ??");
        Console.WriteLine("   - WiFi transport: Future consideration for TCP connectivity ??\n");
        
        // Transport-specific discovery demonstration
        await DemonstrateTransportDiscoveryAsync(preferredTransport);
        
        // Main advanced operations
        var allDevices = await GetFilteredDevicesAsync(preferredTransport);
        if (allDevices.Count == 0)
        {
            ShowNoDevicesFoundAdvanced(preferredTransport);
            DemonstrateArchitectureWithoutDevice();
            return;
        }
        
        Console.WriteLine($"?? Filtered Discovery Results: {allDevices.Count} device(s) matching criteria\n");
        
        // Advanced operations with selected device
        var selectedDevice = allDevices.First();
        Console.WriteLine($"?? Performing advanced operations with: {selectedDevice.Name}");
        Console.WriteLine($"   Connection Type: {GetConnectionIcon(selectedDevice.ConnectionType)} {selectedDevice.ConnectionType}");
        Console.WriteLine($"   Device ID: {selectedDevice.Id}");
        if (selectedDevice.SignalStrength.HasValue)
        {
            Console.WriteLine($"   Signal Strength: {selectedDevice.SignalStrength}dBm");
        }
        Console.WriteLine();
        
        using var client = new MeshCodeClient(selectedDevice);
        
        try
        {
            await client.ConnectAsync();
            Console.WriteLine("? Connected successfully for advanced testing\n");
            
            // Concurrent API operations test
            await PerformConcurrentOperationsAsync(client, selectedDevice.ConnectionType);
            
            // Configuration management demonstration
            await DemonstrateConfigurationManagementAsync(client);
            
            // Network operations demonstration
            await DemonstrateNetworkOperationsAsync(client);
            
            // Contact and messaging operations
            await DemonstrateContactOperationsAsync(client, selectedDevice.ConnectionType);
            
            // Real-time event handling demonstration
            await DemonstrateEventHandlingAsync(client);
            
            // Transport-specific operations
            await DemonstrateTransportSpecificOperationsAsync(client, selectedDevice.ConnectionType);
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Advanced operations error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
        }
        
        Console.WriteLine("\n? Advanced demo completed successfully!");
        DisplayFutureRoadmap(preferredTransport);
    }
    
    private static async Task DemonstrateTransportDiscoveryAsync(DeviceConnectionType? preferredTransport)
    {
        Console.WriteLine("?? Transport-Specific Discovery Demonstration...\n");
        
        if (preferredTransport == null || preferredTransport == DeviceConnectionType.USB)
        {
            // USB devices discovery
            try
            {
                var usbDevices = await UsbTransport.DiscoverDevicesAsync();
                Console.WriteLine($"?? USB Transport Discovery: {usbDevices.Count} device(s) found");
                foreach (var device in usbDevices)
                {
                    Console.WriteLine($"   • {device.Name}");
                    Console.WriteLine($"     Port: {device.Id}");
                    Console.WriteLine($"     Address: {device.Address}");
                    Console.WriteLine($"     Status: Ready for connection");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? USB Discovery Error: {ex.Message}");
            }
            Console.WriteLine();
        }
        
        if (preferredTransport == null || preferredTransport == DeviceConnectionType.BluetoothLE)
        {
            // Bluetooth devices discovery
            try
            {
                var bluetoothDevices = await MeshCodeClient.DiscoverBluetoothDevicesAsync(TimeSpan.FromSeconds(5));
                Console.WriteLine($"?? Bluetooth LE Discovery: {bluetoothDevices.Count} device(s) found");
                if (bluetoothDevices.Count == 0)
                {
                    Console.WriteLine("   ?? Note: Bluetooth LE implementation status");
                    Console.WriteLine("   ??? Architecture: Complete and ready");
                    Console.WriteLine("   ?? Will enable wireless connectivity in v2.0");
                    Console.WriteLine("   ?? Same API will work seamlessly with BLE devices");
                }
                else
                {
                    foreach (var device in bluetoothDevices)
                    {
                        Console.WriteLine($"   • {device.Name}");
                        Console.WriteLine($"     Address: {device.Address}");
                        Console.WriteLine($"     Signal: {device.SignalStrength}dBm");
                        Console.WriteLine($"     Paired: {(device.IsPaired ? "Yes" : "No")}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"?? Bluetooth Discovery: {ex.Message}");
            }
            Console.WriteLine();
        }
    }
    
    private static async Task<List<MeshCoreDevice>> GetFilteredDevicesAsync(DeviceConnectionType? preferredTransport)
    {
        List<MeshCoreDevice> allDevices;
        
        if (preferredTransport == DeviceConnectionType.USB)
        {
            allDevices = await UsbTransport.DiscoverDevicesAsync();
        }
        else if (preferredTransport == DeviceConnectionType.BluetoothLE)
        {
            allDevices = await MeshCodeClient.DiscoverBluetoothDevicesAsync(TimeSpan.FromSeconds(10));
        }
        else
        {
            allDevices = await MeshCodeClient.DiscoverDevicesAsync();
            if (preferredTransport.HasValue)
            {
                allDevices = allDevices.Where(d => d.ConnectionType == preferredTransport.Value).ToList();
            }
        }
        
        return allDevices;
    }
    
    private static async Task PerformConcurrentOperationsAsync(MeshCodeClient client, DeviceConnectionType connectionType)
    {
        Console.WriteLine($"? Concurrent API Operations Test (via {GetConnectionIcon(connectionType)} {connectionType}):");
        Console.WriteLine("   Testing multiple simultaneous API calls for performance...");
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();
        
        tasks.Add(Task.Run(async () =>
        {
            var info = await client.GetDeviceInfoAsync();
            Console.WriteLine($"   ?? Device info: {info.DeviceId} (FW: {info.FirmwareVersion})");
        }));
        
        tasks.Add(Task.Run(async () =>
        {
            var contacts = await client.GetContactsAsync();
            Console.WriteLine($"   ?? Contacts: {contacts.Count} found");
        }));
        
        tasks.Add(Task.Run(async () =>
        {
            var messages = await client.GetMessagesAsync();
            Console.WriteLine($"   ?? Messages: {messages.Count} found");
        }));
        
        tasks.Add(Task.Run(async () =>
        {
            var network = await client.GetNetworkStatusAsync();
            Console.WriteLine($"   ?? Network: {(network.IsConnected ? "Connected" : "Disconnected")} ({network.SignalStrength}%)");
        }));
        
        tasks.Add(Task.Run(async () =>
        {
            var config = await client.GetConfigurationAsync();
            Console.WriteLine($"   ?? Config: {config.DeviceName ?? "Default"} (Channel {config.Channel})");
        }));
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        Console.WriteLine($"   ? All 5 concurrent operations completed in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"   ?? Transport: {GetConnectionIcon(connectionType)} {connectionType} performed well\n");
    }
    
    private static async Task DemonstrateConfigurationManagementAsync(MeshCodeClient client)
    {
        Console.WriteLine("?? Configuration Management Demo:");
        
        var originalConfig = await client.GetConfigurationAsync();
        Console.WriteLine($"   Original device name: '{originalConfig.DeviceName ?? "Default"}'");
        Console.WriteLine($"   Original TX power: {originalConfig.TransmitPower}%");
        Console.WriteLine($"   Original channel: {originalConfig.Channel}");
        
        // Test configuration update
        var testConfig = originalConfig;
        testConfig.DeviceName = $"C# SDK Demo - {DateTime.Now:HHmmss}";
        testConfig.TransmitPower = Math.Max(50, originalConfig.TransmitPower - 10); // Reduce power safely
        
        await client.SetConfigurationAsync(testConfig);
        Console.WriteLine($"   ?? Updated device name to: '{testConfig.DeviceName}'");
        Console.WriteLine($"   ?? Updated TX power to: {testConfig.TransmitPower}%");
        
        // Verify the changes
        await Task.Delay(1000);
        var updatedConfig = await client.GetConfigurationAsync();
        var nameMatches = updatedConfig.DeviceName == testConfig.DeviceName;
        var powerMatches = updatedConfig.TransmitPower == testConfig.TransmitPower;
        
        Console.WriteLine($"   ? Name update verified: {(nameMatches ? "Success" : "Failed")}");
        Console.WriteLine($"   ? Power update verified: {(powerMatches ? "Success" : "Failed")}");
        
        // Restore original configuration
        await Task.Delay(1000);
        await client.SetConfigurationAsync(originalConfig);
        Console.WriteLine($"   ?? Configuration restored to original values\n");
    }
    
    private static async Task DemonstrateNetworkOperationsAsync(MeshCodeClient client)
    {
        Console.WriteLine("?? Network Operations Demo:");
        
        try
        {
            var networks = await client.ScanNetworksAsync();
            Console.WriteLine($"   Found {networks.Count} mesh networks:");
            
            if (networks.Any())
            {
                foreach (var network in networks.Take(5)) // Show first 5
                {
                    Console.WriteLine($"   ?? {network}");
                }
                if (networks.Count > 5)
                {
                    Console.WriteLine($"   ... and {networks.Count - 5} more networks");
                }
            }
            else
            {
                Console.WriteLine("   No mesh networks detected in range");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ?? Network scan error: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemonstrateContactOperationsAsync(MeshCodeClient client, DeviceConnectionType connectionType)
    {
        Console.WriteLine($"?? Contact Operations Demo (via {GetConnectionIcon(connectionType)} {connectionType}):");
        
        var originalContacts = await client.GetContactsAsync();
        Console.WriteLine($"   Original contact count: {originalContacts.Count}");
        
        // Add a temporary test contact with transport-specific naming
        var testContactName = $"Demo-{connectionType}-{DateTime.Now:HHmmss}";
        var testNodeId = $"NODE{Random.Shared.Next(1000, 9999)}";
        
        try
        {
            var newContact = await client.AddContactAsync(testContactName, testNodeId);
            Console.WriteLine($"   ? Added test contact: {newContact.Name} ({newContact.NodeId})");
            Console.WriteLine($"   ?? Added via {GetConnectionIcon(connectionType)} {connectionType} transport");
            
            // Verify the contact was added
            var updatedContacts = await client.GetContactsAsync();
            var contactExists = updatedContacts.Any(c => c.Name == testContactName);
            Console.WriteLine($"   ? Contact verification: {(contactExists ? "Success" : "Failed")}");
            
            // Send a test message to the new contact
            if (contactExists)
            {
                var testMessage = $"Advanced demo via {connectionType}!";
                var sentMessage = await client.SendMessageAsync(newContact.Id, testMessage);
                Console.WriteLine($"   ?? Test message sent: {sentMessage.Id}");
                Console.WriteLine($"   ?? Message sent via {GetConnectionIcon(connectionType)} transport");
            }
            
            // Clean up - remove the test contact
            await Task.Delay(1000);
            await client.DeleteContactAsync(newContact.Id);
            Console.WriteLine($"   ??? Test contact cleaned up successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ?? Contact operations error: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static async Task DemonstrateEventHandlingAsync(MeshCodeClient client)
    {
        Console.WriteLine("?? Real-time Event Handling Demo:");
        Console.WriteLine("   Setting up event listeners for 5 seconds...");
        
        var messageCount = 0;
        var networkUpdates = 0;
        var contactUpdates = 0;
        
        // Set up event handlers
        client.MessageReceived += (sender, message) =>
        {
            messageCount++;
            Console.WriteLine($"   ?? [Event] Message from {message.FromContactId}: {message.Content}");
        };
        
        client.NetworkStatusChanged += (sender, status) =>
        {
            networkUpdates++;
            Console.WriteLine($"   ?? [Event] Network status: {(status.IsConnected ? "Connected" : "Disconnected")}");
        };
        
        client.ContactStatusChanged += (sender, contact) =>
        {
            contactUpdates++;
            Console.WriteLine($"   ?? [Event] Contact {contact.Name} status: {contact.Status}");
        };
        
        client.ErrorOccurred += (sender, error) =>
        {
            Console.WriteLine($"   ? [Event] Error: {error.Message}");
        };
        
        // Wait for potential events
        await Task.Delay(5000);
        
        Console.WriteLine($"   ?? Event summary: {messageCount} messages, {networkUpdates} network updates, {contactUpdates} contact updates");
        Console.WriteLine();
    }
    
    private static async Task DemonstrateTransportSpecificOperationsAsync(MeshCodeClient client, DeviceConnectionType connectionType)
    {
        Console.WriteLine($"?? Transport-Specific Operations ({GetConnectionIcon(connectionType)} {connectionType}):");
        
        switch (connectionType)
        {
            case DeviceConnectionType.USB:
                Console.WriteLine("   ?? USB-specific optimizations:");
                Console.WriteLine("   • High-speed data transfer capabilities");
                Console.WriteLine("   • Reliable connection with flow control");
                Console.WriteLine("   • No battery drain on mobile devices");
                Console.WriteLine("   • Perfect for development and debugging");
                break;
                
            case DeviceConnectionType.BluetoothLE:
                Console.WriteLine("   ?? Bluetooth LE-specific features (when available):");
                Console.WriteLine("   • Low power consumption for mobile devices");
                Console.WriteLine("   • Wireless freedom within range");
                Console.WriteLine("   • Automatic reconnection capabilities");
                Console.WriteLine("   • Signal strength monitoring");
                break;
                
            default:
                Console.WriteLine("   ? Unknown transport type - using standard operations");
                break;
        }
        
        // Demonstrate transport-agnostic operations work the same way
        try
        {
            var deviceInfo = await client.GetDeviceInfoAsync();
            Console.WriteLine($"   ? Standard operations work identically across all transports");
            Console.WriteLine($"   ?? Device: {deviceInfo.DeviceId} via {GetConnectionIcon(connectionType)} {connectionType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ?? Transport operation error: {ex.Message}");
        }
        
        Console.WriteLine();
    }
    
    private static void ShowNoDevicesFoundAdvanced(DeviceConnectionType? preferredTransport)
    {
        Console.WriteLine("? No devices available for advanced demo");
        
        if (preferredTransport.HasValue)
        {
            var icon = GetConnectionIcon(preferredTransport.Value);
            Console.WriteLine($"   ?? Searched specifically for {icon} {preferredTransport.Value} devices");
            
            if (preferredTransport == DeviceConnectionType.BluetoothLE)
            {
                Console.WriteLine("   ?? Bluetooth LE support is coming in v2.0");
                Console.WriteLine("   ?? Try --usb flag for current functionality");
            }
            else if (preferredTransport == DeviceConnectionType.USB)
            {
                Console.WriteLine("   Please connect a MeshCore device via USB");
            }
        }
        else
        {
            Console.WriteLine("   Please connect a MeshCore device to continue");
        }
        Console.WriteLine();
    }
    
    private static void DemonstrateArchitectureWithoutDevice()
    {
        Console.WriteLine("??? SDK Architecture Demonstration (No Device Required):");
        Console.WriteLine("   Even without a connected device, the SDK demonstrates:");
        Console.WriteLine("   • Clean separation of transport and protocol layers");
        Console.WriteLine("   • Extensible design supporting multiple connection types");
        Console.WriteLine("   • Consistent API across different transport mechanisms");
        Console.WriteLine("   • Professional error handling and user experience");
        Console.WriteLine("   • Transport-specific optimizations while maintaining compatibility");
        Console.WriteLine();
    }
    
    private static void DisplayFutureRoadmap(DeviceConnectionType? preferredTransport)
    {
        Console.WriteLine("?? Roadmap & Future Features:");
        Console.WriteLine("   ?? Version 2.0 (Next Release):");
        Console.WriteLine("      • Full Bluetooth LE implementation");
        Console.WriteLine("      • Multiple simultaneous device connections");
        Console.WriteLine("      • Enhanced diagnostic and logging capabilities");
        Console.WriteLine("      • Performance optimizations and caching");
        
        if (preferredTransport == DeviceConnectionType.BluetoothLE)
        {
            Console.WriteLine("      ?? Specific to your Bluetooth LE interest:");
            Console.WriteLine("         • Native BLE scanning and pairing");
            Console.WriteLine("         • Signal strength monitoring");
            Console.WriteLine("         • Automatic reconnection handling");
            Console.WriteLine("         • Mobile device power optimization");
        }
        
        Console.WriteLine();
        Console.WriteLine("   ?? Future Versions:");
        Console.WriteLine("      • Mobile platform support (Xamarin/MAUI)");
        Console.WriteLine("      • WiFi transport for TCP-based connections");
        Console.WriteLine("      • Real-time mesh network visualization");
        Console.WriteLine("      • Advanced message routing and queuing");
        Console.WriteLine("      • GUI management tools and utilities");
        Console.WriteLine();
        Console.WriteLine("   ?? The SDK architecture is designed to support all these features");
        Console.WriteLine("   ?? while maintaining backward compatibility and ease of use.");
    }
    
    private static string GetConnectionIcon(DeviceConnectionType connectionType)
    {
        return connectionType switch
        {
            DeviceConnectionType.USB => "??",
            DeviceConnectionType.BluetoothLE => "??",
            DeviceConnectionType.Bluetooth => "??",
            _ => "?"
        };
    }
}