# MeshCore.Net.SDK

A comprehensive C# SDK for communicating with MeshCore devices via USB serial protocol.

[![Build and Release](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/actions/workflows/build-and-release.yml/badge.svg)](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/actions/workflows/build-and-release.yml)
[![NuGet Version](https://img.shields.io/nuget/v/MeshCore.Net.SDK)](https://www.nuget.org/packages/MeshCore.Net.SDK/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/MeshCore.Net.SDK)](https://www.nuget.org/packages/MeshCore.Net.SDK/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## ?? Features

- **Full USB Protocol Support**: Complete implementation of the MeshCore Companion Radio Protocol
- **Async/Await Pattern**: Modern asynchronous programming support
- **Strongly Typed**: Type-safe enums and models for all MeshCore operations
- **Event-Driven**: Real-time notifications for messages, network status, and contact changes
- **Auto-Discovery**: Automatic detection of connected MeshCore devices
- **Error Handling**: Comprehensive exception handling with detailed error information
- **Cross-Platform**: Works on Windows, macOS, and Linux with .NET 8
- **Extensible Architecture**: Designed for multiple transport types (USB now, Bluetooth LE planned)

## ?? Current Status

| Transport Type | Status | Description |
|---------------|--------|-------------|
| **USB Serial** | ? **Fully Supported** | Complete implementation with device discovery |
| **Bluetooth LE** | ?? **Planned** | Architecture ready, implementation coming soon |
| **WiFi TCP** | ?? **Future** | Under consideration for future releases |

## ?? Project Structure

```
MeshCore.Net.SDK/
??? MeshCore.Net.SDK/           # Main SDK library
?   ??? Transport/              # Transport layer (USB + future BLE)
?   ?   ??? ITransport.cs      # Common transport interface
?   ?   ??? UsbTransport.cs    # USB implementation ?
?   ?   ??? BluetoothTransport.cs  # BLE placeholder ??
?   ??? Protocol/               # Protocol implementation
?   ??? Models/                 # Data models
?   ??? Exceptions/             # Custom exceptions
?   ??? MeshCodeClient.cs       # Main client
??? MeshCore.Net.SDK.Tests/     # Unit tests
??? MeshCore.Net.SDK.Demo/      # Console demo application
?   ??? Demos/                  # Demo implementations
?   ?   ??? BasicDemo.cs       # Basic usage demonstration
?   ?   ??? AdvancedDemo.cs    # Advanced features demonstration
?   ??? Program.cs             # Demo entry point
??? .github/                    # CI/CD workflows and templates
??? scripts/                    # Release automation scripts
```

## ?? Installation

### NuGet Package (Recommended)
```bash
dotnet add package MeshCore.Net.SDK
```

### From Source
```bash
# Clone the repository
git clone https://github.com/WayneWalterBerry/MeshCore.Net.SDK.git

# Build the entire solution
cd MeshCore.Net.SDK
dotnet build
```

## ?? Quick Demo

Run the included demo application with various options:

```bash
# Basic demo with auto-detected transport
dotnet run --project MeshCore.Net.SDK.Demo

# Advanced demo with transport architecture showcase
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced

# USB-specific demos
dotnet run --project MeshCore.Net.SDK.Demo -- --usb
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced --usb

# Bluetooth LE demos (architecture demonstration)
dotnet run --project MeshCore.Net.SDK.Demo -- --bluetooth
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced --ble

# Show help with all options
dotnet run --project MeshCore.Net.SDK.Demo -- --help
```

## ?? Quick Start

### Device Discovery and Connection

```csharp
using MeshCore.Net.SDK;

// Discover available devices (USB devices currently)
var devices = await MeshCodeClient.DiscoverDevicesAsync();
Console.WriteLine($"Found {devices.Count} MeshCore devices");

// Connect to the first device
using var client = new MeshCodeClient(devices[0]);
await client.ConnectAsync();

// Get device information
var deviceInfo = await client.GetDeviceInfoAsync();
Console.WriteLine($"Connected to: {deviceInfo.DeviceId} via {devices[0].ConnectionType}");
```

### Sending Messages

```csharp
// Get contacts
var contacts = await client.GetContactsAsync();

if (contacts.Any())
{
    // Send a message to the first contact
    var contact = contacts.First();
    var message = await client.SendMessageAsync(contact.Id, "Hello from C# SDK!");
    Console.WriteLine($"Message sent: {message.Id}");
}
```

### Real-time Events

```csharp
// Set up event handlers
client.MessageReceived += (sender, message) =>
{
    Console.WriteLine($"?? New message from {message.FromContactId}: {message.Content}");
};

client.ContactStatusChanged += (sender, contact) =>
{
    Console.WriteLine($"?? {contact.Name} is now {contact.Status}");
};

client.NetworkStatusChanged += (sender, status) =>
{
    Console.WriteLine($"?? Network: {(status.IsConnected ? "Connected" : "Disconnected")}");
};
```

### Future Bluetooth Usage (Coming Soon)

```csharp
// This will be available in future releases
var bluetoothDevices = await MeshCodeClient.DiscoverBluetoothDevicesAsync();
using var client = new MeshCodeClient(bluetoothDevices[0]); // BLE device
await client.ConnectAsync(); // Same API, different transport!
```

## ?? API Reference

### MeshCodeClient

The main client class for interacting with MeshCore devices.

#### Device Operations

- `Task ConnectAsync()` - Connect to the device
- `void Disconnect()` - Disconnect from the device
- `Task<DeviceInfo> GetDeviceInfoAsync()` - Get device information
- `Task SetDeviceTimeAsync(DateTime dateTime)` - Set device time
- `Task<DateTime> GetDeviceTimeAsync()` - Get device time
- `Task ResetDeviceAsync()` - Reset the device

#### Contact Management

- `Task<List<Contact>> GetContactsAsync()` - Get all contacts
- `Task<Contact> AddContactAsync(string name, string nodeId)` - Add a new contact
- `Task DeleteContactAsync(string contactId)` - Delete a contact

#### Messaging

- `Task<Message> SendMessageAsync(string toContactId, string content)` - Send a text message
- `Task<List<Message>> GetMessagesAsync()` - Get all messages
- `Task MarkMessageReadAsync(string messageId)` - Mark a message as read
- `Task DeleteMessageAsync(string messageId)` - Delete a message

#### Network Operations

- `Task<NetworkStatus> GetNetworkStatusAsync()` - Get current network status
- `Task<List<string>> ScanNetworksAsync()` - Scan for available networks

#### Configuration

- `Task<DeviceConfiguration> GetConfigurationAsync()` - Get device configuration
- `Task SetConfigurationAsync(DeviceConfiguration config)` - Set device configuration

#### Device Discovery

- `static Task<List<MeshCoreDevice>> DiscoverDevicesAsync()` - Discover all available devices
- `static Task<List<string>> DiscoverUsbDevicesAsync()` - USB devices only (backward compatibility)
- `static Task<List<MeshCoreDevice>> DiscoverBluetoothDevicesAsync()` - Bluetooth devices (future)

### Events

- `MessageReceived` - Fired when a new message is received
- `ContactStatusChanged` - Fired when a contact's status changes
- `NetworkStatusChanged` - Fired when network status changes
- `ErrorOccurred` - Fired when an error occurs

## ?? Protocol Details

This SDK implements the MeshCore Companion Radio Protocol:

### Frame Structure

**Inbound (PC ? Radio):**
- Start byte: `0x3C` ('<')
- Length: 2 bytes (little-endian)
- Payload: Variable length

**Outbound (Radio ? PC):**
- Start byte: `0x3E` ('>')
- Length: 2 bytes (little-endian)  
- Payload: Variable length

### Supported Commands

| Command | Value | Description |
|---------|-------|-------------|
| CMD_DEVICE_QUERY | 22 | Query device status |
| CMD_GET_DEVICE_INFO | 23 | Get device information |
| CMD_SET_DEVICE_TIME | 24 | Set device time |
| CMD_GET_CONTACTS | 30 | Get contact list |
| CMD_SEND_MESSAGE | 40 | Send a message |
| CMD_GET_NETWORK_STATUS | 50 | Get network status |

## ?? MeshCore Bluetooth Support

**Good News:** MeshCore officially supports Bluetooth LE connectivity! 

- ? **MeshCore BLE Companion firmware** exists and is officially supported
- ? **Same frame-based protocol** works over both USB and BLE
- ? **Python SDK already supports BLE** (`MeshCore.create_ble()`)
- ? **meshcore-cli supports BLE connections**

**Current SDK Status:**
- ?? **USB Transport**: Fully implemented and tested
- ?? **BLE Transport**: Architecture ready, implementation coming in v2.0

## ??? Demos

The included demo application shows practical usage of the SDK with transport selection options:

### Demo Commands

**Basic Demo** - Device connection and essential operations:
```bash
# Auto-detect any available transport
dotnet run --project MeshCore.Net.SDK.Demo

# USB devices only
dotnet run --project MeshCore.Net.SDK.Demo -- --usb

# Bluetooth LE devices only (v2.0 preview)
dotnet run --project MeshCore.Net.SDK.Demo -- --bluetooth
```

**Advanced Demo** - Transport architecture and complex operations:
```bash
# Advanced demo with auto-detection
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced

# Advanced demo with USB transport focus
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced --usb

# Advanced demo with Bluetooth LE architecture preview
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced --ble
```

**Help and Options:**
```bash
# Show all available options
dotnet run --project MeshCore.Net.SDK.Demo -- --help
```

### Demo Structure

The demo project (`MeshCore.Net.SDK.Demo`) contains:

- `Demos/BasicDemo.cs` - Basic device connection, messaging, and configuration with transport selection
- `Demos/AdvancedDemo.cs` - Advanced features, concurrent operations, and transport-specific demonstrations
- `Program.cs` - Entry point with command-line argument handling and help system

### Transport Selection

| Flag | Description | Status |
|------|-------------|---------|
| (none) | Auto-detect all transports, prefer USB | ? Available |
| `--usb` | USB serial devices only | ? Fully supported |
| `--bluetooth`, `--ble` | Bluetooth LE devices only | ?? Architecture ready, v2.0 |

The demo intelligently handles transport preferences and provides helpful feedback when devices aren't available or features aren't yet implemented.

## ?? Testing

Run the unit tests:

```bash
dotnet test
```

The test suite includes:
- Protocol frame parsing and generation
- Command and status code validation
- Model serialization/deserialization
- Exception handling scenarios

## ?? CI/CD and Releases

### Automated Builds
This project uses GitHub Actions for continuous integration:

- **Build and Test**: Runs on every push and pull request
- **Multi-platform Testing**: Tests on Windows, macOS, and Linux
- **Code Coverage**: Uploads coverage reports to Codecov
- **Demo Validation**: Ensures demo application compiles on all platforms

### Creating a Release

**Option 1: Using Release Scripts (Recommended)**
```bash
# Linux/macOS
./scripts/release.sh 1.0.0

# Windows (Command Prompt)
scripts\release.bat 1.0.0

# Windows (PowerShell)
.\scripts\release.ps1 1.0.0
```

**Option 2: Manual Git Tag**
```bash
git tag -a v1.0.0 -m "Release version 1.0.0"
git push origin v1.0.0
```

### Release Process
1. **Tag Creation**: Creates a version tag (e.g., `v1.0.0`)
2. **Automated Build**: GitHub Actions builds and tests the code
3. **NuGet Package**: Automatically creates and publishes NuGet package
4. **GitHub Release**: Creates a GitHub release with artifacts
5. **Multi-platform Binaries**: Builds demo app for all platforms

## ?? Troubleshooting

### Common Issues

1. **"No MeshCore devices found"**
   - Ensure the device is connected via USB
   - Check that the correct drivers are installed
   - Verify the device appears in Device Manager (Windows) or `lsusb` (Linux)

2. **"Failed to connect to device"**
   - Make sure no other applications are using the serial port
   - Try a different USB cable or port
   - Check device permissions on Linux/macOS

3. **"Operation timed out"**
   - The device might be busy or unresponsive
   - Try increasing the timeout value
   - Reset the device and try again

4. **"Bluetooth LE support not available"**
   - This is expected in the current version
   - USB connectivity is fully supported
   - Bluetooth LE support is coming in future releases

### Debug Logging

Enable detailed logging by handling the `ErrorOccurred` event:

```csharp
client.ErrorOccurred += (sender, error) =>
{
    Console.WriteLine($"Error: {error}");
};
```

## ??? Roadmap

### Version 1.0 (Current)
- ? Complete USB serial support
- ? Full protocol implementation
- ? Cross-platform compatibility
- ? Comprehensive documentation

### Version 2.0 (Planned)
- ?? **Bluetooth LE Support** - Full implementation
- ?? **Multiple Device Connections** - Connect to several devices simultaneously
- ?? **Enhanced Diagnostics** - Better logging and debugging tools
- ?? **Performance Optimizations** - Faster device discovery and data transfer

### Future Versions
- ?? **Mobile Support** - Xamarin/MAUI compatibility
- ?? **WiFi Transport** - TCP-based connectivity
- ??? **GUI Tools** - Visual device management applications
- ?? **Advanced Mesh Features** - Network topology visualization

## ?? Contributing

Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) for details on:

- Setting up the development environment
- Coding standards and guidelines
- Submitting pull requests
- Reporting issues

### Quick Start for Contributors
```bash
git clone https://github.com/WayneWalterBerry/MeshCore.Net.SDK.git
cd MeshCore.Net.SDK
dotnet restore
dotnet build
dotnet test
```

## ?? License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## ?? Support

For issues and questions:
- ?? [Report bugs](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/issues/new?template=bug_report.yml)
- ?? [Request features](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/issues/new?template=feature_request.yml)
- ?? [GitHub Discussions](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/discussions)
- ?? Check the [documentation](https://github.com/WayneWalterBerry/MeshCore.Net.SDK/wiki)

## ?? Related Projects

- [MeshCore Official Repository](https://github.com/meshcore-dev/MeshCore)
- [MeshCore Python SDK](https://pypi.org/project/meshcore/) - Official Python implementation with BLE support
- [meshcore-cli](https://github.com/meshcore-dev/meshcore-cli) - Command-line tool with BLE support
- [MeshCore Documentation](https://github.com/meshcore-dev/MeshCore/wiki)

## ?? Acknowledgments

- MeshCore development team for the excellent protocol documentation
- .NET community for the amazing tooling and libraries
- Contributors and users who help improve this SDK

---

**Note**: This SDK is based on the documented MeshCore USB protocol and provides the same functionality as the official Python library, but implemented natively in C# for .NET applications. Bluetooth LE support is coming soon to match the full feature set of the Python SDK!