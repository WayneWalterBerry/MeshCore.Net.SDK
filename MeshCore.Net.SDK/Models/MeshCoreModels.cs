namespace MeshCore.Net.SDK.Models;

/// <summary>
/// Represents a MeshCore device information
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// Gets or sets the unique device identifier
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the firmware version
    /// </summary>
    public string? FirmwareVersion { get; set; }

    /// <summary>
    /// Gets or sets the hardware version
    /// </summary>
    public string? HardwareVersion { get; set; }

    /// <summary>
    /// Gets or sets the device serial number
    /// </summary>
    public string? SerialNumber { get; set; }

    /// <summary>
    /// Gets or sets when the device was last seen on the network
    /// </summary>
    public DateTime? LastSeen { get; set; }

    /// <summary>
    /// Gets or sets the battery level percentage (0-100)
    /// </summary>
    public int BatteryLevel { get; set; }

    /// <summary>
    /// Gets or sets whether the device is currently connected
    /// </summary>
    public bool IsConnected { get; set; }
}

/// <summary>
/// Defines the possible status values for a contact
/// </summary>
public enum ContactStatus
{
    /// <summary>
    /// Contact status is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Contact is online and available
    /// </summary>
    Online = 1,

    /// <summary>
    /// Contact is offline
    /// </summary>
    Offline = 2,

    /// <summary>
    /// Contact is away
    /// </summary>
    Away = 3
}

/// <summary>
/// Defines the possible types of messages
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Text message
    /// </summary>
    Text = 0,

    /// <summary>
    /// Binary data message
    /// </summary>
    Binary = 1,

    /// <summary>
    /// Location information message
    /// </summary>
    Location = 2,

    /// <summary>
    /// Status update message
    /// </summary>
    Status = 3,

    /// <summary>
    /// Emergency message
    /// </summary>
    Emergency = 4
}

/// <summary>
/// Defines the possible status values for a message
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message is pending transmission
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Message has been sent
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Message has been delivered to recipient
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Message delivery failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Message has been acknowledged by recipient
    /// </summary>
    Acknowledged = 4
}

/// <summary>
/// Represents network status information
/// </summary>
public class NetworkStatus
{
    /// <summary>
    /// Gets or sets whether the device is connected to the network
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the name of the network
    /// </summary>
    public string? NetworkName { get; set; }

    /// <summary>
    /// Gets or sets the signal strength of the network connection
    /// </summary>
    public int SignalStrength { get; set; }

    /// <summary>
    /// Gets or sets the number of nodes connected to the network
    /// </summary>
    public int ConnectedNodes { get; set; }

    /// <summary>
    /// Gets or sets when the network status was last synchronized
    /// </summary>
    public DateTime? LastSync { get; set; }

    /// <summary>
    /// Gets or sets the network operation mode
    /// </summary>
    public NetworkMode Mode { get; set; }
}

/// <summary>
/// Defines the possible network operation modes
/// </summary>
public enum NetworkMode
{
    /// <summary>
    /// Device operates as a client node
    /// </summary>
    Client = 0,

    /// <summary>
    /// Device operates as a router node
    /// </summary>
    Router = 1,

    /// <summary>
    /// Device operates as a gateway node
    /// </summary>
    Gateway = 2
}

/// <summary>
/// Represents a channel message in the MeshCore network
/// </summary>
public class ChannelMessage
{
    /// <summary>
    /// Gets or sets the unique message identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel name this message belongs to
    /// </summary>
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the channel identifier
    /// </summary>
    public string ChannelId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact identifier of the sender
    /// </summary>
    public string FromContactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the message content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the message was sent
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the message type
    /// </summary>
    public MessageType Type { get; set; }

    /// <summary>
    /// Gets or sets the current status of the message
    /// </summary>
    public MessageStatus Status { get; set; }

    /// <summary>
    /// Gets or sets whether the message has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the signal strength when the message was received
    /// </summary>
    public int SignalStrength { get; set; }
}

/// <summary>
/// Represents channel statistics and information
/// </summary>
public class ChannelInfo
{
    /// <summary>
    /// Gets or sets the channel configuration
    /// </summary>
    public Channel Configuration { get; set; } = new();

    /// <summary>
    /// Gets or sets the number of messages sent on this channel
    /// </summary>
    public long MessagesSent { get; set; }

    /// <summary>
    /// Gets or sets the number of messages received on this channel
    /// </summary>
    public long MessagesReceived { get; set; }

    /// <summary>
    /// Gets or sets the last message timestamp
    /// </summary>
    public DateTime? LastMessageTime { get; set; }

    /// <summary>
    /// Gets or sets the average signal strength for this channel
    /// </summary>
    public double AverageSignalStrength { get; set; }

    /// <summary>
    /// Gets or sets whether this channel is currently active
    /// </summary>
    public bool IsActive { get; set; }
}