using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Demo.Demos;

/// <summary>
/// Basic demonstration of MeshCore.Net.SDK with transport selection support
/// </summary>
public class BasicDemo
{
    public static async Task RunAsync(DeviceConnectionType? preferredTransport = null)
    {
        // Show transport preference
        if (preferredTransport.HasValue)
        {
            Console.WriteLine($"Looking for {preferredTransport.Value} devices specifically...");
        }
        else
        {
            Console.WriteLine("Discovering all available MeshCore devices...");
        }
        
        try
        {
            List<MeshCoreDevice> devices;
            
            // Device discovery based on transport preference
            if (preferredTransport == DeviceConnectionType.USB)
            {
                Console.WriteLine("Scanning for USB devices only...");
                devices = await UsbTransport.DiscoverDevicesAsync();
            }
            else if (preferredTransport == DeviceConnectionType.BluetoothLE)
            {
                Console.WriteLine("Scanning for Bluetooth LE devices only...");
                devices = await MeshCodeClient.DiscoverBluetoothDevicesAsync(TimeSpan.FromSeconds(10));
            }
            else
            {
                Console.WriteLine("Auto-detecting all device types...");
                devices = await MeshCodeClient.DiscoverDevicesAsync();
            }
            
            if (devices.Count == 0)
            {
                ShowNoDevicesFound(preferredTransport);
                return;
            }
            
            // Filter devices by preferred transport if specified
            if (preferredTransport.HasValue)
            {
                var filteredDevices = devices.Where(d => d.ConnectionType == preferredTransport.Value).ToList();
                if (filteredDevices.Count == 0)
                {
                    Console.WriteLine($"No {preferredTransport.Value} devices found!");
                    Console.WriteLine($"Found {devices.Count} device(s) of other types:");
                    foreach (var device in devices)
                    {
                        Console.WriteLine($"  - {device.Name} ({device.ConnectionType})");
                    }
                    Console.WriteLine($"\nTip: Remove --{preferredTransport.Value.ToString().ToLower()} flag to use any available device");
                    return;
                }
                devices = filteredDevices;
            }
            
            Console.WriteLine($"\nFound {devices.Count} compatible device(s):");
            foreach (var device in devices)
            {
                Console.WriteLine($"  - {device.Name} ({device.ConnectionType})");
            }
            
            // Connect to the first available device
            var selectedDevice = devices[0];
            Console.WriteLine($"\nConnecting to {selectedDevice.Name} via {selectedDevice.ConnectionType}...");
            
            using var client = new MeshCodeClient(selectedDevice);
            
            // Set up event handlers for real-time notifications
            client.MessageReceived += (sender, message) =>
            {
                Console.WriteLine($"New message from {message.FromContactId}: {message.Content}");
            };
            
            client.ContactStatusChanged += (sender, contact) =>
            {
                Console.WriteLine($"Contact {contact.Name} is now {contact.Status}");
            };
            
            client.NetworkStatusChanged += (sender, status) =>
            {
                Console.WriteLine($"Network status: {(status.IsConnected ? "Connected" : "Disconnected")}");
            };
            
            client.ErrorOccurred += (sender, error) =>
            {
                Console.WriteLine($"Error: {error.Message}");
            };
            
            // Connect to the device
            await client.ConnectAsync();
            Console.WriteLine("Connected successfully!");
            
            // Get device information
            var deviceInfo = await client.GetDeviceInfoAsync();
            Console.WriteLine($"\n=== Device Information ===");
            Console.WriteLine($"ID: {deviceInfo.DeviceId}");
            Console.WriteLine($"Firmware: {deviceInfo.FirmwareVersion}");
            Console.WriteLine($"Hardware: {deviceInfo.HardwareVersion}");
            Console.WriteLine($"Serial: {deviceInfo.SerialNumber}");
            Console.WriteLine($"Battery: {deviceInfo.BatteryLevel}%");
            Console.WriteLine($"Connection: {selectedDevice.ConnectionType}");
            
            // Sync device time
            Console.WriteLine($"\n=== Time Synchronization ===");
            await client.SetDeviceTimeAsync(DateTime.UtcNow);
            var deviceTime = await client.GetDeviceTimeAsync();
            Console.WriteLine($"Device time: {deviceTime:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Get network status
            var networkStatus = await client.GetNetworkStatusAsync();
            Console.WriteLine($"\n=== Network Status ===");
            Console.WriteLine($"Network: {networkStatus.NetworkName ?? "Not connected"}");
            Console.WriteLine($"Signal: {networkStatus.SignalStrength}%");
            Console.WriteLine($"Connected nodes: {networkStatus.ConnectedNodes}");
            Console.WriteLine($"Mode: {networkStatus.Mode}");
            
            // Get contacts
            Console.WriteLine("\n=== STARTING DETAILED CONTACT ANALYSIS ===");
            var contacts = await client.GetContactsAsync();
            Console.WriteLine($"\n=== Contacts ({contacts.Count}) ===");
            if (contacts.Any())
            {
                Console.WriteLine("Displaying all contacts:");
                for (int i = 0; i < contacts.Count; i++)
                {
                    var contact = contacts[i];
                    var status = contact.IsOnline ? "Online" : "Offline";
                    var lastSeen = contact.LastSeen?.ToString("HH:mm") ?? "Never";
                    var nodeIdPreview = contact.NodeId?.Length > 12 ? contact.NodeId[..12] + "..." : contact.NodeId ?? "N/A";
                    var contactIdPreview = contact.Id?.Length > 12 ? contact.Id[..12] + "..." : contact.Id ?? "N/A";
                    
                    Console.WriteLine($"  [{i+1:D2}] {contact.Name}");
                    Console.WriteLine($"       NodeID: {nodeIdPreview}");
                    Console.WriteLine($"       Status: {status} - Last seen: {lastSeen}");
                    Console.WriteLine($"       Contact ID: {contactIdPreview}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("  No contacts found");
            }
            Console.WriteLine("=== END CONTACT ANALYSIS ===");
            
            /* TEMPORARILY REMOVED TO FOCUS ON CONTACTS
            // Get recent messages
            var messages = await client.GetMessagesAsync();
            Console.WriteLine($"\n=== Recent Messages ({messages.Count}) ===");
            if (messages.Any())
            {
                foreach (var message in messages.Take(10)) // Show up to 10 messages
                {
                    var direction = message.FromContactId == deviceInfo.DeviceId || message.FromContactId == "self" ? "Sent" : "Received";
                    var timestamp = message.Timestamp.ToString("HH:mm:ss");
                    var preview = message.Content?.Length > 50 ? message.Content[..50] + "..." : message.Content ?? "(no content)";
                    var from = string.IsNullOrEmpty(message.FromContactId) ? "Unknown" : message.FromContactId;
                    
                    Console.WriteLine($"  [{timestamp}] From: {from} - {preview}");
                }
                if (messages.Count > 10)
                {
                    Console.WriteLine($"  ... and {messages.Count - 10} more messages (use advanced demo for full list)");
                }
            }
            else
            {
                Console.WriteLine("  No messages found");
                Console.WriteLine("  Note: If your radio shows unread messages, they may need to be");
                Console.WriteLine("        retrieved using a different method or there may be a parsing issue");
            }
            
            // Example: Send a test message (if there are contacts)
            if (contacts.Any())
            {
                var firstContact = contacts.First();
                Console.WriteLine($"\n=== Sending Test Message ===");
                Console.WriteLine($"Sending test message to {firstContact.Name}...");
                
                try
                {
                    // Validate that the contact has a valid ID before attempting to send
                    if (string.IsNullOrEmpty(firstContact.Id))
                    {
                        Console.WriteLine("Cannot send message: Contact ID is null or empty");
                        Console.WriteLine("This may indicate an issue with contact parsing from the device");
                    }
                    else
                    {
                        var testContent = $"Hello from C# SDK via {selectedDevice.ConnectionType} at {DateTime.Now:HH:mm:ss}!";
                        var sentMessage = await client.SendMessageAsync(firstContact.Id, testContent);
                        Console.WriteLine($"Message sent successfully!");
                        Console.WriteLine($"Message ID: {sentMessage.Id}");
                        Console.WriteLine($"Status: {sentMessage.Status}");
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"Failed to send message: {ex.Message}");
                    Console.WriteLine("This indicates a validation issue with the message parameters");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send message: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"\n=== Sending Test Message ===");
                Console.WriteLine("No contacts available to send message to");
                Console.WriteLine("Note: The MeshCore device may not have any contacts configured,");
                Console.WriteLine("      or contact parsing may need further implementation");
            }
            
            // Get device configuration
            var config = await client.GetConfigurationAsync();
            Console.WriteLine($"\n=== Device Configuration ===");
            Console.WriteLine($"Device Name: {config.DeviceName ?? "Default"}");
            Console.WriteLine($"TX Power: {config.TransmitPower}%");
            Console.WriteLine($"Channel: {config.Channel}");
            Console.WriteLine($"Auto Relay: {(config.AutoRelay ? "Enabled" : "Disabled")}");
            Console.WriteLine($"Heartbeat: {config.HeartbeatInterval.TotalSeconds}s");
            Console.WriteLine($"Message Timeout: {config.MessageTimeout.TotalMinutes}min");
            
            // Keep the connection alive to receive any incoming messages
            Console.WriteLine($"\n=== Listening for Messages ===");
            Console.WriteLine("Listening for incoming messages for 10 seconds...");
            Console.WriteLine("(Try sending a message from another device)");
            await Task.Delay(10000);
            END TEMPORARILY REMOVED */
        }
        catch (NotImplementedException ex) when (ex.Message.Contains("Bluetooth"))
        {
            Console.WriteLine("Bluetooth LE support is not yet available in this version.");
            Console.WriteLine("Please use --usb flag for USB connections");
            Console.WriteLine("Bluetooth LE support is planned for v2.0");
            Console.WriteLine($"Details: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Details: {ex.InnerException.Message}");
        }
        finally
        {
            Console.WriteLine("\nBasic demo completed.");
        }
    }
    
    private static void ShowNoDevicesFound(DeviceConnectionType? preferredTransport)
    {
        Console.WriteLine("No MeshCore devices found!");
        
        if (preferredTransport == DeviceConnectionType.USB)
        {
            Console.WriteLine("USB-specific search performed. Make sure a MeshCore device is:");
            Console.WriteLine("  - Connected via USB cable");
            Console.WriteLine("  - Powered on and recognized by your system");
            Console.WriteLine("  - Using the correct drivers");
            Console.WriteLine("\nTip: Try without --usb flag to search all transport types");
        }
        else if (preferredTransport == DeviceConnectionType.BluetoothLE)
        {
            Console.WriteLine("Bluetooth LE search performed.");
            Console.WriteLine("Note: Bluetooth LE support is coming in v2.0");
            Console.WriteLine("Try using --usb flag for USB devices");
        }
        else
        {
            Console.WriteLine("Make sure a MeshCore device is:");
            Console.WriteLine("  - Connected via USB cable (primary method)");
            Console.WriteLine("  - Powered on and recognized by your system");
            Console.WriteLine("  - Using the correct drivers");
            Console.WriteLine("\nNote: Bluetooth LE support is coming in v2.0");
        }
    }
    
    private static string GetConnectionIcon(DeviceConnectionType connectionType)
    {
        return connectionType switch
        {
            DeviceConnectionType.USB => "USB",
            DeviceConnectionType.BluetoothLE => "BLE",
            DeviceConnectionType.Bluetooth => "Bluetooth",
            _ => "Unknown"
        };
    }
    
    private static string GetSignalIcon(int signalStrength)
    {
        return signalStrength switch
        {
            >= 80 => "Excellent",
            >= 60 => "Good",
            >= 40 => "Fair",
            >= 20 => "Poor",
            _ => "Very Poor"
        };
    }
}