namespace MeshCore.Net.SDK.Models;

/// <summary>
/// Represents a MeshCore device information
/// </summary>
public class DeviceInfo
{
    public string? DeviceId { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? HardwareVersion { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? LastSeen { get; set; }
    public int BatteryLevel { get; set; }
    public bool IsConnected { get; set; }
}

/// <summary>
/// Represents a contact in the MeshCore network
/// </summary>
public class Contact
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NodeId { get; set; }
    public DateTime? LastSeen { get; set; }
    public int SignalStrength { get; set; }
    public bool IsOnline { get; set; }
    public ContactStatus Status { get; set; }
}

/// <summary>
/// Contact status enumeration
/// </summary>
public enum ContactStatus
{
    Unknown = 0,
    Online = 1,
    Offline = 2,
    Away = 3
}

/// <summary>
/// Represents a message in the MeshCore network
/// </summary>
public class Message
{
    public string Id { get; set; } = string.Empty;
    public string FromContactId { get; set; } = string.Empty;
    public string ToContactId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public MessageType Type { get; set; }
    public MessageStatus Status { get; set; }
    public bool IsRead { get; set; }
    public int? DeliveryAttempts { get; set; }
}

/// <summary>
/// Message type enumeration
/// </summary>
public enum MessageType
{
    Text = 0,
    Binary = 1,
    Location = 2,
    Status = 3,
    Emergency = 4
}

/// <summary>
/// Message status enumeration
/// </summary>
public enum MessageStatus
{
    Pending = 0,
    Sent = 1,
    Delivered = 2,
    Failed = 3,
    Acknowledged = 4
}

/// <summary>
/// Represents network status information
/// </summary>
public class NetworkStatus
{
    public bool IsConnected { get; set; }
    public string? NetworkName { get; set; }
    public int SignalStrength { get; set; }
    public int ConnectedNodes { get; set; }
    public DateTime? LastSync { get; set; }
    public NetworkMode Mode { get; set; }
}

/// <summary>
/// Network mode enumeration
/// </summary>
public enum NetworkMode
{
    Client = 0,
    Router = 1,
    Gateway = 2
}

/// <summary>
/// Represents device configuration
/// </summary>
public class DeviceConfiguration
{
    public string? DeviceName { get; set; }
    public int TransmitPower { get; set; }
    public int Channel { get; set; }
    public bool AutoRelay { get; set; }
    public TimeSpan HeartbeatInterval { get; set; }
    public TimeSpan MessageTimeout { get; set; }
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}