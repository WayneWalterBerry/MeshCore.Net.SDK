using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Models;

namespace MeshCore.Net.SDK.Examples;

/// <summary>
/// Example usage of the MeshCore.Net.SDK
/// </summary>
public class BasicUsageExample
{
    public static async Task RunExampleAsync()
    {
        // Discover available MeshCore devices
        Console.WriteLine("Discovering MeshCore devices...");
        var devices = await MeshCodeClient.DiscoverDevicesAsync();
        
        if (devices.Count == 0)
        {
            Console.WriteLine("No MeshCore devices found!");
            return;
        }
        
        Console.WriteLine($"Found {devices.Count} device(s): {string.Join(", ", devices)}");
        
        // Connect to the first available device
        using var client = new MeshCodeClient(devices[0]);
        
        // Set up event handlers
        client.MessageReceived += (sender, message) =>
        {
            Console.WriteLine($"?? New message from {message.FromContactId}: {message.Content}");
        };
        
        client.ContactStatusChanged += (sender, contact) =>
        {
            Console.WriteLine($"?? Contact {contact.Name} is now {contact.Status}");
        };
        
        client.NetworkStatusChanged += (sender, status) =>
        {
            Console.WriteLine($"?? Network status: {(status.IsConnected ? "Connected" : "Disconnected")}");
        };
        
        client.ErrorOccurred += (sender, error) =>
        {
            Console.WriteLine($"? Error: {error.Message}");
        };
        
        try
        {
            // Connect to the device
            Console.WriteLine($"Connecting to device on {devices[0]}...");
            await client.ConnectAsync();
            Console.WriteLine("? Connected successfully!");
            
            // Get device information
            var deviceInfo = await client.GetDeviceInfoAsync();
            Console.WriteLine($"?? Device: {deviceInfo.DeviceId}");
            Console.WriteLine($"?? Firmware: {deviceInfo.FirmwareVersion}");
            Console.WriteLine($"?? Hardware: {deviceInfo.HardwareVersion}");
            Console.WriteLine($"?? Battery: {deviceInfo.BatteryLevel}%");
            
            // Sync device time
            await client.SetDeviceTimeAsync(DateTime.UtcNow);
            var deviceTime = await client.GetDeviceTimeAsync();
            Console.WriteLine($"? Device time synchronized: {deviceTime:yyyy-MM-dd HH:mm:ss} UTC");
            
            // Get network status
            var networkStatus = await client.GetNetworkStatusAsync();
            Console.WriteLine($"?? Network: {networkStatus.NetworkName ?? "Not connected"}");
            Console.WriteLine($"?? Signal: {networkStatus.SignalStrength}%");
            Console.WriteLine($"?? Nodes: {networkStatus.ConnectedNodes}");
            
            // Get contacts
            var contacts = await client.GetContactsAsync();
            Console.WriteLine($"?? Contacts ({contacts.Count}):");
            foreach (var contact in contacts.Take(5)) // Show first 5
            {
                var status = contact.IsOnline ? "??" : "??";
                Console.WriteLine($"  {status} {contact.Name} ({contact.NodeId})");
            }
            
            // Get recent messages
            var messages = await client.GetMessagesAsync();
            Console.WriteLine($"?? Recent messages ({messages.Count}):");
            foreach (var message in messages.Take(3)) // Show last 3
            {
                var direction = message.FromContactId == deviceInfo.DeviceId ? "??" : "??";
                var timestamp = message.Timestamp.ToString("HH:mm");
                Console.WriteLine($"  {direction} [{timestamp}] {message.Content}");
            }
            
            // Example: Send a message (if there are contacts)
            if (contacts.Any())
            {
                var firstContact = contacts.First();
                Console.WriteLine($"?? Sending test message to {firstContact.Name}...");
                
                var sentMessage = await client.SendMessageAsync(firstContact.Id, "Hello from C# SDK!");
                Console.WriteLine($"? Message sent: {sentMessage.Id}");
            }
            
            // Example: Get device configuration
            var config = await client.GetConfigurationAsync();
            Console.WriteLine($"??  Configuration:");
            Console.WriteLine($"  Device Name: {config.DeviceName}");
            Console.WriteLine($"  TX Power: {config.TransmitPower}%");
            Console.WriteLine($"  Channel: {config.Channel}");
            Console.WriteLine($"  Auto Relay: {(config.AutoRelay ? "Enabled" : "Disabled")}");
            
            // Keep the connection alive for a moment to receive any incoming messages
            Console.WriteLine("? Listening for incoming messages for 10 seconds...");
            await Task.Delay(10000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("?? Disconnecting...");
            client.Disconnect();
        }
    }
}

/// <summary>
/// Advanced example showing more complex operations
/// </summary>
public class AdvancedUsageExample
{
    public static async Task RunAdvancedExampleAsync()
    {
        var devices = await MeshCodeClient.DiscoverDevicesAsync();
        if (devices.Count == 0) return;
        
        using var client = new MeshCodeClient(devices[0]);
        await client.ConnectAsync();
        
        // Add a new contact
        Console.WriteLine("? Adding new contact...");
        var newContact = await client.AddContactAsync("Test Contact", "NODE123456");
        Console.WriteLine($"? Added contact: {newContact.Name} ({newContact.Id})");
        
        // Send multiple messages
        var testMessages = new[]
        {
            "Hello from C# SDK!",
            "This is a test message ??",
            "Testing MeshCore communication ??"
        };
        
        foreach (var messageText in testMessages)
        {
            var message = await client.SendMessageAsync(newContact.Id, messageText);
            Console.WriteLine($"?? Sent: {message.Content}");
            await Task.Delay(1000); // Small delay between messages
        }
        
        // Update device configuration
        Console.WriteLine("?? Updating device configuration...");
        var config = await client.GetConfigurationAsync();
        config.DeviceName = "My C# MeshCore Device";
        config.TransmitPower = 80;
        config.AutoRelay = true;
        
        await client.SetConfigurationAsync(config);
        Console.WriteLine("? Configuration updated");
        
        // Scan for networks
        Console.WriteLine("?? Scanning for networks...");
        var networks = await client.ScanNetworksAsync();
        Console.WriteLine($"Found {networks.Count} networks:");
        foreach (var network in networks)
        {
            Console.WriteLine($"  ?? {network}");
        }
        
        // Clean up - delete the test contact
        await client.DeleteContactAsync(newContact.Id);
        Console.WriteLine($"??? Deleted test contact: {newContact.Name}");
    }
}