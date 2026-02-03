using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Transport;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Debug test specifically for message retrieval issues
/// </summary>
[Collection("SequentialTests")]
public class MessageDebugTest
{
    [Fact]
    public async Task DebugGetMessagesAsync()
    {
        // Test message retrieval on a real device
        var devices = await UsbTransport.DiscoverDevicesAsync();
        
        if (devices.Count == 0)
        {
            // Skip test if no device available
            Assert.True(true, "No device available for testing");
            return;
        }
        
        var device = devices.First();
        using var client = new MeshCodeClient(device);
        
        try
        {
            Console.WriteLine("DEBUG: Connecting to device...");
            await client.ConnectAsync();
            
            Console.WriteLine("DEBUG: Getting messages...");
            var messages = await client.GetMessagesAsync();
            
            Console.WriteLine($"DEBUG: Retrieved {messages.Count} messages");
            foreach (var message in messages)
            {
                Console.WriteLine($"  - {message.Content} (from: {message.FromContactId})");
            }
            
            Assert.True(true, $"Successfully retrieved {messages.Count} messages");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DEBUG: Exception occurred: {ex}");
            throw;
        }
    }
}