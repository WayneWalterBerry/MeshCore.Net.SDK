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

## ?? Project Structure

```
MeshCore.Net.SDK/
??? MeshCore.Net.SDK/           # Main SDK library
?   ??? Protocol/               # Protocol implementation
?   ??? Models/                 # Data models
?   ??? Exceptions/             # Custom exceptions
?   ??? Transport/              # USB communication
?   ??? Examples/               # Usage examples
?   ??? MeshCodeClient.cs       # Main client
??? MeshCore.Net.SDK.Tests/     # Unit tests
??? MeshCore.Net.SDK.Demo/      # Console demo application
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

Run the included demo application:

```bash
# Basic demo
dotnet run --project MeshCore.Net.SDK.Demo

# Advanced demo with more features
dotnet run --project MeshCore.Net.SDK.Demo -- --advanced
```

## ?? Quick Start

### Basic Device Connection

```csharp
using MeshCore.Net.SDK;

// Discover available devices
var devices = await MeshCodeClient.DiscoverDevicesAsync();
Console.WriteLine($"Found {devices.Count} MeshCore devices");

// Connect to the first device
using var client = new MeshCodeClient(devices[0]);
await client.ConnectAsync();

// Get device information
var deviceInfo = await client.GetDeviceInfoAsync();
Console.WriteLine($"Connected to: {deviceInfo.DeviceId}");
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