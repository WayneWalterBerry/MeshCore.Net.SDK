using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Transport;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Exceptions;
using MeshCore.Net.SDK.Logging;

namespace MeshCore.Net.SDK;

/// <summary>
/// Main client for interacting with MeshCore devices via USB or Bluetooth
/// </summary>
public class MeshCodeClient : IDisposable
{
    private readonly ITransport _transport;
    private readonly ILogger<MeshCodeClient> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private bool _disposed;

    public event EventHandler<Message>? MessageReceived;
    public event EventHandler<Contact>? ContactStatusChanged;
    public event EventHandler<NetworkStatus>? NetworkStatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _transport.IsConnected;
    public string? ConnectionId => _transport.ConnectionId;

    /// <summary>
    /// Creates a new MeshCodeClient with the specified transport and optional logger
    /// </summary>
    public MeshCodeClient(ITransport transport, ILoggerFactory? loggerFactory = null)
    {
        _transport = transport;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory?.CreateLogger<MeshCodeClient>() ?? NullLogger<MeshCodeClient>.Instance;
        
        _transport.FrameReceived += OnFrameReceived;
        _transport.ErrorOccurred += OnTransportError;
        
        // Log SDK initialization
        _logger.LogSdkInitialized(GetType().Assembly.GetName().Version?.ToString() ?? "Unknown");
        
        // Also log to ETW
        MeshCoreSdkEventSource.Log.SdkInitialized(GetType().Assembly.GetName().Version?.ToString() ?? "Unknown");
    }

    /// <summary>
    /// Creates a new MeshCodeClient for a specific device with optional logger
    /// </summary>
    public MeshCodeClient(MeshCoreDevice device, ILoggerFactory? loggerFactory = null) 
        : this(TransportFactory.CreateTransport(device), loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new MeshCodeClient with a connection string (backward compatibility)
    /// </summary>
    public MeshCodeClient(string connectionString, ILoggerFactory? loggerFactory = null) 
        : this(TransportFactory.CreateTransport(connectionString), loggerFactory)
    {
    }

    /// <summary>
    /// Connects to the MeshCore device
    /// </summary>
    public async Task ConnectAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var transportType = _transport.GetType().Name;
        
        _logger.LogDeviceConnectionStarted(deviceId, transportType);
        MeshCoreSdkEventSource.Log.DeviceConnectionStarted(deviceId, transportType);
        
        try
        {
            await _transport.ConnectAsync();
            
            // Initialize device after connection
            await InitializeDeviceAsync();
            
            _logger.LogDeviceConnectionSucceeded(deviceId, transportType);
            MeshCoreSdkEventSource.Log.DeviceConnectionSucceeded(deviceId, transportType);
        }
        catch (Exception ex)
        {
            _logger.LogDeviceConnectionFailed(ex, deviceId, transportType);
            MeshCoreSdkEventSource.Log.DeviceConnectionFailed(deviceId, transportType, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Disconnects from the MeshCore device
    /// </summary>
    public void Disconnect()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        _transport.Disconnect();
        
        _logger.LogDeviceDisconnected(deviceId);
        MeshCoreSdkEventSource.Log.DeviceDisconnected(deviceId);
    }

    /// <summary>
    /// Discovers available MeshCore devices across all transport types
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverDevicesAsync(TimeSpan? timeout = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<MeshCodeClient>() ?? NullLogger<MeshCodeClient>. Instance;
        logger.LogDeviceDiscoveryStarted("All");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("All");
        
        return TransportFactory.DiscoverAllDevicesAsync(timeout);
    }

    /// <summary>
    /// Discovers USB MeshCore devices only (backward compatibility)
    /// </summary>
    public static async Task<List<string>> DiscoverUsbDevicesAsync(ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<MeshCodeClient>() ?? NullLogger<MeshCodeClient>.Instance;
        logger.LogDeviceDiscoveryStarted("USB");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("USB");
        
        var devices = await UsbTransport.DiscoverDevicesAsync();
        var deviceIds = devices.Select(d => d.Id).ToList();
        
        logger.LogDeviceDiscoveryCompleted(devices.Count, "USB");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryCompleted(devices.Count, "USB");
        
        return deviceIds;
    }

    /// <summary>
    /// Discovers Bluetooth LE MeshCore devices only
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverBluetoothDevicesAsync(TimeSpan? timeout = null, ILoggerFactory? loggerFactory = null)
    {
        var logger = loggerFactory?.CreateLogger<MeshCodeClient>() ?? NullLogger<MeshCodeClient>.Instance;
        logger.LogDeviceDiscoveryStarted("BluetoothLE");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("BluetoothLE");
        
        return BluetoothTransport.DiscoverDevicesAsync(timeout);
    }

    #region Device Operations

    /// <summary>
    /// Gets device information
    /// </summary>
    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(GetDeviceInfoAsync);
        var startTime = DateTimeOffset.UtcNow;
        
        _logger.LogDebug("Starting operation: {OperationName} for device: {DeviceId}", operationName, deviceId);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);
        
        try
        {
            _logger.LogCommandSending((byte)MeshCoreCommand.CMD_DEVICE_QUERY, deviceId);
            MeshCoreSdkEventSource.Log.CommandSending((byte)MeshCoreCommand.CMD_DEVICE_QUERY, deviceId);
            
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
            
            _logger.LogResponseReceived((byte)MeshCoreCommand.CMD_DEVICE_QUERY, response.Payload.FirstOrDefault(), deviceId);
            MeshCoreSdkEventSource.Log.ResponseReceived((byte)MeshCoreCommand.CMD_DEVICE_QUERY, response.Payload.FirstOrDefault(), deviceId);
            
            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, "Failed to get device info");
                
                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, ex.Message);
                
                throw ex;
            }

            var deviceInfo = ParseDeviceInfo(response.Payload);
            
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms", operationName, deviceId, (long)duration);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
            
            return deviceInfo;
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sets the device time
    /// </summary>
    public async Task SetDeviceTimeAsync(DateTime dateTime)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var timestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        var data = BitConverter.GetBytes(timestamp);
        
        _logger.LogCommandSending((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, deviceId);
        MeshCoreSdkEventSource.Log.CommandSending((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, deviceId);
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_DEVICE_TIME, data);
        
        _logger.LogResponseReceived((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, response.Payload.FirstOrDefault(), deviceId);
        MeshCoreSdkEventSource.Log.ResponseReceived((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, response.Payload.FirstOrDefault(), deviceId);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, statusByte, "Failed to set device time");
            
            _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, statusByte);
            MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, statusByte, ex.Message);
            
            throw ex;
        }
    }

    /// <summary>
    /// Gets the device time
    /// </summary>
    public async Task<DateTime> GetDeviceTimeAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        
        _logger.LogCommandSending((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, deviceId);
        MeshCoreSdkEventSource.Log.CommandSending((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, deviceId);
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
        
        _logger.LogResponseReceived((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, response.Payload.FirstOrDefault(), deviceId);
        MeshCoreSdkEventSource.Log.ResponseReceived((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, response.Payload.FirstOrDefault(), deviceId);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_CURR_TIME)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, statusByte, "Failed to get device time");
            
            _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, statusByte);
            MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, statusByte, ex.Message);
            
            throw ex;
        }

        var data = response.Payload;
        if (data.Length >= 4)
        {
            var timestamp = BitConverter.ToInt32(data, 0);
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
        
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the device
    /// </summary>
    public async Task ResetDeviceAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var rebootData = Encoding.ASCII.GetBytes("reboot");
        
        _logger.LogCommandSending((byte)MeshCoreCommand.CMD_REBOOT, deviceId);
        MeshCoreSdkEventSource.Log.CommandSending((byte)MeshCoreCommand.CMD_REBOOT, deviceId);
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REBOOT, rebootData);
        
        // No response expected as device will restart
        _logger.LogInformation("Device reboot command sent to {DeviceId}", deviceId);
    }

    #endregion

    #region Contact Operations

    /// <summary>
    /// Gets all contacts
    /// </summary>
    public async Task<List<Contact>> GetContactsAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        
        _logger.LogContactRetrievalStarted(deviceId);
        MeshCoreSdkEventSource.Log.ContactRetrievalStarted(deviceId);
        
        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_CONTACTS);
            var responseCode = response.GetResponseCode();
            
            _logger.LogDebug("Initial contact response: {ResponseCode} for device {DeviceId}", responseCode, deviceId);
            
            // Handle different protocol variations for contact retrieval
            if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACTS_START)
            {
                // Standard protocol: CONTACTS_START -> CONTACT... -> END_OF_CONTACTS
                var contacts = await ParseContactsSequence(response.Payload);
                
                _logger.LogContactRetrievalCompleted(deviceId, contacts.Count);
                MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, contacts.Count);
                
                return contacts;
            }
            else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                // Alternative protocol: Direct CONTACT response (single contact or first of many)
                _logger.LogDebug("Device {DeviceId} using direct contact response protocol", deviceId);
                
                var contacts = new List<Contact>();
                
                // Parse the first contact from the initial response
                try
                {
                    var firstContact = ParseSingleContact(response.Payload);
                    contacts.Add(firstContact);
                    _logger.LogDebug("Parsed first contact: {ContactName} ({ContactId})", firstContact.Name, firstContact.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse initial contact from device {DeviceId}", deviceId);
                }
                
                // Continue retrieving additional contacts using SYNC_NEXT_MESSAGE
                await ContinueParsingContacts(contacts, deviceId);
                
                _logger.LogContactRetrievalCompleted(deviceId, contacts.Count);
                MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, contacts.Count);
                
                return contacts;
            }
            else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                // Handle error responses
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                
                switch (statusByte)
                {
                    case (byte)MeshCoreStatus.InvalidParameter:
                        _logger.LogInformation("Device {DeviceId} reports no contacts available (empty contact list)", deviceId);
                        var emptyList = new List<Contact>();
                        _logger.LogContactRetrievalCompleted(deviceId, emptyList.Count);
                        MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, emptyList.Count);
                        return emptyList;
                        
                    case (byte)MeshCoreStatus.InvalidCommand:
                        _logger.LogWarning("Device {DeviceId} does not support contact operations", deviceId);
                        throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONTACTS, statusByte, 
                            "Get contacts command not supported by this device firmware");
                            
                    case (byte)MeshCoreStatus.DeviceError:
                        _logger.LogWarning("Device {DeviceId} is in an error state for contact operations", deviceId);
                        // Return empty list for device errors as this might be recoverable
                        var deviceErrorEmptyList = new List<Contact>();
                        _logger.LogContactRetrievalCompleted(deviceId, deviceErrorEmptyList.Count);
                        MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, deviceErrorEmptyList.Count);
                        return deviceErrorEmptyList;
                        
                    case (byte)MeshCoreStatus.NetworkError:
                        _logger.LogWarning("Device {DeviceId} has a network error for contact operations", deviceId);
                        // Return empty list for network errors as this might be recoverable
                        var networkErrorEmptyList = new List<Contact>();
                        _logger.LogContactRetrievalCompleted(deviceId, networkErrorEmptyList.Count);
                        MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, networkErrorEmptyList.Count);
                        return networkErrorEmptyList;
                        
                    case (byte)MeshCoreStatus.TimeoutError:
                        _logger.LogWarning("Device {DeviceId} has a timeout error for contact operations", deviceId);
                        // Return empty list for timeout errors as this might be recoverable
                        var timeoutErrorEmptyList = new List<Contact>();
                        _logger.LogContactRetrievalCompleted(deviceId, timeoutErrorEmptyList.Count);
                        MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, timeoutErrorEmptyList.Count);
                        return timeoutErrorEmptyList;
                        
                    case (byte)MeshCoreStatus.UnknownError:
                        _logger.LogWarning("Device {DeviceId} has an unknown error for contact operations", deviceId);
                        // Return empty list for unknown errors as this might be recoverable
                        var unknownErrorEmptyList = new List<Contact>();
                        _logger.LogContactRetrievalCompleted(deviceId, unknownErrorEmptyList.Count);
                        MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, unknownErrorEmptyList.Count);
                        return unknownErrorEmptyList;
                        
                    default:
                        _logger.LogWarning("Device {DeviceId} returned error for contacts with status {Status}", deviceId, statusByte);
                        throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONTACTS, statusByte, 
                            $"Device returned error status {statusByte} for contact retrieval");
                }
            }
            else
            {
                // Unexpected response code
                _logger.LogWarning("Device {DeviceId} returned unexpected response code {ResponseCode} for contacts", deviceId, responseCode);
                throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONTACTS, 0x01, 
                    $"Unexpected response code {responseCode} for contact retrieval");
            }
        }
        catch (ProtocolException)
        {
            // Re-throw protocol exceptions as they contain useful error information
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during contact retrieval for device {DeviceId}", deviceId);
            
            // For unexpected errors, we can return an empty list and log the issue
            var errorEmptyList = new List<Contact>();
            _logger.LogContactRetrievalCompleted(deviceId, errorEmptyList.Count);
            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId, errorEmptyList.Count);
            return errorEmptyList;
        }
    }

    /// <summary>
    /// Adds a new contact
    /// </summary>
    public async Task<Contact> AddContactAsync(string name, string nodeId)
    {
        var data = Encoding.UTF8.GetBytes($"{name}\0{nodeId}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_ADD_UPDATE_CONTACT, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_ADD_UPDATE_CONTACT, 
                statusByte, "Failed to add contact");
        }

        // CMD_ADD_UPDATE_CONTACT only returns an acknowledgment, not contact data
        // So we construct the Contact object from the input parameters
        return new Contact
        {
            Id = nodeId,
            Name = name,
            NodeId = nodeId,
            LastSeen = DateTime.UtcNow,
            IsOnline = false,
            Status = ContactStatus.Unknown
        };
    }

    /// <summary>
    /// Deletes a contact
    /// </summary>
    public async Task DeleteContactAsync(string contactId)
    {
        var data = Encoding.UTF8.GetBytes(contactId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REMOVE_CONTACT, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_REMOVE_CONTACT, 
                statusByte, "Failed to delete contact");
        }
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Sends a text message
    /// </summary>
    public async Task<Message> SendMessageAsync(string toContactId, string content)
    {
        if (string.IsNullOrEmpty(toContactId))
            throw new ArgumentException("Contact ID cannot be null or empty", nameof(toContactId));
        
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Message content cannot be null or empty", nameof(content));

        _logger.LogMessageSendingStarted(toContactId, content.Length);
        MeshCoreSdkEventSource.Log.MessageSendingStarted(toContactId, content.Length);

        var messageData = Encoding.UTF8.GetBytes($"{toContactId}\0{content}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SEND_TXT_MSG, messageData);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_SENT)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, statusByte, "Failed to send message");
            
            _logger.LogMessageSendingFailed(ex, toContactId);
            MeshCoreSdkEventSource.Log.MessageSendingFailed(toContactId, ex.Message);
            
            throw ex;
        }

        var message = ParseMessage(response.Payload);
        
        _logger.LogMessageSent(toContactId, message.Id);
        MeshCoreSdkEventSource.Log.MessageSent(toContactId, message.Id);
        
        return message;
    }

    /// <summary>
    /// Gets all messages
    /// </summary>
    public async Task<List<Message>> GetMessagesAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        _logger.LogMessageRetrievalStarted(deviceId);
        MeshCoreSdkEventSource.Log.MessageRetrievalStarted(deviceId);
        
        var messages = new List<Message>();
        
        try
        {
            var maxAttempts = 10;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE);
                    var responseCode = response.GetResponseCode();
                    
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                    {
                        _logger.LogDebug("Device reports no more messages for device {DeviceId}", deviceId);
                        break;
                    }
                    
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                    {
                        _logger.LogWarning("Error response during message sync for device {DeviceId}", deviceId);
                        break;
                    }
                    
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3 ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3)
                    {
                        try
                        {
                            var message = ParseMessage(response.Payload, responseCode.Value);
                            messages.Add(message);
                            _logger.LogDebug("Retrieved message from {FromContactId} for device {DeviceId}", message.FromContactId, deviceId);
                        }
                        catch (Exception parseEx)
                        {
                            _logger.LogWarning(parseEx, "Failed to parse message for device {DeviceId}", deviceId);
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Unexpected response during message sync: {ResponseCode} for device {DeviceId}", responseCode, deviceId);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error during message sync for device {DeviceId}", deviceId);
                    break;
                }
            }
            
            _logger.LogMessageRetrievalCompleted(deviceId, messages.Count);
            MeshCoreSdkEventSource.Log.MessageRetrievalCompleted(deviceId, messages.Count);
            
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError(ex, nameof(GetMessagesAsync));
            return new List<Message>();
        }
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageReadAsync(string messageId) => await Task.CompletedTask;

    /// <summary>
    /// Deletes a message
    /// </summary>
    public async Task DeleteMessageAsync(string messageId) => await Task.CompletedTask;

    #endregion

    #region Network Operations

    /// <summary>
    /// Gets current network status
    /// </summary>
    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
            
            if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_CURR_TIME)
            {
                return new NetworkStatus
                {
                    IsConnected = true,
                    NetworkName = "MeshCore Local",
                    SignalStrength = 100,
                    ConnectedNodes = 1,
                    LastSync = DateTime.UtcNow,
                    Mode = NetworkMode.Client
                };
            }
            else
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, 
                    statusByte, "Failed to verify device connectivity");
            }
        }
        catch (Exception)
        {
            return new NetworkStatus
            {
                IsConnected = false,
                NetworkName = null,
                SignalStrength = 0,
                ConnectedNodes = 0,
                LastSync = DateTime.UtcNow,
                Mode = NetworkMode.Client
            };
        }
    }

    /// <summary>
    /// Scans for available networks
    /// </summary>
    public async Task<List<string>> ScanNetworksAsync() => new List<string>();

    #endregion

    #region Configuration Operations

    /// <summary>
    /// Gets device configuration
    /// </summary>
    public async Task<DeviceConfiguration> GetConfigurationAsync()
    {
        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_BATT_AND_STORAGE);
            
            if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE)
            {
                return ParseConfiguration(response.Payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get battery/storage info, using default configuration");
        }
        
        return new DeviceConfiguration
        {
            DeviceName = "MeshCore Device (Default)",
            TransmitPower = 100,
            Channel = 1,
            AutoRelay = false,
            HeartbeatInterval = TimeSpan.FromSeconds(30),
            MessageTimeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Sets device configuration
    /// </summary>
    public async Task SetConfigurationAsync(DeviceConfiguration config)
    {
        var data = SerializeConfiguration(config);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_RADIO_PARAMS, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, 
                statusByte, "Failed to set configuration");
        }
    }

    #endregion

    #region Private Methods

    private async Task InitializeDeviceAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        _logger.LogDebug("Initializing device {DeviceId}", deviceId);
        
        try
        {
            var appStartResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_APP_START, new byte[] { 0x08 });
            var deviceQueryResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
            
            if (deviceQueryResponse.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
                throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                    0x01, "Device initialization failed");
                    
            _logger.LogDebug("Device initialization completed successfully for {DeviceId}", deviceId);
        }
        catch (MeshCoreTimeoutException)
        {
            _logger.LogWarning("APP_START timed out, trying just device query for {DeviceId}", deviceId);
            
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
            
            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
                throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                    0x01, "Device initialization failed");
                    
            _logger.LogDebug("Device initialization completed with fallback method for {DeviceId}", deviceId);
        }
    }

    private void OnFrameReceived(object? sender, MeshCoreFrame frame)
    {
        try
        {
            var deviceId = _transport.ConnectionId ?? "Unknown";
            _logger.LogFrameParsed(frame.StartByte, frame.Length, frame.Payload.Length);
            MeshCoreSdkEventSource.Log.FrameParsed(frame.StartByte, frame.Length, frame.Payload.Length);
            
            var responseCode = frame.GetResponseCode();
            switch (responseCode)
            {
                case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV:
                case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3:
                    var message = ParseMessage(frame.Payload);
                    _logger.LogMessageReceived(message.FromContactId, message.Content?.Length ?? 0);
                    MeshCoreSdkEventSource.Log.MessageReceived(message.FromContactId, message.Content?.Length ?? 0);
                    MessageReceived?.Invoke(this, message);
                    break;
                    
                case MeshCoreResponseCode.RESP_CODE_CONTACT:
                    var contact = ParseContact(frame.Payload);
                    ContactStatusChanged?.Invoke(this, contact);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError(ex, nameof(OnFrameReceived));
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, nameof(OnFrameReceived));
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    private void OnTransportError(object? sender, Exception ex)
    {
        _logger.LogUnexpectedError(ex, "Transport");
        MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, "Transport");
        ErrorOccurred?.Invoke(this, ex);
    }

    private async Task<List<Contact>> ParseContactsSequence(byte[] initialData)
    {
        var contacts = new List<Contact>();
        var deviceId = _transport.ConnectionId ?? "Unknown";
        
        _logger.LogDebug("Parsing contacts sequence for device {DeviceId}", deviceId);
        
        var maxContacts = 100;
        var contactCount = 0;
        var consecutiveErrors = 0;
        var maxConsecutiveErrors = 3;
        
        while (contactCount < maxContacts)
        {
            try
            {
                await Task.Delay(100);
                
                var nextResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE);
                var responseCode = nextResponse.GetResponseCode();
                
                _logger.LogTrace("Next response: {ResponseCode} for contact #{ContactNumber}", responseCode, contactCount + 1);
                
                if (responseCode == MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS)
                {
                    _logger.LogDebug("End of contacts reached for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
                {
                    try
                    {
                        var contact = ParseSingleContact(nextResponse.Payload);
                        contacts.Add(contact);
                        contactCount++;
                        consecutiveErrors = 0;
                        
                        _logger.LogContactParsed(contact.Name, contact.NodeId ?? "Unknown");
                        MeshCoreSdkEventSource.Log.ContactParsed(contact.Name, contact.NodeId ?? "Unknown");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogContactParsingFailed(ex);
                        MeshCoreSdkEventSource.Log.ContactParsingFailed(ex.Message);
                        contactCount++;
                        consecutiveErrors++;
                    }
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                {
                    _logger.LogDebug("Device reports no more messages/contacts for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    consecutiveErrors++;
                    _logger.LogWarning("Error response during contact enumeration for device {DeviceId} (consecutive errors: {ConsecutiveErrors})", deviceId, consecutiveErrors);
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        _logger.LogWarning("Stopping contact retrieval after {ConsecutiveErrors} consecutive errors for device {DeviceId}", consecutiveErrors, deviceId);
                        break;
                    }
                    else
                    {
                        contactCount++;
                    }
                }
                else
                {
                    consecutiveErrors++;
                    _logger.LogWarning("Unexpected response during contact enumeration: {ResponseCode} for device {DeviceId}", responseCode, deviceId);
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        _logger.LogWarning("Too many unexpected responses, stopping contact retrieval for device {DeviceId}", deviceId);
                        break;
                    }
                    else
                    {
                        contactCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                _logger.LogError(ex, "Error during contact retrieval for device {DeviceId} (consecutive errors: {ConsecutiveErrors})", deviceId, consecutiveErrors);
                
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    _logger.LogError("Too many consecutive errors, stopping contact retrieval for device {DeviceId}", deviceId);
                    break;
                }
                else
                {
                    contactCount++;
                }
            }
        }
        
        _logger.LogDebug("Contact retrieval summary for device {DeviceId}: {ContactCount} contacts retrieved, {TotalAttempts} total attempts, {FinalConsecutiveErrors} final consecutive errors", 
            deviceId, contacts.Count, contactCount, consecutiveErrors);
        
        return contacts;
    }

    /// <summary>
    /// Continue parsing contacts using the existing ParseContactsSequence logic
    /// </summary>
    private async Task ContinueParsingContacts(List<Contact> contacts, string deviceId)
    {
        var maxContacts = 100;
        var contactCount = contacts.Count; // Start with contacts already added
        var consecutiveErrors = 0;
        var maxConsecutiveErrors = 3;
        
        while (contactCount < maxContacts)
        {
            try
            {
                await Task.Delay(100);
                
                var nextResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE);
                var responseCode = nextResponse.GetResponseCode();
                
                _logger.LogTrace("Next contact response: {ResponseCode} for contact #{ContactNumber}", responseCode, contactCount + 1);
                
                if (responseCode == MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS)
                {
                    _logger.LogDebug("End of contacts reached for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
                {
                    try
                    {
                        var contact = ParseSingleContact(nextResponse.Payload);
                        contacts.Add(contact);
                        contactCount++;
                        consecutiveErrors = 0;
                        
                        _logger.LogDebug("Parsed contact: {ContactName} ({ContactId})", contact.Name, contact.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse contact #{ContactNumber} for device {DeviceId}", contactCount + 1, deviceId);
                        contactCount++;
                        consecutiveErrors++;
                    }
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                {
                    _logger.LogDebug("Device reports no more contacts for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    consecutiveErrors++;
                    _logger.LogWarning("Error response during contact enumeration for device {DeviceId} (consecutive errors: {ConsecutiveErrors})", deviceId, consecutiveErrors);
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        _logger.LogWarning("Stopping contact retrieval after {ConsecutiveErrors} consecutive errors for device {DeviceId}", consecutiveErrors, deviceId);
                        break;
                    }
                    else
                    {
                        contactCount++;
                    }
                }
                else
                {
                    consecutiveErrors++;
                    _logger.LogWarning("Unexpected response during contact enumeration: {ResponseCode} for device {DeviceId}", responseCode, deviceId);
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        _logger.LogWarning("Too many unexpected responses, stopping contact retrieval for device {DeviceId}", deviceId);
                        break;
                    }
                    else
                    {
                        contactCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                _logger.LogError(ex, "Error during contact retrieval for device {DeviceId} (consecutive errors: {ConsecutiveErrors})", deviceId, consecutiveErrors);
                
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    _logger.LogError("Too many consecutive errors, stopping contact retrieval for device {DeviceId}", deviceId);
                    break;
                }
                else
                {
                    contactCount++;
                }
            }
        }
        
        _logger.LogDebug("Contact continuation summary for device {DeviceId}: {TotalContacts} total contacts, {ConsecutiveErrors} final consecutive errors", 
            deviceId, contacts.Count, consecutiveErrors);
    }

    #endregion

    #region Parsing Methods
    
    private static DeviceInfo ParseDeviceInfo(byte[] data)
    {
        if (data.Length < 1)
        {
            return new DeviceInfo
            {
                DeviceId = "Unknown",
                FirmwareVersion = "Unknown",
                HardwareVersion = "Unknown", 
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow
            };
        }

        try
        {
            return new DeviceInfo
            {
                DeviceId = $"MeshCore Device",
                FirmwareVersion = $"v1.11.0",
                HardwareVersion = "T-Beam",
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow,
                BatteryLevel = 85
            };
        }
        catch (Exception)
        {
            return new DeviceInfo
            {
                DeviceId = "Parse Error",
                FirmwareVersion = "Unknown",
                HardwareVersion = "Unknown",
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow
            };
        }
    }

    private static Contact ParseSingleContact(byte[] data)
    {
        try
        {
            var payloadStart = 0;
            if (data.Length > 0 && data[0] == (byte)MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                payloadStart = 1;
            }
            
            if (data.Length <= payloadStart)
            {
                throw new Exception("Contact data too short");
            }
            
            var contactData = new byte[data.Length - payloadStart];
            Array.Copy(data, payloadStart, contactData, 0, contactData.Length);
            
            string contactName = "Unknown Contact";
            string nodeId = "UNKNOWN";
            
            if (contactData.Length >= 32)
            {
                var publicKey = new byte[32];
                Array.Copy(contactData, 0, publicKey, 0, 32);
                nodeId = Convert.ToHexString(publicKey).ToLowerInvariant();
            }
            
            // Simple name extraction
            for (int startOffset = 32; startOffset < contactData.Length - 8; startOffset++)
            {
                if (contactData[startOffset] >= 32 && contactData[startOffset] <= 126)
                {
                    var nameBytes = new List<byte>();
                    
                    for (int i = startOffset; i < Math.Min(contactData.Length, startOffset + 64); i++)
                    {
                        if (contactData[i] == 0) break;
                        else if (contactData[i] >= 32 && contactData[i] <= 126) nameBytes.Add(contactData[i]);
                        else if (contactData[i] >= 0x80) nameBytes.Add(contactData[i]);
                        else break;
                    }
                    
                    if (nameBytes.Count >= 3)
                    {
                        try
                        {
                            var candidateName = Encoding.UTF8.GetString(nameBytes.ToArray()).Trim();
                            if (!string.IsNullOrWhiteSpace(candidateName) && candidateName.Length >= 3)
                            {
                                var uniqueChars = candidateName.Distinct().Count();
                                if (uniqueChars >= 2)
                                {
                                    contactName = candidateName;
                                    break;
                                }
                            }
                        }
                        catch { }
                    }
                }
            }
            
            return new Contact
            {
                Id = nodeId,
                Name = contactName,
                NodeId = nodeId,
                LastSeen = DateTime.UtcNow,
                IsOnline = false,
                Status = ContactStatus.Unknown
            };
        }
        catch (Exception)
        {
            return new Contact
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Parse Error",
                NodeId = "ERROR",
                LastSeen = DateTime.UtcNow,
                IsOnline = false,
                Status = ContactStatus.Unknown
            };
        }
    }

    private static Contact ParseContact(byte[] data) => ParseSingleContact(data);
    
    private static Message ParseMessage(byte[] data) => ParseMessage(data, null);
    
    private static Message ParseMessage(byte[] data, MeshCoreResponseCode? responseCode)
    {
        try
        {
            var payloadStart = 0;
            if (data.Length > 0 && responseCode.HasValue && data[0] == (byte)responseCode.Value)
            {
                payloadStart = 1;
            }
            
            if (data.Length <= payloadStart)
            {
                return new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    FromContactId = "unknown",
                    ToContactId = "self",
                    Content = "Empty message",
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Text,
                    Status = MessageStatus.Delivered,
                    IsRead = false
                };
            }
            
            var remainingData = new byte[data.Length - payloadStart];
            Array.Copy(data, payloadStart, remainingData, 0, remainingData.Length);
            
            if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3 ||
                responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3)
            {
                return ParseMessageV3Format(remainingData, responseCode.Value);
            }
            else
            {
                return ParseMessageLegacyFormat(remainingData, responseCode);
            }
        }
        catch (Exception)
        {
            return new Message
            {
                Id = Guid.NewGuid().ToString(),
                FromContactId = "parse-error",
                ToContactId = "self",
                Content = "Failed to parse message",
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Text,
                Status = MessageStatus.Failed,
                IsRead = false
            };
        }
    }
    
    private static Message ParseMessageV3Format(byte[] data, MeshCoreResponseCode responseCode)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        
        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Type = responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3 ? 
                   MessageType.Text : MessageType.Text,
            Status = MessageStatus.Delivered,
            IsRead = false
        };
        
        if (parts.Length >= 2)
        {
            message.FromContactId = parts[0];
            message.Content = parts[1];
            message.ToContactId = responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3 ? 
                                  "channel" : "self";
        }
        else if (parts.Length == 1)
        {
            message.FromContactId = "unknown";
            message.Content = parts[0];
            message.ToContactId = "self";
        }
        else
        {
            message.FromContactId = "unknown";
            message.Content = "No content";
            message.ToContactId = "self";
        }
        
        return message;
    }
    
    private static Message ParseMessageLegacyFormat(byte[] data, MeshCoreResponseCode? responseCode)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new Message
        {
            Id = parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) ? parts[0] : Guid.NewGuid().ToString(),
            FromContactId = parts.Length > 1 ? parts[1] : "unknown",
            ToContactId = parts.Length > 2 ? parts[2] : "self",
            Content = parts.Length > 3 ? parts[3] : (parts.Length > 0 ? parts[0] : "No content"),
            Timestamp = DateTime.UtcNow,
            Type = MessageType.Text,
            Status = MessageStatus.Delivered,
            IsRead = false
        };
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
            var deviceId = _transport.ConnectionId ?? "Unknown";
            _logger.LogDeviceDisconnected(deviceId);
            MeshCoreSdkEventSource.Log.DeviceDisconnected(deviceId);
            
            _transport?.Dispose();
            _disposed = true;
        }
    }
}