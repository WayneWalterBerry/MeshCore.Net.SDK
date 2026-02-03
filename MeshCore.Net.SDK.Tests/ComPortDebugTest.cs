using System;
using System.Threading.Tasks;
using MeshCore.Net.SDK.Transport;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Manual test for debugging specific COM port communication
/// </summary>
public class ComPortDebugTest
{
    /// <summary>
    /// Test a specific COM port to see if it's a MeshCore device
    /// </summary>
    public static async Task TestSpecificPortAsync(string portName = "COM3")
    {
        Console.WriteLine($"?? Testing {portName} for MeshCore communication...");
        
        try
        {
            using var transport = new UsbTransport(portName);
            Console.WriteLine($"? Transport created for {portName}");
            
            // Try to connect
            Console.WriteLine($"?? Attempting to connect to {portName}...");
            await transport.ConnectAsync();
            Console.WriteLine($"? Connected to {portName}");
            
            // Try to send a query command
            Console.WriteLine($"?? Sending device query command...");
            var response = await transport.SendCommandAsync(
                MeshCoreCommand.CMD_DEVICE_QUERY,
                timeout: TimeSpan.FromSeconds(5)); // Longer timeout for testing
            
            Console.WriteLine($"? Received response!");
            Console.WriteLine($"   Status: {response.GetStatus()}");
            Console.WriteLine($"   Command: {response.GetCommand()}");
            Console.WriteLine($"   Data Length: {response.GetDataPayload()?.Length ?? 0} bytes");
            
            if (response.GetStatus() == MeshCoreStatus.Success)
            {
                Console.WriteLine($"?? {portName} is a valid MeshCore device!");
            }
            else
            {
                Console.WriteLine($"?? {portName} responded but with status: {response.GetStatus()}");
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"? Access denied to {portName}: {ex.Message}");
            Console.WriteLine($"   Tip: Make sure no other application is using this port");
        }
        catch (DeviceConnectionException ex)
        {
            Console.WriteLine($"? Connection failed to {portName}: {ex.Message}");
            Console.WriteLine($"   Inner exception: {ex.InnerException?.Message}");
        }
        catch (MeshCoreTimeoutException ex)
        {
            Console.WriteLine($"? Timeout waiting for response from {portName}: {ex.Message}");
            Console.WriteLine($"   This usually means the device is not a MeshCore device");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"?? Unexpected error with {portName}: {ex.GetType().Name}");
            Console.WriteLine($"   Message: {ex.Message}");
            Console.WriteLine($"   Stack trace: {ex.StackTrace}");
        }
    }
    
    /// <summary>
    /// Test all available COM ports with detailed logging
    /// </summary>
    public static async Task TestAllPortsAsync()
    {
        var ports = System.IO.Ports.SerialPort.GetPortNames();
        Console.WriteLine($"?? Found {ports.Length} serial ports: {string.Join(", ", ports)}");
        
        foreach (var port in ports)
        {
            Console.WriteLine($"\n{'='*50}");
            await TestSpecificPortAsync(port);
        }
    }
}