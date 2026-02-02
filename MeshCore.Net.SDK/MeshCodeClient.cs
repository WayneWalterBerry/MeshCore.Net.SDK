using System.Text;
using MeshCore.Net.SDK.Transport;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK;

/// <summary>
/// Main client for interacting with MeshCore devices
/// </summary>
public class MeshCodeClient : IDisposable
{
    private readonly UsbTransport _transport;
    private bool _disposed;

    public event EventHandler<Message>? MessageReceived;
    public event EventHandler<Contact>? ContactStatusChanged;
    public event EventHandler<NetworkStatus>? NetworkStatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _transport.IsConnected;
    public string? PortName => _transport.PortName;

    public MeshCodeClient(string portName)
    {
        _transport = new UsbTransport(portName);
        _transport.FrameReceived += OnFrameReceived;
        _transport.ErrorOccurred += OnTransportError;
    }

    /// <summary>
    /// Connects to the MeshCore device
    /// </summary>
    public async Task ConnectAsync()
    {
        await _transport.ConnectAsync();
        
        // Initialize device after connection
        await InitializeDeviceAsync();
    }

    /// <summary>
    /// Disconnects from the MeshCore device
    /// </summary>
    public void Disconnect()
    {
        _transport.Disconnect();
    }

    /// <summary>
    /// Discovers available MeshCore devices
    /// </summary>
    public static Task<List<string>> DiscoverDevicesAsync()
    {
        return UsbTransport.DiscoverDevicesAsync();
    }

    #region Device Operations

    /// <summary>
    /// Gets device information
    /// </summary>
    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_INFO);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_INFO, 
                (byte)response.GetStatus()!, "Failed to get device info");

        return ParseDeviceInfo(response.GetDataPayload());
    }

    /// <summary>
    /// Sets the device time
    /// </summary>
    public async Task SetDeviceTimeAsync(DateTime dateTime)
    {
        var timestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        var data = BitConverter.GetBytes(timestamp);
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_DEVICE_TIME, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, 
                (byte)response.GetStatus()!, "Failed to set device time");
    }

    /// <summary>
    /// Gets the device time
    /// </summary>
    public async Task<DateTime> GetDeviceTimeAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, 
                (byte)response.GetStatus()!, "Failed to get device time");

        var data = response.GetDataPayload();
        if (data.Length >= 8)
        {
            var timestamp = BitConverter.ToInt64(data, 0);
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
        
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the device
    /// </summary>
    public async Task ResetDeviceAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_RESET_DEVICE);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_RESET_DEVICE, 
                (byte)response.GetStatus()!, "Failed to reset device");
    }

    #endregion

    #region Contact Operations

    /// <summary>
    /// Gets all contacts
    /// </summary>
    public async Task<List<Contact>> GetContactsAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_CONTACTS);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONTACTS, 
                (byte)response.GetStatus()!, "Failed to get contacts");

        return ParseContacts(response.GetDataPayload());
    }

    /// <summary>
    /// Adds a new contact
    /// </summary>
    public async Task<Contact> AddContactAsync(string name, string nodeId)
    {
        var data = Encoding.UTF8.GetBytes($"{name}\0{nodeId}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_ADD_CONTACT, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_ADD_CONTACT, 
                (byte)response.GetStatus()!, "Failed to add contact");

        return ParseContact(response.GetDataPayload());
    }

    /// <summary>
    /// Deletes a contact
    /// </summary>
    public async Task DeleteContactAsync(string contactId)
    {
        var data = Encoding.UTF8.GetBytes(contactId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DELETE_CONTACT, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_DELETE_CONTACT, 
                (byte)response.GetStatus()!, "Failed to delete contact");
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Sends a text message
    /// </summary>
    public async Task<Message> SendMessageAsync(string toContactId, string content)
    {
        var messageData = Encoding.UTF8.GetBytes($"{toContactId}\0{content}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SEND_MESSAGE, messageData);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_MESSAGE, 
                (byte)response.GetStatus()!, "Failed to send message");

        return ParseMessage(response.GetDataPayload());
    }

    /// <summary>
    /// Gets all messages
    /// </summary>
    public async Task<List<Message>> GetMessagesAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_MESSAGES);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_MESSAGES, 
                (byte)response.GetStatus()!, "Failed to get messages");

        return ParseMessages(response.GetDataPayload());
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageReadAsync(string messageId)
    {
        var data = Encoding.UTF8.GetBytes(messageId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_MARK_MESSAGE_READ, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_MARK_MESSAGE_READ, 
                (byte)response.GetStatus()!, "Failed to mark message as read");
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    public async Task DeleteMessageAsync(string messageId)
    {
        var data = Encoding.UTF8.GetBytes(messageId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DELETE_MESSAGE, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_DELETE_MESSAGE, 
                (byte)response.GetStatus()!, "Failed to delete message");
    }

    #endregion

    #region Network Operations

    /// <summary>
    /// Gets current network status
    /// </summary>
    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_NETWORK_STATUS);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_NETWORK_STATUS, 
                (byte)response.GetStatus()!, "Failed to get network status");

        return ParseNetworkStatus(response.GetDataPayload());
    }

    /// <summary>
    /// Scans for available networks
    /// </summary>
    public async Task<List<string>> ScanNetworksAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SCAN_NETWORKS);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SCAN_NETWORKS, 
                (byte)response.GetStatus()!, "Failed to scan networks");

        return ParseNetworkList(response.GetDataPayload());
    }

    #endregion

    #region Configuration Operations

    /// <summary>
    /// Gets device configuration
    /// </summary>
    public async Task<DeviceConfiguration> GetConfigurationAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_CONFIG);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONFIG, 
                (byte)response.GetStatus()!, "Failed to get configuration");

        return ParseConfiguration(response.GetDataPayload());
    }

    /// <summary>
    /// Sets device configuration
    /// </summary>
    public async Task SetConfigurationAsync(DeviceConfiguration config)
    {
        var data = SerializeConfiguration(config);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_CONFIG, data);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SET_CONFIG, 
                (byte)response.GetStatus()!, "Failed to set configuration");
    }

    #endregion

    #region Private Methods

    private async Task InitializeDeviceAsync()
    {
        // Perform initial device query
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY);
        
        if (response.GetStatus() != MeshCoreStatus.Success)
            throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                (byte)response.GetStatus()!, "Device initialization failed");
    }

    private void OnFrameReceived(object? sender, MeshCoreFrame frame)
    {
        try
        {
            // Handle unsolicited frames (events)
            var command = frame.GetCommand();
            switch (command)
            {
                case MeshCoreCommand.CMD_SEND_MESSAGE:
                    // Incoming message notification
                    var message = ParseMessage(frame.GetDataPayload());
                    MessageReceived?.Invoke(this, message);
                    break;
                    
                case MeshCoreCommand.CMD_GET_NETWORK_STATUS:
                    // Network status update
                    var status = ParseNetworkStatus(frame.GetDataPayload());
                    NetworkStatusChanged?.Invoke(this, status);
                    break;
                    
                case MeshCoreCommand.CMD_GET_CONTACTS:
                    // Contact status update
                    var contact = ParseContact(frame.GetDataPayload());
                    ContactStatusChanged?.Invoke(this, contact);
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    private void OnTransportError(object? sender, Exception ex)
    {
        ErrorOccurred?.Invoke(this, ex);
    }

    #endregion

    #region Parsing Methods

    private static DeviceInfo ParseDeviceInfo(byte[] data)
    {
        // Simple parsing example - in real implementation, this would follow the actual protocol
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new DeviceInfo
        {
            DeviceId = parts.Length > 0 ? parts[0] : null,
            FirmwareVersion = parts.Length > 1 ? parts[1] : null,
            HardwareVersion = parts.Length > 2 ? parts[2] : null,
            SerialNumber = parts.Length > 3 ? parts[3] : null,
            IsConnected = true,
            LastSeen = DateTime.UtcNow
        };
    }

    private static List<Contact> ParseContacts(byte[] data)
    {
        var contacts = new List<Contact>();
        var text = Encoding.UTF8.GetString(data);
        var contactEntries = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var entry in contactEntries)
        {
            var parts = entry.Split('\0');
            if (parts.Length >= 3)
            {
                contacts.Add(new Contact
                {
                    Id = parts[0],
                    Name = parts[1],
                    NodeId = parts[2],
                    LastSeen = DateTime.UtcNow,
                    IsOnline = parts.Length > 3 && parts[3] == "1",
                    Status = ContactStatus.Online
                });
            }
        }
        
        return contacts;
    }

    private static Contact ParseContact(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new Contact
        {
            Id = parts.Length > 0 ? parts[0] : string.Empty,
            Name = parts.Length > 1 ? parts[1] : string.Empty,
            NodeId = parts.Length > 2 ? parts[2] : null,
            LastSeen = DateTime.UtcNow,
            IsOnline = parts.Length > 3 && parts[3] == "1",
            Status = ContactStatus.Online
        };
    }

    private static List<Message> ParseMessages(byte[] data)
    {
        var messages = new List<Message>();
        var text = Encoding.UTF8.GetString(data);
        var messageEntries = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var entry in messageEntries)
        {
            var parts = entry.Split('\0');
            if (parts.Length >= 4)
            {
                messages.Add(new Message
                {
                    Id = parts[0],
                    FromContactId = parts[1],
                    ToContactId = parts[2],
                    Content = parts[3],
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Text,
                    Status = MessageStatus.Delivered,
                    IsRead = parts.Length > 4 && parts[4] == "1"
                });
            }
        }
        
        return messages;
    }

    private static Message ParseMessage(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new Message
        {
            Id = parts.Length > 0 ? parts[0] : Guid.NewGuid().ToString(),
            FromContactId = parts.Length > 1 ? parts[1] : string.Empty,
            ToContactId = parts.Length > 2 ? parts[2] : string.Empty,
            Content = parts.Length > 3 ? parts[3] : string.Empty,
            Timestamp = DateTime.UtcNow,
            Type = MessageType.Text,
            Status = MessageStatus.Delivered,
            IsRead = false
        };
    }

    private static NetworkStatus ParseNetworkStatus(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new NetworkStatus
        {
            IsConnected = parts.Length > 0 && parts[0] == "1",
            NetworkName = parts.Length > 1 ? parts[1] : null,
            SignalStrength = parts.Length > 2 && int.TryParse(parts[2], out var signal) ? signal : 0,
            ConnectedNodes = parts.Length > 3 && int.TryParse(parts[3], out var nodes) ? nodes : 0,
            LastSync = DateTime.UtcNow,
            Mode = NetworkMode.Client
        };
    }

    private static List<string> ParseNetworkList(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static DeviceConfiguration ParseConfiguration(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new DeviceConfiguration
        {
            DeviceName = parts.Length > 0 ? parts[0] : null,
            TransmitPower = parts.Length > 1 && int.TryParse(parts[1], out var power) ? power : 100,
            Channel = parts.Length > 2 && int.TryParse(parts[2], out var channel) ? channel : 1,
            AutoRelay = parts.Length > 3 && parts[3] == "1",
            HeartbeatInterval = TimeSpan.FromSeconds(30),
            MessageTimeout = TimeSpan.FromMinutes(5)
        };
    }

    private static byte[] SerializeConfiguration(DeviceConfiguration config)
    {
        var configString = $"{config.DeviceName}\0{config.TransmitPower}\0{config.Channel}\0{(config.AutoRelay ? "1" : "0")}";
        return Encoding.UTF8.GetBytes(configString);
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _transport?.Dispose();
            _disposed = true;
        }
    }
}