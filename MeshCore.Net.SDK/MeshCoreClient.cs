// <copyright file="Advertisement.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Collections.Concurrent;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using MeshCore.Net.SDK.Exceptions;
using MeshCore.Net.SDK.Logging;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Serialization;
using MeshCore.Net.SDK.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MeshCore.Net.SDK;

/// <summary>
/// Main client for interacting with MeshCore devices via USB or Bluetooth
/// </summary>
public class MeshCoreClient : IDisposable
{
    /// <summary>
    /// Maximum number of channels supported by the MeshCore device
    /// </summary>
    public const int MaxChannelsSupported = 40;

    private readonly ITransport _transport;
    private readonly ILogger<MeshCoreClient> _logger;
    private readonly ILoggerFactory? _loggerFactory;
    private bool _disposed;

    /// <summary>
    /// Event fired when a message is received from the MeshCore device
    /// </summary>
    public event EventHandler<Message>? MessageReceived;

    /// <summary>
    /// Event fired when a contact's status changes
    /// </summary>
    public event EventHandler<Contact>? ContactStatusChanged;

    /// <summary>
    /// Event fired when the network status changes
    /// </summary>
#pragma warning disable CS0067 // Event is declared but never used - will be implemented in future versions
    public event EventHandler<NetworkStatus>? NetworkStatusChanged;
#pragma warning restore CS0067

    /// <summary>
    /// Event fired when an error occurs during communication
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Gets whether the client is currently connected to a MeshCore device
    /// </summary>
    public bool IsConnected => _transport.IsConnected;

    /// <summary>
    /// Gets the connection identifier for the current transport
    /// </summary>
    public string? ConnectionId => _transport.ConnectionId;

    /// <summary>
    /// Creates a new MeshCodeClient with the specified transport and optional logger
    /// </summary>
    /// <param name="transport">The transport layer to use for communication</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging</param>
    private MeshCoreClient(ITransport transport, ILoggerFactory? loggerFactory = null)
    {
        _transport = transport;
        _loggerFactory = loggerFactory;
        _logger = _loggerFactory?.CreateLogger<MeshCoreClient>() ?? NullLogger<MeshCoreClient>.Instance;

        _transport.ErrorOccurred += OnTransportError;

        // Log SDK initialization
        _logger.LogSdkInitialized(GetType().Assembly.GetName().Version?.ToString() ?? "Unknown");

        // Also log to ETW
        MeshCoreSdkEventSource.Log.SdkInitialized(GetType().Assembly.GetName().Version?.ToString() ?? "Unknown");
    }

    /// <summary>
    /// Creates a new MeshCodeClient for a specific device with optional logger
    /// </summary>
    /// <param name="device">The MeshCore device to connect to</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging</param>
    private MeshCoreClient(MeshCoreDevice device, ILoggerFactory? loggerFactory = null)
        : this(TransportFactory.CreateTransport(device), loggerFactory)
    {
    }

    /// <summary>
    /// Creates a new MeshCodeClient with a connection string (backward compatibility)
    /// </summary>
    /// <param name="connectionString">Connection string specifying the device (e.g., "COM3")</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging</param>
    private MeshCoreClient(string connectionString, ILoggerFactory? loggerFactory = null)
        : this(TransportFactory.CreateTransport(connectionString), loggerFactory)
    {
    }

    /// <summary>
    /// Establishes an asynchronous connection to the specified MeshCoreDevice and returns a MeshCodeClient instance for
    /// communication.
    /// </summary>
    /// <param name="transport">The transport layer to use for communication</param>
    /// <param name="loggerFactory">An optional ILoggerFactory used to create loggers for the client. If null, logging is disabled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a MeshCodeClient instance connected
    /// to the specified device.</returns>
    public static async Task<MeshCoreClient> ConnectAsync(ITransport transport, ILoggerFactory? loggerFactory = null)
    {
        MeshCoreClient meshCoreClient = new MeshCoreClient(transport, loggerFactory);
        await meshCoreClient.ConnectAsync();
        return meshCoreClient;
    }

    /// <summary>
    /// Establishes an asynchronous connection to the specified MeshCoreDevice and returns a MeshCodeClient instance for
    /// communication.
    /// </summary>
    /// <param name="device">The MeshCoreDevice to connect to. Cannot be null.</param>
    /// <param name="loggerFactory">An optional ILoggerFactory used to create loggers for the client. If null, logging is disabled.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a MeshCodeClient instance connected
    /// to the specified device.</returns>
    public static async Task<MeshCoreClient> ConnectAsync(MeshCoreDevice device, ILoggerFactory? loggerFactory = null)
    {
        MeshCoreClient meshCoreClient = new MeshCoreClient(device, loggerFactory);
        await meshCoreClient.ConnectAsync();
        return meshCoreClient;
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
        var logger = loggerFactory?.CreateLogger<MeshCoreClient>() ?? NullLogger<MeshCoreClient>.Instance;
        logger.LogDeviceDiscoveryStarted("All");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("All");

        return TransportFactory.DiscoverAllDevicesAsync(timeout);
    }

    /// <summary>
    /// Discovers USB MeshCore devices only (backward compatibility)
    /// </summary>
    public static async Task<List<string>> DiscoverUsbDevicesAsync(ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory?.CreateLogger<MeshCoreClient>() ?? NullLogger<MeshCoreClient>.Instance;
        logger.LogDeviceDiscoveryStarted("USB");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("USB");

        var devices = await UsbTransport.DiscoverDevicesAsync(cancellationToken: cancellationToken);
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
        var logger = loggerFactory?.CreateLogger<MeshCoreClient>() ?? NullLogger<MeshCoreClient>.Instance;
        logger.LogDeviceDiscoveryStarted("BluetoothLE");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("BluetoothLE");

        return BluetoothTransport.DiscoverDevicesAsync(timeout);
    }

    #region Device Operations

    /// <summary>
    /// Gets device information
    /// </summary>
    public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(GetDeviceInfoAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            MeshCoreFrame response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 }, cancellationToken);

            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, "Failed to get device info");

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, ex.Message);

                throw ex;
            }

            var deviceInfo = DeviceInfoSerialization.Instance.Deserialize(response.Payload);

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
        var timestamp = (uint)((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        var data = BitConverter.GetBytes(timestamp);

        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_DEVICE_TIME, data);

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
    public async Task<DateTime?> TryGetDeviceTimeAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";

        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME, cancellationToken: cancellationToken);

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
        if (data.Length >= 5) // Need at least 5 bytes: response code (1) + timestamp (4)
        {
            var timestamp = BitConverter.ToUInt32(data, 1); // Skip response code at offset 0
            var deviceTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

            // Log the raw hex data for debugging firmware time issues
            var hex = BitConverter.ToString(data, 1, 4).Replace("-", string.Empty); // Skip response code
            _logger.LogDebug("Device time raw payload: {Hex}, decoded timestamp: {Timestamp}, decoded time: {Time}",
                hex, timestamp, deviceTime);

            return deviceTime;
        }

        return default(DateTime);
    }

    /// <summary>
    /// Resets the device
    /// </summary>
    public async Task ResetDeviceAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var rebootData = Encoding.ASCII.GetBytes("reboot");

        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REBOOT, rebootData);

        // No response expected as device will restart
        _logger.LogInformation("Device reboot command sent to {DeviceId}", deviceId);
    }

    #endregion

    #region Contact Operations

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<Contact>> GetContactsAsync(CancellationToken cancellationToken)
    {
        ConcurrentDictionary<ContactPublicKey, Contact> contacts = new ConcurrentDictionary<ContactPublicKey, Contact>();

        void OnContactReceived(object? sender, Contact contact)
        {
            contacts.TryAdd(contact.PublicKey, contact);
        }
        ;

        ContactStatusChanged += OnContactReceived;

        try
        {
            await GetContactListAsync(cancellationToken);
        }
        finally
        {
            ContactStatusChanged -= OnContactReceived;
        }

        return contacts.Values.ToList();
    }

    /// <summary>
    /// Gets all contacts
    /// </summary>
    private async Task GetContactListAsync(CancellationToken cancellationToken)
    {
        uint lastmod = 0;
        var deviceId = _transport.ConnectionId ?? "Unknown";

        _logger.LogContactRetrievalStarted(deviceId);
        MeshCoreSdkEventSource.Log.ContactRetrievalStarted(deviceId);

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            if (!frame.IsOutbound)
            {
                return;
            }

            var frameResponseCode = frame.GetResponseCode();

            if (frameResponseCode == null)
            {
                _logger.LogWarning("Received frame with null response code on {DeviceId}. Frame: {Frame}", deviceId, frame);
            }

            if (frameResponseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                Contact? contact;
                if (TryDeserializeContact(frame.Payload, out contact) && (contact != null))
                {
                    MeshCoreSdkEventSource.Log.ContactParsed(contact.Name, contact.PublicKey.ToString());
                    ContactStatusChanged?.Invoke(this, contact);
                }
                else
                {
                    _logger.LogWarning("Received contact response could not be parsed for device {DeviceId}", deviceId);
                }
            }
        }
        ;

        try
        {
            // Subscribe to frame events
            _transport.FrameReceived += OnFrameReceived;

            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                byte[]? payload = null;
                if (lastmod != 0)
                {
                    payload = BitConverter.GetBytes(lastmod);
                }

                var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_CONTACT_LIST_GET, payload, cancellationToken);
                var responseCode = response.GetResponseCode();
                var status = response.GetStatus();

                // Handle different protocol variations for contact retrieval
                if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACTS_START)
                {
                    // Bytes [1..4] are the total contacts count as a little-endian uint32.
                    var totalCount = BitConverter.ToUInt32(response.Payload, 1);

                    _logger.LogDebug("Device {DeviceId} using standard contact retrieval protocol with total count: {TotalCount}", deviceId, totalCount);

                    // Standard protocol: CONTACTS_START -> CONTACT... -> END_OF_CONTACTS
                    lastmod = await ParseContactsSequenceAsync(cancellationToken);

                    _logger.LogContactRetrievalCompleted(deviceId);
                    MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);

                    if (lastmod == 0)
                    {
                        return;
                    }
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
                {
                    // Alternative protocol: Direct CONTACT response (single contact or first of many)
                    _logger.LogDebug("Device {DeviceId} using direct contact response protocol", deviceId);

                    // Parse the first contact from the initial response
                    try
                    {
                        Contact? contact;
                        if (TryDeserializeContact(response.Payload, out contact) && (contact != null))
                        {
                            MeshCoreSdkEventSource.Log.ContactParsed(contact.Name, contact.PublicKey.ToString());
                            ContactStatusChanged?.Invoke(this, contact);
                        }
                        else
                        {
                            _logger.LogWarning("Received contact response could not be parsed for device {DeviceId}", deviceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogContactParsingFailed(ex);
                        MeshCoreSdkEventSource.Log.ContactParsingFailed(ex.Message);
                    }

                    // Continue retrieving additional contacts using SYNC_NEXT_MESSAGE
                    await ParseContactsSequenceAsync(cancellationToken);

                    _logger.LogContactRetrievalCompleted(deviceId);
                    MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    // Handle error responses
                    var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                    _logger.LogDebug("Device {DeviceId} returned RESP_CODE_ERR for contacts with status {StatusByte} ({Status})", deviceId, statusByte, status?.ToString() ?? "null");

                    switch (statusByte)
                    {
                        case (byte)MeshCoreStatus.InvalidParameter:
                            _logger.LogInformation("Device {DeviceId} reports no contacts available (empty contact list)", deviceId);
                            _logger.LogContactRetrievalCompleted(deviceId);
                            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                            return;

                        case (byte)MeshCoreStatus.InvalidCommand:
                            _logger.LogWarning("Device {DeviceId} does not support contact operations", deviceId);
                            throw new ProtocolException((byte)MeshCoreCommand.CMD_CONTACT_LIST_GET, statusByte,
                                "Get contacts command not supported by this device firmware");

                        case (byte)MeshCoreStatus.DeviceError:
                            _logger.LogWarning("Device {DeviceId} is in an error state for contact operations", deviceId);
                            // Return empty list for device errors as this might be recoverable
                            _logger.LogContactRetrievalCompleted(deviceId);
                            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                            return;

                        case (byte)MeshCoreStatus.NetworkError:
                            _logger.LogWarning("Device {DeviceId} has a network error for contact operations", deviceId);
                            // Return empty list for network errors as this might be recoverable
                            _logger.LogContactRetrievalCompleted(deviceId);
                            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                            return;

                        case (byte)MeshCoreStatus.TimeoutError:
                            _logger.LogWarning("Device {DeviceId} has a timeout error for contact operations", deviceId);
                            // Return empty list for timeout errors as this might be recoverable
                            _logger.LogContactRetrievalCompleted(deviceId);
                            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                            return;

                        case (byte)MeshCoreStatus.UnknownError:
                            _logger.LogWarning("Device {DeviceId} has an unknown error for contact operations", deviceId);
                            // Return empty list for unknown errors as this might be recoverable
                            _logger.LogContactRetrievalCompleted(deviceId);
                            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
                            return;

                        default:
                            _logger.LogWarning("Device {DeviceId} returned error for contacts with status {Status}", deviceId, statusByte);
                            throw new ProtocolException((byte)MeshCoreCommand.CMD_CONTACT_LIST_GET, statusByte,
                                $"Device returned error status {statusByte} for contact retrieval");
                    }
                }
                else
                {
                    // Unexpected response code
                    _logger.LogWarning("Device {DeviceId} returned unexpected response code {ResponseCode} for contacts", deviceId, responseCode);
                    throw new ProtocolException((byte)MeshCoreCommand.CMD_CONTACT_LIST_GET, 0x01,
                        $"Unexpected response code {responseCode} for contact retrieval");
                }
            } while (cancellationToken.IsCancellationRequested == false);
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
            _logger.LogContactRetrievalCompleted(deviceId);
            MeshCoreSdkEventSource.Log.ContactRetrievalCompleted(deviceId);
        }
        finally
        {
            _transport.FrameReceived -= OnFrameReceived;
        }
    }

    /// <summary>
    /// Attempts to retrieve a single contact from the device by its full 32-byte public key
    /// using the <c>CMD_GET_CONTACT_BY_KEY</c> protocol command.
    /// </summary>
    /// <param name="publicKey">The 32-byte public key of the contact to look up.</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// <see cref="Contact"/> if found; otherwise, <c>null</c> when the device reports
    /// that the contact does not exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="publicKey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="publicKey"/> is not exactly 32 bytes long as required
    /// by the MeshCore protocol.
    /// </exception>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error other than "not found" or an unexpected
    /// response code for the contact lookup request.
    /// </exception>
    public async Task<Contact?> TryGetContactAsync(ContactPublicKey publicKey, CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(TryGetContactAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, publicKey={PublicKey}",
            operationName,
            deviceId,
            publicKey);

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            // Wire format for CMD_GET_CONTACT_BY_KEY:
            //   [CMD_GET_CONTACT_BY_KEY][pub_key(32)]
            // The transport layer adds the command byte; we send only the 32‑byte key.
            var response = await _transport.SendCommandAsync(
                MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                publicKey.Value,
                cancellationToken);

            var responseCode = response.GetResponseCode();

            if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                Contact? contact;
                if (!TryDeserializeContact(response.Payload, out contact) || contact == null)
                {
                    var ex = new ProtocolException(
                        (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                        0x01,
                        "Failed to parse contact payload from CMD_GET_CONTACT_BY_KEY response.");

                    _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY, 0x01);
                    MeshCoreSdkEventSource.Log.ProtocolError(
                        (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                        0x01,
                        ex.Message);

                    throw ex;
                }

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug(
                    "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. Contact={ContactName} ({ContactKey})",
                    operationName,
                    deviceId,
                    (long)duration,
                    contact.Name,
                    contact.PublicKey);
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                return contact;
            }

            if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                // Firmware uses ERR_CODE_NOT_FOUND (0x02) when the contact does not exist.
                if (statusByte == 0x02)
                {
                    _logger.LogDebug(
                        "Contact with publicKey={PublicKey} not found on device {DeviceId}",
                        publicKey,
                        deviceId);

                    var durationNotFound = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)durationNotFound);

                    return null;
                }

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand => "Get contact by key command not supported by this device firmware.",
                    MeshCoreStatus.InvalidParameter => "Invalid public key supplied for contact lookup.",
                    MeshCoreStatus.DeviceError => "Device is in an error state and cannot provide contact information.",
                    MeshCoreStatus.NetworkError => "Network error occurred while retrieving contact information.",
                    MeshCoreStatus.TimeoutError => "Timeout occurred while retrieving contact information.",
                    MeshCoreStatus.UnknownError => "Unknown error occurred while retrieving contact information.",
                    _ => $"Failed to get contact by key (status: 0x{statusByte:X2})."
                };

                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                    statusByte,
                    errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError(
                    (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                    statusByte,
                    ex.Message);

                throw ex;
            }

            // Any other response code is unexpected for this command.
            var unexpected = new ProtocolException(
                (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                0x01,
                $"Unexpected response code {responseCode} for contact lookup request.");
            _logger.LogProtocolError(unexpected, (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY, 0x01);
            MeshCoreSdkEventSource.Log.ProtocolError(
                (byte)MeshCoreCommand.CMD_GET_CONTACT_BY_KEY,
                0x01,
                unexpected.Message);

            throw unexpected;
        }
        catch (Exception ex) when (ex is not ProtocolException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Adds a new contact
    /// </summary>
    public async Task<Contact> AddContactAsync(string name, ContactPublicKey publicKey)
    {
        var data = Encoding.UTF8.GetBytes($"{name}\0{publicKey}");
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
            Name = name,
            PublicKey = publicKey
        };
    }

    /// <summary>
    /// Deletes a contact
    /// </summary>
    public async Task DeleteContactAsync(ContactPublicKey publicKey, CancellationToken cancellationToken = default)
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REMOVE_CONTACT, publicKey.Value, cancellationToken);

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

        var responseCode = response.GetResponseCode();
        switch (responseCode)
        {
            case MeshCoreResponseCode.RESP_CODE_SENT:

                Message? message;
                if (!MessageV3Serialization.Instance.TryDeserialize(response.Payload, out message) || (message == null))
                {
                    _logger.LogMessageSendingFailed(new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 0x01, "Failed to parse sent message"), toContactId);
                    MeshCoreSdkEventSource.Log.MessageSendingFailed(toContactId, "Failed to parse sent message");
                    throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 0x01, "Failed to parse sent message");
                }

                _logger.LogMessageSent(toContactId);
                MeshCoreSdkEventSource.Log.MessageSent(toContactId);

                return message;

            default:

                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, statusByte, "Failed to send message");

                _logger.LogMessageSendingFailed(ex, toContactId);
                MeshCoreSdkEventSource.Log.MessageSendingFailed(toContactId, ex.Message);

                throw ex;
        }
    }

    /// <summary>
    /// Gets pending messages from the device queue, similar to getFromOfflineQueue in MyMesh.cpp
    /// This is the core message retrieval mechanism
    /// </summary>
    public async Task SyncronizeQueueAsync(CancellationToken cancellationToken)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        _logger.LogMessageRetrievalStarted(deviceId);

        try
        {
            do
            {
                var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE, cancellationToken: cancellationToken);
                var responseCode = response.GetResponseCode();

                cancellationToken.ThrowIfCancellationRequested();

                switch (responseCode)
                {
                    case MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES:
                        {
                            _logger.LogDebug("No more messages in queue for device {DeviceId}", deviceId);
                            return;
                        }
                    case MeshCoreResponseCode.RESP_CODE_ERR:
                        {
                            _logger.LogWarning("Error response during message sync for device {DeviceId}", deviceId);
                            return;
                        }
                    case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV:
                        {
                            try
                            {
                                Message? message;
                                if (!MessageLegacySerialization.Instance.TryDeserialize(response.Payload, out message) || (message == null))
                                {
                                    _logger.LogWarning("Failed to parse message for device {DeviceId}", deviceId);
                                    break;
                                }

                                _logger.LogDebug("Retrieved message from {FromContactId} for device {DeviceId}", message.FromContactId, deviceId);
                                MessageReceived?.Invoke(this, message);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogWarning(parseEx, "Failed to parse message for device {DeviceId}", deviceId);
                            }

                            continue;
                        }
                    case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3:
                        {
                            try
                            {
                                Message? message;
                                if (!MessageV3Serialization.Instance.TryDeserialize(response.Payload, out message) || (message == null))
                                {
                                    _logger.LogWarning("Failed to parse message for device {DeviceId}", deviceId);
                                    break;
                                }

                                _logger.LogDebug("Retrieved message from {FromContactId} for device {DeviceId}", message.FromContactId, deviceId);
                                MessageReceived?.Invoke(this, message);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogWarning(parseEx, "Failed to parse message for device {DeviceId}", deviceId);
                            }

                            continue;
                        }
                    case MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV:
                        {
                            try
                            {
                                Message? message;
                                if (!MessageChannelLegacySerialization.Instance.TryDeserialize(response.Payload, out message) || (message == null))
                                {
                                    _logger.LogWarning("Failed to parse message for device {DeviceId}", deviceId);
                                    break;
                                }

                                _logger.LogDebug("Retrieved message from {FromContactId} for device {DeviceId}", message.FromContactId, deviceId);
                                MessageReceived?.Invoke(this, message);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogWarning(parseEx, "Failed to parse message for device {DeviceId}", deviceId);
                            }

                            continue;
                        }
                    case MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3:
                        {
                            try
                            {
                                Message? message;
                                if (!MessageChannelV3Serialization.Instance.TryDeserialize(response.Payload, out message) || (message == null))
                                {
                                    _logger.LogWarning("Failed to parse message for device {DeviceId}", deviceId);
                                    break;
                                }

                                _logger.LogDebug("Retrieved message from {FromContactId} for device {DeviceId}", message.FromContactId, deviceId);
                                MessageReceived?.Invoke(this, message);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogWarning(parseEx, "Failed to parse message for device {DeviceId}", deviceId);
                            }

                            continue;
                        }
                    case MeshCoreResponseCode.RESP_CODE_CONTACT:
                        {
                            // Handle contact information that can be queued in the offline queue
                            // This happens when contact iteration is active or contacts are being synchronized
                            // These are not messages, so we continue without adding to the message list
                            _logger.LogDebug("Received contact information during message sync for device {DeviceId}, skipping", deviceId);

                            // Trigger the ContactStatusChanged event if there are listeners
                            try
                            {
                                Contact? contact;
                                if (!TryDeserializeContact(response.Payload, out contact) || (contact == null))
                                {
                                    _logger.LogWarning("Failed to parse message for contact {DeviceId}", deviceId);
                                    break;
                                }

                                _logger.LogDebug("Retrieved contact from {PublicKey} for device {DeviceId}", contact.PublicKey, deviceId);
                                ContactStatusChanged?.Invoke(this, contact);
                            }
                            catch (Exception parseEx)
                            {
                                _logger.LogDebug(parseEx, "Failed to parse contact information during message sync for device {DeviceId}", deviceId);
                            }

                            // Continue to next iteration - this is not a message
                            continue;
                        }
                    case MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS:
                        {
                            // Handle end of contact iteration that can be queued in the offline queue
                            _logger.LogDebug("Received end of contacts marker during message sync for device {DeviceId}, skipping", deviceId);

                            // Continue to next iteration - this is not a message
                            continue;
                        }
                }

                // Small delay to prevent overwhelming the device
                await Task.Delay(50);

            } while (!cancellationToken.IsCancellationRequested);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during message sync attempt on device {DeviceId}", deviceId);
        }

        _logger.LogSyncQueueCompleted(deviceId);
    }

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

    #endregion

    #region Channel Operations

    /// <summary>
    /// Gets the public channel configuration by listing all channels and finding the default
    /// </summary>
    /// <returns>The public channel configuration</returns>
    public async Task<Channel> GetPublicChannelAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(GetPublicChannelAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            // According to the research documentation, we should use the channels listing approach
            // instead of CMD_GET_CHANNEL for a specific index. The device stores channels in 
            // /channels2 file with up to 40 entries, and index 0 is typically the Public channel.

            _logger.LogDebug("Attempting to retrieve channel list to find public channel for device {DeviceId}", deviceId);

            // Try to get all available channels first
            var allChannels = await GetChannelsAsync();

            // Look for the default public channel (should be at index 0)
            var channelConfig = allChannels.FirstOrDefault(c => c.IsDefaultChannel);

            if (channelConfig == default(Channel))
            {
                channelConfig = CreateDefaultChannel();
            }

            var defaultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug("Operation completed with default: {OperationName} for device: {DeviceId} in {Duration}ms", operationName, deviceId, (long)defaultDuration);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)defaultDuration);

            return channelConfig;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }
    /// <summary>
    /// Creates a default channel configuration for devices that don't support channel operations
    /// </summary>
    /// <returns>A default channel configuration</returns>
    private static Channel CreateDefaultChannel()
    {
        return new Channel
        {
            Index = 0,
            Name = "All",
            Frequency = 433175000, // Default LoRa frequency for MeshCore
            IsEncrypted = false
        };
    }

    /// <summary>
    /// Sets the channel configuration
    /// </summary>
    /// <param name="channelConfig">The channel configuration to set</param>
    /// <returns>The updated channel configuration</returns>
    public async Task<Channel> SetChannelAsync(Channel channelConfig)
    {
        if (channelConfig == null)
            throw new ArgumentNullException(nameof(channelConfig));

        if (string.IsNullOrWhiteSpace(channelConfig.Name))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelConfig));

        if (channelConfig.Name.Length > 31)
            throw new ArgumentException("Channel name cannot exceed 31 characters", nameof(channelConfig));

        // Frequency validation: hashtag channels (starting with '#') derive their
        // configuration from the name via firmware, so Frequency = 0 is valid.
        // For non-hashtag channels, a positive frequency is required.
        if (channelConfig.Frequency <= 0 && !channelConfig.Name.StartsWith('#'))
            throw new ArgumentException("Channel frequency must be greater than 0", nameof(channelConfig));

        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(SetChannelAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var data = SerializeChannel(channelConfig);

            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_CHANNEL, data);

            if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_OK)
            {
                // Success - update the channel configuration with actual values
                var updatedConfig = new Channel
                {
                    Index = channelConfig.Index,
                    Name = channelConfig.Name,
                    Frequency = channelConfig.Frequency,
                    IsEncrypted = channelConfig.IsEncrypted,
                    EncryptionKey = channelConfig.EncryptionKey
                };

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug("Channel configuration set successfully: {ChannelName} (ID: {ChannelIndex}) on device {DeviceId}",
                    channelConfig.Name, updatedConfig.Index, deviceId);
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                return updatedConfig;
            }
            else if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand =>
                        $"CMD_SET_CHANNEL returned InvalidCommand for channel '{channelConfig.Name}' (index {channelConfig.Index}). " +
                        $"This indicates a calling pattern issue. Sent payload: {Convert.ToHexString(data)}. " +
                        $"Expected binary format: [index(1)][name(32)][secret(16)] = 49 bytes. " +
                        $"Review protocol specification and reference implementations.",
                    _ => $"Failed to set channel configuration (status: 0x{statusByte:X2})"
                };

                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SET_CHANNEL, statusByte, errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_CHANNEL, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SET_CHANNEL, statusByte, ex.Message);

                throw ex;
            }
            else
            {
                // Unexpected response code - treat as successful but log warning
                _logger.LogWarning("Unexpected response code {ResponseCode} for CMD_SET_CHANNEL on device {DeviceId}, treating as success", response.GetResponseCode(), deviceId);

                var updatedConfig = new Channel
                {
                    Index = channelConfig.Index, // Use the actual ID (generated or provided)
                    Name = channelConfig.Name,
                    Frequency = channelConfig.Frequency,
                    IsEncrypted = channelConfig.IsEncrypted,
                    EncryptionKey = channelConfig.EncryptionKey
                };

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                return updatedConfig;
            }
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sends a message to a specific channel using the correct MeshCore protocol format
    /// Based on research: CMD_SEND_CHANNEL_TXT_MSG payload = CMD + TXT_TYPE + CHANNEL_IDX + TIMESTAMP + MESSAGE + NULL
    /// </summary>
    /// <param name="channelName">The name of the channel to send the message to</param>
    /// <param name="content">The message content</param>
    /// <returns>The sent message</returns>
    /// <exception cref="NotSupportedException">Thrown when the device does not support channel messaging</exception>
    public async Task SendChannelMessageAsync(string channelName, string content)
    {
        if (string.IsNullOrWhiteSpace(channelName))
            throw new ArgumentException("Channel name cannot be null or empty", nameof(channelName));

        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Message content cannot be null or empty", nameof(content));

        var deviceId = _transport.ConnectionId ?? "Unknown";

        _logger.LogDebug("Sending channel message to {ChannelName} from device {DeviceId}, content length: {ContentLength}",
            channelName, deviceId, content.Length);
        MeshCoreSdkEventSource.Log.MessageSendingStarted(channelName, content.Length);

        try
        {
            // Map channel names to numeric IDs by querying the device
            var channelConfig = await TryGetChannelAsync(channelName);
            if (channelConfig == null)
            {
                _logger.LogError("Channel '{ChannelName}' not found on device {DeviceId}. Cannot send message.", channelName, deviceId);
                throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, 0x02,
                    $"Channel '{channelName}' not found. Use CMD_SET_CHANNEL to configure the channel first.");
            }

            _logger.LogDebug("Mapped channel '{ChannelName}' to index {channelConfig.Id} for device {DeviceId}",
                channelName, channelConfig.Index.ToString(), deviceId);

            // Build the correct CMD_SEND_CHANNEL_TXT_MSG payload format:
            // CMD(0x03) + TXT_TYPE(0x00) + CHANNEL_IDX + TIMESTAMP(4 bytes) + MESSAGE + NULL
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var messageBytes = Encoding.UTF8.GetBytes(content);

            var payload = new List<byte>
            {
                // CMD is added automatically by transport layer
                0x00, // txt_type - 0x00 for plain text
                (byte)channelConfig.Index, // channel_idx - numeric channel ID
            };

            // Add timestamp (4 bytes, little-endian as per MeshCore protocol)
            var timestampBytes = BitConverter.GetBytes((uint)timestamp);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes); // Ensure little-endian on big-endian systems
            }
            payload.AddRange(timestampBytes);

            // Add message content
            payload.AddRange(messageBytes);

            // Add null terminator
            payload.Add(0x00);

            var payloadArray = payload.ToArray();

            _logger.LogDebug("Sending CMD_SEND_CHANNEL_TXT_MSG with payload: TXT_TYPE=0x00, CHANNEL_IDX=0x{ChannelIndex:X2}, TIMESTAMP={Timestamp}, MESSAGE_LEN={MessageLen}",
                channelConfig.Index, timestamp, messageBytes.Length);

            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, payloadArray);

            var responseCode = response.GetResponseCode();

            // According to research: CMD_SEND_CHANNEL_TXT_MSG should return RESP_CODE_SENT (0x06), not RESP_CODE_OK
            if (responseCode == MeshCoreResponseCode.RESP_CODE_SENT)
            {
                _logger.LogInformation("Channel message sent successfully to {ChannelName} (index {ChannelIndex}) from device {DeviceId}",
                    channelName, channelConfig.Index, deviceId);

                // Agent mode: Log message sent without a separate messageId parameter
                MeshCoreSdkEventSource.Log.MessageSent(channelName);

                return;
            }
            else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                // Handle specific error cases based on research
                if (status == MeshCoreStatus.InvalidCommand)
                {
                    _logger.LogError("Device {DeviceId} does not support CMD_SEND_CHANNEL_TXT_MSG", deviceId);
                    throw new NotSupportedException($"Channel messaging is not supported by this device firmware. Device {deviceId} does not recognize the CMD_SEND_CHANNEL_TXT_MSG command.");
                }
                else if (statusByte == 0x02) // ERR_CODE_NOT_FOUND from research
                {
                    _logger.LogError("Channel {ChannelName} (index {ChannelIndex}) not found on device {DeviceId}. Channel may need to be configured first.",
                        channelName, channelConfig.Index, deviceId);

                    throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, statusByte,
                        $"Channel '{channelName}' (index {channelConfig.Index}) not found. Use CMD_SET_CHANNEL to configure the channel first.");
                }
                else
                {
                    var errorMessage = status switch
                    {
                        MeshCoreStatus.InvalidParameter => "Invalid channel index or message content",
                        MeshCoreStatus.DeviceError => "Device is in an error state and cannot send messages",
                        MeshCoreStatus.NetworkError => "Network error occurred while sending message",
                        MeshCoreStatus.TimeoutError => "Message sending timed out",
                        _ => $"Failed to send channel message (status: 0x{statusByte:X2})"
                    };

                    var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, statusByte, errorMessage);

                    _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, statusByte);
                    MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, statusByte, ex.Message);

                    throw ex;
                }
            }
            else
            {
                // Unexpected response code - log warning but don't fail
                _logger.LogWarning("Unexpected response code {ResponseCode} for CMD_SEND_CHANNEL_TXT_MSG on device {DeviceId}, treating as success", responseCode, deviceId);

                // If it's any other success code, treat as success
                if (responseCode == MeshCoreResponseCode.RESP_CODE_OK)
                {
                    _logger.LogInformation("Channel message accepted with RESP_CODE_OK instead of expected RESP_CODE_SENT");

                    // Agent mode: Log message sent without messageId
                    MeshCoreSdkEventSource.Log.MessageSent(channelName);
                    return;
                }
                else
                {
                    throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG, 0x01,
                        $"Unexpected response code: {responseCode}");
                }
            }
        }
        catch (Exception ex) when (!(ex is ProtocolException) && !(ex is NotSupportedException))
        {
            _logger.LogUnexpectedError(ex, nameof(SendChannelMessageAsync));
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, nameof(SendChannelMessageAsync));
            throw;
        }
    }

    /// <summary>
    /// Discovers the actual channel configuration on the device by querying channel indices
    /// This helps us understand what channels are configured at each numeric index
    /// </summary>
    /// <returns>Dictionary mapping channel indices to channel names</returns>
    public async Task<IEnumerable<Channel>> GetChannelsAsync()
    {
        var result = new List<Channel>();
        var deviceId = _transport.ConnectionId ?? "Unknown";

        _logger.LogDebug("Discovering device channel configuration for {DeviceId}", deviceId);

        // According to research: channels are stored in /channels2 file with up to 40 entries
        // Query channel indices 0-9 to see what's configured (limiting to first 10 for efficiency)
        for (uint channelIndex = 0; channelIndex <= MeshCoreClient.MaxChannelsSupported; channelIndex++)
        {
            var channel = await TryGetChannelAsync(channelIndex);
            if (channel != null)
            {
                result.Add(channel);
            }
        }

        _logger.LogInformation("Device {DeviceId} has {ChannelCount} configured channels: {ChannelList}",
            deviceId, result.Count, string.Join(", ", result.Select(kvp => $"{kvp.Index}={kvp.Name}")));

        return result;
    }

    /// <summary>
    /// Attempts to retrieve the channel information for the specified channel index from the connected device
    /// asynchronously.
    /// </summary>
    /// <remarks>If the device does not have a channel configured at the specified index, the method returns
    /// <see langword="null"/>. All MeshCore devices are required to support channel queries and provide a public
    /// channel at index 0; failure to do so results in a <see cref="ProtocolException"/>.</remarks>
    /// <param name="channelIndex">The zero-based index of the channel to query. Index 0 refers to the public channel, which must be present on all
    /// MeshCore devices.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the channel information if found;
    /// otherwise, <see langword="null"/> if no channel is configured at the specified index.</returns>
    /// <exception cref="ProtocolException">Thrown if the device does not support channel commands or fails to provide the required public channel at index
    /// 0, indicating a device compatibility issue.</exception>
    public async Task<Channel?> TryGetChannelAsync(uint channelIndex)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";

        try
        {
            _logger.LogDebug("Querying channel index {ChannelIndex} on device {DeviceId}", channelIndex, deviceId);

            var channelIndexData = new byte[] { (byte)channelIndex };
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_CHANNEL, channelIndexData);

            var responseCode = response.GetResponseCode();

            // Check for success responses
            if (responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_INFO ||
                responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                responseCode == MeshCoreResponseCode.RESP_CODE_DEVICE_INFO) // Sometimes channels return device info format
            {
                // Parse channel info from response
                if (response.Payload.Length > 1)
                {
                    Channel? channel;
                    if (TryDeserializeChannel(response.Payload, out channel) && (channel != null))
                    {
                        _logger.LogInformation("Found channel at index {ChannelIndex}: '{ChannelName}' on device {DeviceId}",
                            channelIndex, channel.Name, deviceId);

                        return channel;
                    }
                }
                else if (channelIndex == 0)
                {
                    // Default public channel should always exist at index 0
                    throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CHANNEL, (byte)MeshCoreStatus.InvalidCommand,
                        $"Device {deviceId} does not support channel commands, but a public channel at index 0 is required by MeshCore protocol. This indicates a fundamental device compatibility issue.");
                }
            }
            else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();

                if (status == MeshCoreStatus.InvalidCommand && channelIndex == 0)
                {
                    // Every MeshCore device must support channel queries and have a public channel at index 0
                    throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CHANNEL, (byte)MeshCoreStatus.InvalidCommand,
                        $"Device {deviceId} does not support channel commands, but every MeshCore device must support CMD_GET_CHANNEL and have a public channel at index 0. This indicates a fundamental device compatibility issue.");
                }
                else if (status == MeshCoreStatus.InvalidParameter || status?.ToString().Contains("NotFound") == true)
                {
                    // Channel not found at this index - this is normal
                    _logger.LogDebug("No channel configured at index {ChannelIndex} on device {DeviceId}", channelIndex, deviceId);
                }
                else
                {
                    _logger.LogDebug("Unexpected error response for channel index {ChannelIndex} on device {DeviceId}: status {Status}",
                        channelIndex, deviceId, status);
                }
            }
            else
            {
                _logger.LogDebug("Unexpected response code {ResponseCode} for channel index {ChannelIndex} on device {DeviceId}",
                    responseCode, channelIndex, deviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error querying channel index {ChannelIndex} on device {DeviceId}: {ErrorMessage}",
                channelIndex, deviceId, ex.Message);

            // If this is index 0 and we get an error, assume it's the public channel
            if (channelIndex == 0)
            {
                throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CHANNEL, (byte)0x01,
                    $"Device {deviceId} failed to respond to channel query for index 0 (public channel). Error: {ex.Message}. Every MeshCore device must have a public channel at index 0.");
            }
        }

        return default(Channel);
    }

    /// <summary>
    /// Attempts to find a channel by name on the connected device by iterating through
    /// all channel indices and comparing names case-insensitively.
    /// </summary>
    /// <param name="channelName">The channel name to search for (case-insensitive match).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// <see cref="Channel"/> if a channel with the specified name is found; otherwise,
    /// <see langword="null"/>.
    /// </returns>
    public async Task<Channel?> TryGetChannelAsync(string channelName, CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";

        _logger.LogDebug("Searching for channel '{ChannelName}' on device {DeviceId}", channelName, deviceId);

        for (uint i = 0; i <= MaxChannelsSupported; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var channel = await TryGetChannelAsync(i);
            if (channel != null && string.Equals(channel.Name, channelName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Found channel '{ChannelName}' at index {ChannelIndex} on device {DeviceId}",
                    channelName, channel.Index, deviceId);
                return channel;
            }
        }

        _logger.LogDebug("Channel '{ChannelName}' not found on device {DeviceId}", channelName, deviceId);
        return null;
    }

    /// <summary>
    /// Ensures that a hashtag channel exists on the connected device. If a channel with the
    /// specified name already exists, it is returned unchanged. Otherwise, the channel is
    /// created in the first available slot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Hashtag channels (names beginning with <c>#</c>) use encryption keys that are
    /// automatically derived from the channel name by the firmware. All devices that
    /// create the same <c>#name</c> will compute the same key, allowing them to
    /// communicate without out-of-band key exchange.
    /// </para>
    /// <para>
    /// This corresponds to the Python CLI's <c>add_channel #name</c> command, which
    /// finds the first empty channel slot and calls
    /// <c>mc.commands.set_channel(idx, name, key=None)</c>, letting the firmware
    /// derive the secret from the channel name via SHA-256.
    /// </para>
    /// <para>
    /// Index 0 (the default public channel) is never overwritten. The search for an
    /// empty slot begins at index 1.
    /// </para>
    /// </remarks>
    /// <param name="channelName">
    /// The hashtag channel name (e.g., <c>"#MyChannel"</c> or <c>"MyChannel"</c>).
    /// If the leading <c>#</c> is omitted it is prepended automatically.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A task whose result is the <see cref="Channel"/> that was found or created on the device.
    /// When the channel already exists, the existing configuration is returned without modification.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="channelName"/> is <c>null</c>, empty, consists only of
    /// whitespace, or exceeds the 31-character firmware limit.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no empty channel slot is available on the device (all 40 slots are in use).
    /// </exception>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error while querying or setting the channel.
    /// </exception>
    public async Task<Channel> EnsureHashTagChannelAsync(
        string channelName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelName))
        {
            throw new ArgumentException("Channel name cannot be null or empty.", nameof(channelName));
        }

        // Normalize: ensure name starts with '#'
        if (!channelName.StartsWith('#'))
        {
            channelName = "#" + channelName;
        }

        if (channelName.Length > 31)
        {
            throw new ArgumentException(
                $"Channel name cannot exceed 31 characters (got {channelName.Length}).",
                nameof(channelName));
        }

        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(EnsureHashTagChannelAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, channelName='{ChannelName}'",
            operationName, deviceId, channelName);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            int? emptySlot = null;

            for (uint i = 1; i <= MaxChannelsSupported; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var channel = await TryGetChannelAsync(i);
                if (channel == null && emptySlot == null)
                {
                    emptySlot = (int)i;
                }

                if (channel != null && string.Equals(channel.Name, channelName, StringComparison.OrdinalIgnoreCase))
                {
                    var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    _logger.LogDebug(
                        "Channel '{ChannelName}' already exists at index {ChannelIndex} on device {DeviceId} in {Duration}ms",
                        channelName, channel.Index, deviceId, (long)duration);
                    MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
                    return channel;
                }
            }

            if (emptySlot == null)
            {
                throw new InvalidOperationException(
                    $"No empty channel slots available on device {deviceId}. " +
                    $"All {MaxChannelsSupported} slots are in use. " +
                    $"Remove an existing channel before adding '{channelName}'.");
            }

            _logger.LogDebug(
                "Adding hashtag channel '{ChannelName}' at index {ChannelIndex} on device {DeviceId}",
                channelName, emptySlot.Value, deviceId);

            // 3. Create the channel.
            //    For hashtag (#) channels, the encryption key is derived from
            //    SHA-256(name)[0:16] by the serializer. This matches the Python CLI
            //    behavior: sha256(channel_name.encode("utf-8")).digest()[0:16]
            var newChannel = new Channel
            {
                Index = (byte)emptySlot.Value,
                Name = channelName,
                Frequency = 0,
                IsEncrypted = true, // Hashtag channels always have a derived key
                EncryptionKey = null // Serializer computes SHA-256 key for # channels
            };

            var setResult = await SetChannelAsync(newChannel);

            // 4. Re-read the channel from the device to get the firmware-generated key
            //    (matches Python CLI pattern which does get_channel after set_channel)
            var confirmedChannel = await TryGetChannelAsync((uint)emptySlot.Value);

            var finalChannel = confirmedChannel ?? setResult;

            var finalDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Hashtag channel '{ChannelName}' created at index {ChannelIndex} on device {DeviceId} in {Duration}ms. " +
                "Encrypted={IsEncrypted}, Key={Key}",
                channelName, emptySlot.Value, deviceId, (long)finalDuration,
                finalChannel.IsEncrypted,
                finalChannel.EncryptionKey ?? "(firmware-derived)");

            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)finalDuration);

            return finalChannel;
        }
        catch (Exception ex) when (ex is not ProtocolException and not InvalidOperationException and not ArgumentException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    #endregion

    #region Configuration Operations

    /// <summary>
    /// Retrieves the current radio statistics from the connected device asynchronously
    /// by issuing CMD_GET_STATS with STATS_TYPE_RADIO (0x01).
    /// </summary>
    /// <remarks>
    /// This corresponds to the Python CLI command <c>get stats_radio</c> which calls
    /// <c>mc.commands.get_stats_radio()</c>. The device returns a 14-byte binary payload
    /// containing noise floor, RSSI, SNR, and cumulative air-time counters.
    /// </remarks>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A <see cref="RadioStats"/> object containing the radio statistics reported by the device.
    /// </returns>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error or an unexpected response to the radio stats query.
    /// </exception>
    public async Task<RadioStats> GetRadioStatsAsync(CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(GetRadioStatsAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            // CMD_GET_STATS (0x38) with STATS_TYPE_RADIO (0x01)
            byte[] requestPayload = new byte[] { 0x01 };
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_STATS, requestPayload, cancellationToken);

            var responseCode = response.GetResponseCode();

            switch (responseCode)
            {
                case MeshCoreResponseCode.RESP_CODE_STATS:

                    try
                    {
                        var radioStats = RadioStatsSerialization.Instance.Deserialize(response.Payload);

                        var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogDebug(
                            "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. {RadioStats}",
                            operationName, deviceId, (long)duration, radioStats);
                        MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                        return radioStats;
                    }
                    catch (Exception parseEx)
                    {
                        var errorMessage = $"Failed to parse radio stats from device response. " +
                            $"Response length: {response.Payload.Length} bytes. " +
                            $"Sent payload: {Convert.ToHexString(requestPayload)}. " +
                            $"Received payload: {Convert.ToHexString(response.Payload)}. " +
                            $"Parse error: {parseEx.Message}";
                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_STATS, 0x01, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_STATS, 0x01);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_STATS, 0x01, ex.Message);

                        throw ex;
                    }
                case MeshCoreResponseCode.RESP_CODE_ERR:
                    {
                        var status = response.GetStatus();
                        var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                        var errorMessage = status switch
                        {
                            MeshCoreStatus.InvalidCommand => "Radio stats command not supported by this device firmware",
                            MeshCoreStatus.DeviceError => "Device is in an error state and cannot provide radio stats",
                            MeshCoreStatus.NetworkError => "Network error occurred while retrieving radio stats",
                            MeshCoreStatus.TimeoutError => "Timeout occurred while retrieving radio stats",
                            MeshCoreStatus.InvalidParameter => "Invalid parameters for radio stats command",
                            MeshCoreStatus.UnknownError => "Unknown error occurred while retrieving radio stats",
                            _ => $"Failed to get radio stats (status: 0x{statusByte:X2})"
                        };

                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_STATS, statusByte, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_STATS, statusByte);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_STATS, statusByte, ex.Message);

                        throw ex;
                    }
                default:
                    {
                        var errorMessage = $"Unexpected response code {responseCode} for radio stats request. " +
                            $"Expected RESP_CODE_STATS (0x{(byte)MeshCoreResponseCode.RESP_CODE_STATS:X2}).";
                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_STATS, 0x01, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_STATS, 0x01);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_STATS, 0x01, ex.Message);

                        throw ex;
                    }
            }
        }
        catch (Exception ex) when (ex is not ProtocolException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sets the LoRa radio parameters on the connected MeshCore device.
    /// A device reboot is required for the new parameters to take effect.
    /// </summary>
    /// <remarks>
    /// This corresponds to the Python CLI command <c>set radio f,bw,sf,cr</c> which calls
    /// <c>mc.commands.set_radio(freq, bw, sf, cr)</c>.
    /// The wire format for CMD_SET_RADIO_PARAMS (0x0B) is:
    /// <code>
    /// [freq_khz: uint32 LE][bw_khz: uint32 LE][sf: uint8][cr: uint8]
    /// </code>
    /// where freq and bw are multiplied by 1000 from their MHz / kHz values.
    /// </remarks>
    /// <param name="radioParams">The radio parameters to set on the device.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="radioParams"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when radio parameter values are out of valid range.</exception>
    /// <exception cref="ProtocolException">Thrown when the device returns an error or unexpected response.</exception>
    public async Task SetRadioParamsAsync(RadioParams radioParams, CancellationToken cancellationToken = default)
    {
        if (radioParams == null)
        {
            throw new ArgumentNullException(nameof(radioParams));
        }

        if (radioParams.FrequencyMHz <= 0)
        {
            throw new ArgumentException("Frequency must be greater than 0", nameof(radioParams));
        }

        if (radioParams.BandwidthKHz <= 0)
        {
            throw new ArgumentException("Bandwidth must be greater than 0", nameof(radioParams));
        }

        if (radioParams.SpreadingFactor < 6 || radioParams.SpreadingFactor > 12)
        {
            throw new ArgumentException("Spreading factor must be between 6 and 12", nameof(radioParams));
        }

        if (radioParams.CodingRate < 5 || radioParams.CodingRate > 8)
        {
            throw new ArgumentException("Coding rate must be between 5 and 8", nameof(radioParams));
        }

        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(SetRadioParamsAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, params: {RadioParams}",
            operationName, deviceId, radioParams);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var payload = RadioParamsSerialization.Instance.Serialize(radioParams);

            _logger.LogDebug(
                "Sending CMD_SET_RADIO_PARAMS: {RadioParams}, payload={Payload}",
                radioParams, Convert.ToHexString(payload));

            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_RADIO_PARAMS, payload, cancellationToken);

            var responseCode = response.GetResponseCode();

            if (responseCode == MeshCoreResponseCode.RESP_CODE_OK)
            {
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug(
                    "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. {RadioParams}. Reboot required to apply.",
                    operationName, deviceId, (long)duration, radioParams);
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
                return;
            }

            if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand => "Set radio params command not supported by this device firmware",
                    MeshCoreStatus.InvalidParameter => "Invalid radio parameters supplied to device",
                    MeshCoreStatus.DeviceError => "Device is in an error state and cannot update radio parameters",
                    _ => $"Failed to set radio parameters (status: 0x{statusByte:X2})"
                };

                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, statusByte, errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, statusByte, ex.Message);

                throw ex;
            }

            var unexpected = new ProtocolException(
                (byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS,
                0x01,
                $"Unexpected response code {responseCode} for set radio params request.");

            _logger.LogProtocolError(unexpected, (byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, 0x01);
            MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, 0x01, unexpected.Message);

            throw unexpected;
        }
        catch (Exception ex) when (ex is not ProtocolException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Gets battery and storage information from the MeshCore device
    /// </summary>
    /// <returns>Battery and storage information including voltage, used storage, and total storage</returns>
    /// <exception cref="ProtocolException">Thrown when the device returns an error or unexpected response</exception>
    public async Task<BatteryAndStorage> GetBatteryAndStorageAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(GetBatteryAndStorageAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_BATT_AND_STORAGE);

            var responseCode = response.GetResponseCode();

            switch (responseCode)
            {
                case MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE:

                    try
                    {
                        var batteryAndStorage = BatteryAndStorageSerialization.Instance.Deserialize(response.Payload);

                        var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogDebug("Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. Battery: {BatteryVoltage}mV, Storage: {UsedStorage}/{TotalStorage}KB",
                            operationName, deviceId, (long)duration, batteryAndStorage.BatteryVoltage, batteryAndStorage.UsedStorage, batteryAndStorage.TotalStorage);
                        MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                        return batteryAndStorage;
                    }
                    catch (Exception parseEx)
                    {
                        var errorMessage = $"Failed to parse battery and storage data from device response. Response length: {response.Payload.Length} bytes. Parse error: {parseEx.Message}";
                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01, ex.Message);

                        throw ex;
                    }
                case MeshCoreResponseCode.RESP_CODE_ERR:
                    {
                        var status = response.GetStatus();
                        var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                        var errorMessage = status switch
                        {
                            MeshCoreStatus.InvalidCommand => "Battery and storage command not supported by this device firmware",
                            MeshCoreStatus.DeviceError => "Device is in an error state and cannot provide battery/storage information",
                            MeshCoreStatus.NetworkError => "Network error occurred while retrieving battery/storage information",
                            MeshCoreStatus.TimeoutError => "Timeout occurred while retrieving battery/storage information",
                            MeshCoreStatus.InvalidParameter => "Invalid parameters for battery/storage command",
                            MeshCoreStatus.UnknownError => "Unknown error occurred while retrieving battery/storage information",
                            _ => $"Failed to get battery and storage information (status: 0x{statusByte:X2})"
                        };

                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, statusByte, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, statusByte);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, statusByte, ex.Message);

                        throw ex;
                    }
                default:
                    {
                        // Unexpected response code
                        var errorMessage = $"Unexpected response code {responseCode} for battery and storage request. Expected RESP_CODE_BATT_AND_STORAGE ({(byte)MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE:X2}).";
                        var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01, errorMessage);

                        _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_BATT_AND_STORAGE, 0x01, ex.Message);

                        throw ex;
                    }
            }
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    #endregion

    #region Network Discovery Operations

    /// <summary>
    /// Sends a self advertisement to announce this device's presence on the mesh network
    /// This is equivalent to the CMD_SEND_SELF_ADVERT command in the MeshCore protocol
    /// </summary>
    /// <param name="advertisement">Advertisement configuration specifying broadcast mode and options</param>
    /// <returns>Task representing the async operation</returns>
    /// <exception cref="ArgumentNullException">Thrown when advertisement is null</exception>
    /// <exception cref="ProtocolException">Thrown when the device returns an error or doesn't support advertising</exception>
    private async Task SendSelfAdvertAsync(Advertisement advertisement)
    {
        if (advertisement == null)
        {
            throw new ArgumentNullException(nameof(advertisement));
        }

        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(SendSelfAdvertAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            // Serialize the advertisement configuration using the dedicated serializer
            var payload = AdvertisementSerialization.Instance.Serialize(advertisement);

            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SEND_SELF_ADVERT, payload);

            var responseCode = response.GetResponseCode();
            if (responseCode == MeshCoreResponseCode.RESP_CODE_OK)
            {
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogInformation("Self advertisement sent successfully from device {DeviceId} using {Mode} mode in {Duration}ms",
                    deviceId, advertisement.UseFloodMode ? "flood" : "zero-hop", (long)duration);
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
            }
            else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand => "Self advertisement command not supported by this device firmware",
                    MeshCoreStatus.DeviceError => "Device is in an error state and cannot send advertisement",
                    MeshCoreStatus.NetworkError => "Network error occurred while sending advertisement",
                    MeshCoreStatus.TimeoutError => "Advertisement sending timed out",
                    MeshCoreStatus.InvalidParameter => "Invalid advertisement parameters",
                    _ => $"Failed to send self advertisement (status: 0x{statusByte:X2})"
                };

                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, statusByte, errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, statusByte, ex.Message);

                throw ex;
            }
            else
            {
                // Unexpected response code
                var errorMessage = $"Unexpected response code {responseCode} for self advertisement. Expected RESP_CODE_OK ({(byte)MeshCoreResponseCode.RESP_CODE_OK:X2}).";
                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, 0x01, errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, 0x01);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SEND_SELF_ADVERT, 0x01, ex.Message);

                throw ex;
            }
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sends a self advertisement using flood mode (network-wide broadcast)
    /// This is a convenience method that creates an Advertisement with UseFloodMode = true
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public async Task SendSelfAdvertFloodAsync()
    {
        var advertisement = new Advertisement
        {
            UseFloodMode = true
        };

        await SendSelfAdvertAsync(advertisement);
    }

    /// <summary>
    /// Sends a self advertisement using zero-hop mode (local broadcast only)
    /// This is a convenience method that creates an Advertisement with UseFloodMode = false
    /// </summary>
    /// <returns>Task representing the async operation</returns>
    public async Task SendSelfAdvertZeroHopAsync()
    {
        var advertisement = new Advertisement
        {
            UseFloodMode = false
        };

        await SendSelfAdvertAsync(advertisement);
    }

    /// <summary>
    /// Gets the most recently observed advert path for a contact given its full 32-byte public key.
    /// </summary>
    /// <param name="publicKey">
    /// The 32-byte public key for the contact. The firmware will match this against the 7-byte
    /// pubkey_prefix stored in the advert path table.
    /// </param>
    /// <returns>
    /// An <see cref="AdvertPathInfo"/> instance containing the received timestamp and hop
    /// path from this node to the contact; or <c>null</c> if no path is known.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="publicKey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="publicKey"/> is not exactly 32 bytes long,
    /// which is required by the MeshCore protocol.
    /// </exception>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error response or an unexpected response code.
    /// </exception>
    public async Task<AdvertPathInfo?> TryGetAdvertPathAsync(ContactPublicKey publicKey)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const byte reserved = 0x00;

        // CMD_GET_ADVERT_PATH frame on wire:
        //   [CMD_GET_ADVERT_PATH][reserved][pub_key(32)]
        var payload = new byte[1 + publicKey.Value.Length];
        payload[0] = reserved;
        Buffer.BlockCopy(publicKey.Value, 0, payload, 1, publicKey.Value.Length);

        _logger.LogDebug(
            "Requesting advert path for contact pubkey {PublicKey} on device {DeviceId}",
            publicKey,
            deviceId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_ADVERT_PATH, payload);

        MeshCoreResponseCode responseCode = response.GetResponseCode();

        switch (responseCode)
        {
            case MeshCoreResponseCode.RESP_CODE_ADVERT_PATH:
                {
                    try
                    {
                        var advertPath = AdvertPathInfoSerialization.Instance.Deserialize(response.Payload);

                        _logger.LogDebug(
                            "Received advert path of length {PathLength} for contact pubkey {PublicKey} on device {DeviceId}",
                            advertPath.Path.Length,
                            publicKey,
                            deviceId);

                        return advertPath;
                    }
                    catch (Exception ex)
                    {
                        var protoEx = new ProtocolException(
                            (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH,
                            0x01,
                            $"Failed to parse advert path payload: {ex.Message}");

                        _logger.LogProtocolError(protoEx, (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, 0x01);
                        MeshCoreSdkEventSource.Log.ProtocolError(
                            (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH,
                            0x01,
                            protoEx.Message);

                        throw protoEx;
                    }
                }

            case MeshCoreResponseCode.RESP_CODE_ERR:
                {
                    var status = response.GetStatus();
                    var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                    if (statusByte == 0x02) // ERR_CODE_NOT_FOUND
                    {
                        _logger.LogDebug(
                            "No advert path found for contact pubkey {PublicKey} on device {DeviceId}",
                            publicKey,
                            deviceId);
                        return null;
                    }

                    var errorMessage = status switch
                    {
                        MeshCoreStatus.InvalidCommand => "Get advert path command not supported by this device firmware",
                        MeshCoreStatus.InvalidParameter => "Invalid public key supplied for advert path query",
                        MeshCoreStatus.DeviceError => "Device is in an error state and cannot provide advert path information",
                        MeshCoreStatus.NetworkError => "Network error occurred while retrieving advert path",
                        MeshCoreStatus.TimeoutError => "Timeout occurred while retrieving advert path",
                        MeshCoreStatus.UnknownError => "Unknown error occurred while retrieving advert path",
                        _ => $"Failed to get advert path (status: 0x{statusByte:X2})"
                    };

                    var ex = new ProtocolException((byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, statusByte, errorMessage);
                    _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, statusByte);
                    MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, statusByte, ex.Message);
                    throw ex;
                }

            default:
                var unexpected = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH,
                    0x01,
                    $"Unexpected response code {responseCode} for advert path request.");
                _logger.LogProtocolError(unexpected, (byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, 0x01);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_ADVERT_PATH, 0x01, unexpected.Message);
                throw unexpected;
        }
    }

    /// <summary>
    /// Sets the node's advertised device name on the MeshCore device using
    /// the <c>CMD_SET_ADVERT_NAME</c> firmware command.
    /// </summary>
    /// <param name="deviceName">The new device name to advertise.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="deviceName"/> is null, empty, or consists only of whitespace.
    /// </exception>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error response or an unexpected response code.
    /// </exception>
    public async Task SetAdvertNameAsync(string deviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            throw new ArgumentException("Device name cannot be null or empty", nameof(deviceName));
        }

        // Firmware stores _prefs.node_name in a fixed-size buffer with a null terminator.
        // CMD_SET_ADVERT_NAME expects:
        //   [CMD_SET_ADVERT_NAME][name bytes...]
        // and enforces a maximum of sizeof(_prefs.node_name) - 1 characters.
        // We conservatively clamp to 31 bytes (common for node_name in firmware).
        const int maxNameBytes = 31;

        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SetAdvertNameAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, newName='{DeviceName}'",
            operationName,
            deviceId,
            deviceName);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var nameBytes = Encoding.UTF8.GetBytes(deviceName);

            if (nameBytes.Length > maxNameBytes)
            {
                throw new ArgumentException(
                    $"Device name cannot exceed {maxNameBytes} bytes when encoded as UTF-8.",
                    nameof(deviceName));
            }

            var response = await _transport.SendCommandAsync(
                MeshCoreCommand.CMD_SET_ADVERT_NAME,
                nameBytes,
                cancellationToken);

            var responseCode = response.GetResponseCode();

            if (responseCode == MeshCoreResponseCode.RESP_CODE_OK)
            {
                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug(
                    "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. NewName='{DeviceName}'",
                    operationName,
                    deviceId,
                    (long)duration,
                    deviceName);
                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
                return;
            }

            if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand => "Set advert name command not supported by this device firmware.",
                    MeshCoreStatus.InvalidParameter => "Invalid advert name supplied to device.",
                    MeshCoreStatus.DeviceError => "Device is in an error state and cannot update advert name.",
                    MeshCoreStatus.NetworkError => "Network error occurred while setting advert name.",
                    MeshCoreStatus.TimeoutError => "Timeout occurred while setting advert name.",
                    MeshCoreStatus.UnknownError => "Unknown error occurred while setting advert name.",
                    _ => $"Failed to set advert name (status: 0x{statusByte:X2})."
                };

                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME,
                    statusByte,
                    errorMessage);

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError(
                    (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME,
                    statusByte,
                    ex.Message);

                throw ex;
            }

            var unexpected = new ProtocolException(
                (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME,
                0x01,
                $"Unexpected response code {responseCode} for set advert name request.");

            _logger.LogProtocolError(unexpected, (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME, 0x01);

            MeshCoreSdkEventSource.Log.ProtocolError(
                (byte)MeshCoreCommand.CMD_SET_ADVERT_NAME,
                0x01,
                unexpected.Message);

            throw unexpected;
        }
        catch (Exception ex) when (ex is not ProtocolException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Discovers the network path to a specific contact using the MeshCore path discovery protocol.
    /// This sends a CMD_SEND_PATH_DISCOVERY_REQ command and waits for the PATH_RESPONSE event.
    /// </summary>
    /// <param name="contact">Contact of Repeater</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the discovered path information including inbound and outbound paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when publicKey is null</exception>
    /// <exception cref="ArgumentException">Thrown when publicKey is not exactly 32 bytes long</exception>
    /// <exception cref="ProtocolException">Thrown when the device returns an error or doesn't support path discovery</exception>
    /// <exception cref="TimeoutException">Thrown when the path discovery operation times out</exception>
    public async Task<PathDiscoveryResult?> TryDiscoverPathAsync(Contact contact, CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.SendCommandAsync<PathDiscoveryResult>(
                MeshCoreCommand.CMD_SEND_PATH_DISCOVERY_REQ,
                payloadFunc: () =>
                {
                    var payload = new byte[1 + contact.PublicKey.Value.Length];
                    payload[0] = 0x00; // Reserved byte
                    Buffer.BlockCopy(contact.PublicKey.Value, 0, payload, 1, contact.PublicKey.Value.Length);
                    return payload;
                },
                frameFunc: (frame, taskCompletionSource) =>
                {
                    if (!frame.IsOutbound) return;

                    var responseCode = frame.GetResponseCode();

                    // PATH_DISCOVERY_RESPONSE (0x8D) arrives as a direct top-level frame
                    // from the firmware, not wrapped inside LOG_RX_DATA.
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_PATH_RESPONSE)
                    {
                        if (PathDiscoveryResultSerialization.Instance.TryDeserialize(frame.Payload, out var result))
                        {
                            taskCompletionSource.TrySetResult(result);
                        }
                    }
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "Path discovery timed out for contact {ContactName} ({PublicKey}) on device {DeviceId}. " +
                "The target node may be offline or out of range.",
                contact.Name, contact.PublicKey, _transport.ConnectionId ?? "Unknown");
            return null;
        }
    }

    /// <summary>
    /// Attempts to trace the network path to the specified repeater or room server contact
    /// using the <c>CMD_SEND_TRACE_PATH</c> command with a single-hop path.
    /// </summary>
    /// <remarks>
    /// This sends a trace packet through the specified repeater and waits for
    /// <c>PUSH_CODE_TRACE_DATA (0x89)</c> containing hop-by-hop SNR measurements.
    /// The wire format for CMD_SEND_TRACE_PATH payload is:
    /// <code>
    /// [tag: uint32 LE][auth_code: uint32 LE][flags: uint8][path_bytes...]
    /// </code>
    /// For a single-hop trace, path_bytes is the first byte of the contact's public key.
    /// </remarks>
    /// <param name="contact">The repeater or room server contact to trace to. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the trace operation.</param>
    /// <returns>A PathDiscoveryResult if the trace succeeds; otherwise, null.</returns>
    public async Task<PathDiscoveryResult?> TrySingleHopTraceAsync(
        Contact contact,
        CancellationToken cancellationToken = default)
    {
        if (contact.NodeType != NodeType.Repeater && contact.NodeType != NodeType.RoomServer)
        {
            _logger.LogWarning("Single hop trace not applicable to client contacts, use TryDiscoverPathAsync instead");
            return null;
        }

        try
        {
            return await SendAsync(
                MeshCoreCommand.CMD_SEND_TRACE_PATH,
                MeshCoreResponseCode.PUSH_CODE_TRACE_DATA,
                payloadFunc: () =>
                {
                    var traceParams = new SendTracePathParams
                    {
                        Tag = (uint)Random.Shared.Next(1, int.MaxValue),
                        AuthCode = 0,
                        Flags = 0x00,
                        Path = new[] { contact.PublicKey.Value[0] } // single-hop: first byte of pubkey
                    };

                    return SendTracePathParamsSerialization.Instance.Serialize(traceParams);
                },
                PathDiscoveryResultSerialization.Instance,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "Trace timed out for contact {ContactName} ({PublicKey}) on device {DeviceId}. " +
                "The target node may be offline or out of range.",
                contact.Name, contact.PublicKey, _transport.ConnectionId ?? "Unknown");
            return null;
        }
    }

    /// <summary>
    /// Attempts to retrieve the list of neighboring contacts from the specified repeater
    /// by issuing a binary neighbours request via <c>CMD_SEND_BINARY_REQ</c> with
    /// <c>BinaryReqType.NEIGHBOURS (0x06)</c>.
    /// </summary>
    /// <param name="repeaterContact">The contact information for the repeater from which to request the neighbor list. Cannot be null.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a NeighborList if the neighbor list
    /// is successfully retrieved and deserialized; otherwise, null.</returns>
    public async Task<NeighborList?> TryGetNeighborsAsync(
        Contact repeaterContact,
        CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(TryGetNeighborsAsync);

        try
        {
            // CMD_SEND_BINARY_REQ (0x32) payload:
            //   [pub_key(32)][binary_req_type(1)][request_data...]
            // BinaryReqType.NEIGHBOURS = 0x06
            // Request data format (from Python req_neighbours_async):
            //   [version(1)][count(1)][offset(2 LE)][order_by(1)][pubkey_prefix_length(1)][random_tag(4 LE)]
            const byte BINARY_REQ_TYPE_NEIGHBOURS = 0x06;
            const byte PUBKEY_PREFIX_LENGTH = 4;
            byte count = 255;
            ushort offset = 0;
            byte orderBy = 0;
            byte version = 0;
            var randomTag = (uint)Random.Shared.Next(1, int.MaxValue);

            var pubkey = repeaterContact.PublicKey.Value;
            var payload = new byte[pubkey.Length + 1 + 10]; // pubkey + req_type + request_data
            Buffer.BlockCopy(pubkey, 0, payload, 0, pubkey.Length);
            var pos = pubkey.Length;
            payload[pos++] = BINARY_REQ_TYPE_NEIGHBOURS;
            payload[pos++] = version;
            payload[pos++] = count;
            BitConverter.GetBytes(offset).CopyTo(payload, pos); pos += 2;
            payload[pos++] = orderBy;
            payload[pos++] = PUBKEY_PREFIX_LENGTH;
            BitConverter.GetBytes(randomTag).CopyTo(payload, pos);

            _logger.LogDebug(
                "Sending CMD_SEND_BINARY_REQ (NEIGHBOURS) to {ContactName} on device {DeviceId}",
                repeaterContact.Name,
                deviceId);

            return await this.SendCommandAsync<NeighborList>(
                MeshCoreCommand.CMD_SEND_BINARY_REQ,
                payloadFunc: () => payload,
                frameFunc: (frame, taskCompletionSource) =>
                {
                    if (!frame.IsOutbound) return;

                    var responseCode = frame.GetResponseCode();

                    // PUSH_CODE_BINARY_RESPONSE (0x8C) carries the neighbours data
                    // Wire format: [0x8C][reserved][tag(4)][response_data...]
                    if (responseCode == MeshCoreResponseCode.PUSH_CODE_BINARY_RESPONSE)
                    {
                        if (frame.Payload.Length < 6) return; // Need at least resp_code + reserved + tag(4)

                        // Extract response_data (after resp_code(1) + reserved(1) + tag(4))
                        var responseData = new byte[frame.Payload.Length - 6];
                        Buffer.BlockCopy(frame.Payload, 6, responseData, 0, responseData.Length);

                        if (NeighborListSerialization.Instance.TryDeserialize(responseData, PUBKEY_PREFIX_LENGTH, out var result))
                        {
                            taskCompletionSource.TrySetResult(result);
                        }
                    }
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "Neighbours request timed out for contact {ContactName} ({PublicKey}) on device {DeviceId}. " +
                "The target node may be offline or out of range.",
                repeaterContact.Name, repeaterContact.PublicKey, deviceId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            return null;
        }
    }

    /// <summary>
    /// Attempts to retrieve a high-level status snapshot from the specified repeater or room server
    /// contact by issuing a binary status request via <c>CMD_SEND_BINARY_REQ</c> with
    /// <c>BinaryReqType.STATUS (0x01)</c>.
    /// </summary>
    /// <param name="contact">
    /// The remote contact (typically a repeater or room server) from which to request status
    /// information. Cannot be <c>null</c>.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token that can be used to cancel the operation before it completes.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="StatusInfo"/> instance when the status response is successfully retrieved
    /// and deserialized; otherwise, <c>null</c> if the request fails, times out, or the payload
    /// cannot be interpreted.
    /// </returns>
    public async Task<StatusInfo?> TryRequestStatusAsync(
        Contact contact,
        CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        var operationName = nameof(TryRequestStatusAsync);

        try
        {
            // CMD_SEND_BINARY_REQ (0x32) payload:
            //   [pub_key(32)][binary_req_type(1)]
            // BinaryReqType.STATUS = 0x01
            const byte BINARY_REQ_TYPE_STATUS = 0x01;
            var payload = new byte[contact.PublicKey.Value.Length + 1];
            Buffer.BlockCopy(contact.PublicKey.Value, 0, payload, 0, contact.PublicKey.Value.Length);
            payload[contact.PublicKey.Value.Length] = BINARY_REQ_TYPE_STATUS;

            _logger.LogDebug(
                "Sending CMD_SEND_BINARY_REQ (STATUS) to {ContactName} on device {DeviceId}",
                contact.Name,
                deviceId);

            return await this.SendCommandAsync<StatusInfo>(
                MeshCoreCommand.CMD_SEND_BINARY_REQ,
                payloadFunc: () => payload,
                frameFunc: (frame, taskCompletionSource) =>
                {
                    if (!frame.IsOutbound) return;

                    var responseCode = frame.GetResponseCode();

                    // PUSH_CODE_STATUS_RESPONSE (0x87) is the direct status response
                    if (responseCode == MeshCoreResponseCode.PUSH_CODE_STATUS_RESPONSE)
                    {
                        if (StatusInfoSerialization.Instance.TryDeserialize(frame.Payload, out var result))
                        {
                            taskCompletionSource.TrySetResult(result);
                        }
                    }
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug(
                "Status request timed out for contact {ContactName} ({PublicKey}) on device {DeviceId}. " +
                "The target node may be offline or out of range.",
                contact.Name, contact.PublicKey, deviceId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            return null;
        }
    }

    #endregion

    #region Auto-Add Contacts Operations

    /// <summary>
    /// Gets the raw auto-add configuration bitmask from the device as a strongly
    /// typed set of <see cref="AutoAddConfigFlags"/>.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// the <see cref="AutoAddConfigFlags"/> value corresponding to the firmware
    /// <c>_prefs.autoadd_config</c> bitmask.
    /// </returns>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error or an unexpected response to the
    /// auto-add configuration query.
    /// </exception>
    public async Task<AutoAddConfigFlags> GetAutoAddMaskAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(GetAutoAddMaskAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_AUTOADD_CONFIG, cancellationToken: CancellationToken.None);

            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_AUTOADD_CONFIG)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG,
                    statusByte,
                    $"Unexpected response code {response.GetResponseCode()} for auto-add configuration request.");

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG, statusByte, ex.Message);

                throw ex;
            }

            if (response.Payload.Length < 2)
            {
                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG,
                    0x01,
                    "Auto-add configuration payload too short.");
                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG, 0x01);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_GET_AUTOADD_CONFIG, 0x01, ex.Message);

                throw ex;
            }

            var rawMask = response.Payload[1];
            var flags = (AutoAddConfigFlags)rawMask;

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. AutoAddMask=0x{Mask:X2}",
                operationName,
                deviceId,
                (long)duration,
                rawMask);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

            return flags;
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sets the auto-add configuration on the device using a strongly typed
    /// <see cref="AutoAddConfigFlags"/> bitmask.
    /// </summary>
    /// <param name="flags">
    /// The <see cref="AutoAddConfigFlags"/> value to apply to the firmware
    /// <c>_prefs.autoadd_config</c> field (for example, enabling Chat, Repeater,
    /// RoomServer, Sensor and OverwriteOldest behavior).
    /// </param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error or an unexpected response while applying
    /// the configuration.
    /// </exception>
    public async Task SetAutoAddMaskAsync(AutoAddConfigFlags flags, CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SetAutoAddMaskAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, mask={Mask}",
            operationName,
            deviceId,
            flags);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var rawMask = (byte)flags;
            var payload = new[] { rawMask };

            var response = await _transport.SendCommandAsync(
                MeshCoreCommand.CMD_SET_AUTOADD_CONFIG,
                payload,
                cancellationToken);

            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_SET_AUTOADD_CONFIG,
                    statusByte,
                    "Failed to set auto-add configuration bitmask.");

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_AUTOADD_CONFIG, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SET_AUTOADD_CONFIG, statusByte, ex.Message);

                throw ex;
            }

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. mask={Mask}",
                operationName,
                deviceId,
                (long)duration,
                flags);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Gets whether the device is configured to automatically add newly heard contacts
    /// to its contacts list, based on the <c>_prefs.manual_add_contacts</c> flag.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is
    /// <c>true</c> if auto-add is enabled; otherwise, <c>false</c>. The current
    /// implementation assumes auto-add is enabled while device info parsing is
    /// being stubbed.
    /// </returns>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error or an unexpected response while reading
    /// the device preferences.
    /// </exception>
    public async Task<bool> GetAutoAddEnabledAsync()
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(GetAutoAddEnabledAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });

            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                var ex = new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, "Failed to read device preferences for auto-add state");

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_DEVICE_QUERY, statusByte, ex.Message);

                throw ex;
            }

            // TODO: Parse manual_add_contacts from RESP_CODE_DEVICE_INFO payload when the
            // device info serializer understands the full firmware layout.
            var isAutoAddEnabled = true;

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. IsAutoAddEnabled={IsAutoAddEnabled}",
                operationName,
                deviceId,
                (long)duration,
                isAutoAddEnabled);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

            return isAutoAddEnabled;
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sets whether the device should automatically add newly heard contacts to its
    /// contacts list, by updating the <c>_prefs.manual_add_contacts</c> flag.
    /// </summary>
    /// <param name="enableAutoAdd">
    /// When <c>true</c>, configures the device to enable auto-add (bit 0 of
    /// <c>manual_add_contacts</c> cleared). When <c>false</c>, configures the device
    /// for manual mode (bit 0 set).
    /// </param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ProtocolException">
    /// Thrown when the device returns an error or an unexpected response while applying
    /// the configuration.
    /// </exception>
    public async Task SetAutoAddEnabledAsync(bool enableAutoAdd, CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SetAutoAddEnabledAsync);
        var startTime = DateTimeOffset.UtcNow;

        _logger.LogDebug(
            "Starting operation: {OperationName} for device: {DeviceId}, enableAutoAdd={EnableAutoAdd}",
            operationName,
            deviceId,
            enableAutoAdd);
        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            byte manualAddContacts = enableAutoAdd ? (byte)0x00 : (byte)0x01;
            var payload = new[] { manualAddContacts };

            var response = await _transport.SendCommandAsync(
                MeshCoreCommand.CMD_SET_OTHER_PARAMS,
                payload,
                cancellationToken);

            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var ex = new ProtocolException(
                    (byte)MeshCoreCommand.CMD_SET_OTHER_PARAMS,
                    statusByte,
                    "Failed to apply manual/auto-add mode configuration.");

                _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SET_OTHER_PARAMS, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError(
                    (byte)MeshCoreCommand.CMD_SET_OTHER_PARAMS,
                    statusByte,
                    ex.Message);

                throw ex;
            }

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            _logger.LogDebug(
                "Operation completed: {OperationName} for device: {DeviceId} in {Duration}ms. enableAutoAdd={EnableAutoAdd}",
                operationName,
                deviceId,
                (long)duration,
                enableAutoAdd);
            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);
        }
        catch (Exception ex) when (!(ex is ProtocolException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Waits asynchronously for a frame with the specified response code to be received, using a dynamic timeout
    /// provided by the caller.
    /// </summary>
    /// <remarks>The wait operation will complete when a matching frame is received or when the timeout
    /// elapses. If the operation is canceled via the provided cancellation token or the timeout, the returned task will
    /// be canceled. The method unsubscribes from frame events after completion to prevent memory leaks.</remarks>
    /// <param name="funcAsync">A delegate that asynchronously computes the timeout duration for the wait operation. The delegate receives a
    /// cancellation token and returns a task that yields the timeout as a TimeSpan.</param>
    /// <param name="responseCode">The response code to match against incoming frames. Only frames with this response code will complete the wait
    /// operation.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation before completion.</param>
    /// <returns>A task that represents the asynchronous wait operation. The task result is the received MeshCoreFrame that
    /// matches the specified response code.</returns>
    private async Task<MeshCoreFrame> WaitForEventAsync(
        Func<CancellationToken, Task<TimeSpan>> funcAsync,
        MeshCoreResponseCode responseCode,
        CancellationToken cancellationToken)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(WaitForEventAsync);
        var startTime = DateTimeOffset.UtcNow;

        TaskCompletionSource<MeshCoreFrame> taskCompleteionSource = new TaskCompletionSource<MeshCoreFrame>();

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            if (!frame.IsOutbound)
            {
                return;
            }

            var frameResponseCode = frame.GetResponseCode();

            if (frameResponseCode == null)
            {
                _logger.LogWarning("Received frame with null response code on {DeviceId}. Frame: {Frame}", deviceId, frame);
            }

            // Check if this is a PATH_RESPONSE frame
            // You'll need to add RESP_CODE_PATH_RESPONSE to MeshCoreResponseCode enum
            if (frameResponseCode == responseCode)
            {
                taskCompleteionSource.TrySetResult(frame);
            }
        }
        ;

        // Subscribe to frame events
        _transport.FrameReceived += OnFrameReceived;

        try
        {
            TimeSpan timeout = await funcAsync(cancellationToken);

            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                using (CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cancellationTokenSource.Token))
                {
                    using (linkedCancellationTokenSource.Token.Register(() => taskCompleteionSource.TrySetCanceled()))
                    {
                        MeshCoreFrame frame = await taskCompleteionSource.Task;

                        var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogDebug("Frame Detected on {DeviceId} in {Duration}ms. Path: {PathDescription}",
                            deviceId, (long)duration, frame);

                        MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                        return frame;
                    }
                }
            }
        }
        finally
        {
            // Always unsubscribe to prevent memory leaks
            _transport.FrameReceived -= OnFrameReceived;
        }
    }

    /// <summary>
    /// Waits asynchronously for an outbound frame with the specified response code to be received after executing the
    /// provided asynchronous operation.
    /// </summary>
    /// <remarks>The method subscribes to frame events and waits for a matching outbound frame after executing
    /// the provided operation. The event subscription is removed when the operation completes or is canceled to prevent
    /// memory leaks. If cancellation is requested before a matching frame is received, the returned task will be
    /// canceled.</remarks>
    /// <param name="funcAsync">A delegate representing the asynchronous operation to execute before waiting for the frame. The operation
    /// receives a cancellation token and should complete before the frame is awaited.</param>
    /// <param name="responseCode">The response code to match against received outbound frames. Only frames with this response code will be
    /// considered.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the wait operation. If cancellation is requested, the task will be canceled.</param>
    /// <returns>A task that represents the asynchronous wait operation. The task result contains the outbound frame matching the
    /// specified response code.</returns>
    private async Task<MeshCoreFrame> WaitForEventAsync(
        Func<CancellationToken, Task> funcAsync,
        MeshCoreResponseCode responseCode,
        CancellationToken cancellationToken)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(WaitForEventAsync);
        var startTime = DateTimeOffset.UtcNow;

        TaskCompletionSource<MeshCoreFrame> taskCompleteionSource = new TaskCompletionSource<MeshCoreFrame>();

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            if (!frame.IsOutbound)
            {
                return;
            }

            var frameResponseCode = frame.GetResponseCode();

            if (frameResponseCode == responseCode)
            {
                taskCompleteionSource.TrySetResult(frame);
            }
        }
        ;

        // Subscribe to frame events
        _transport.FrameReceived += OnFrameReceived;

        try
        {
            await funcAsync(cancellationToken);

            using (cancellationToken.Register(() => taskCompleteionSource.TrySetCanceled()))
            {
                MeshCoreFrame frame = await taskCompleteionSource.Task;

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogDebug("Frame Detected on {DeviceId} in {Duration}ms. Path: {PathDescription}",
                    deviceId, (long)duration, frame);

                MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                return frame;
            }
        }
        finally
        {
            // Always unsubscribe to prevent memory leaks
            _transport.FrameReceived -= OnFrameReceived;
        }
    }

    /// <summary>
    /// Sends a text command to a remote repeater/room server node and waits for a binary data response.
    /// This is used for remote CLI operations like requesting neighbor lists, status, telemetry, etc.
    /// </summary>
    /// <param name="contact">The contact (repeater/room server) to send the command to</param>
    /// <param name="command">The CLI command text to execute on the remote node</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the binary response payload from the remote node.</returns>
    /// <exception cref="ArgumentNullException">Thrown when contact or command is null</exception>
    /// <exception cref="ArgumentException">Thrown when command is empty</exception>
    /// <exception cref="ProtocolException">Thrown when the device returns an error</exception>
    /// <exception cref="TimeoutException">Thrown when the operation times out waiting for response</exception>
    private async Task<byte[]?> SendRemoteCommandAsync(
        Contact contact,
        string command,
        CancellationToken cancellationToken = default)
    {
        if (contact == null)
        {
            throw new ArgumentNullException(nameof(contact));
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException("Command cannot be null or empty", nameof(command));
        }

        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SendRemoteCommandAsync);
        DateTime startTime = DateTime.UtcNow;

        try
        {
            // Create a command
            var commandPayload = Command.Create(contact.PublicKey, command);

            // Serialize it
            var payloadArray = CommandSerialization.Instance.Serialize(commandPayload);

            _logger.LogDebug(
                "Sending CMD_SEND_TXT_MSG with txt_type=0x01 (command) to {ContactName}, command: {Command}",
                contact.Name,
                command);

            // Use WaitForEventAsync pattern to send command and wait for RESP_CODE_CONTACT_MSG_RECV
            var frame = await WaitForEventAsync(async (ct) =>
            {
                var response = await _transport.SendCommandAsync(
                    MeshCoreCommand.CMD_SEND_TXT_MSG,
                    payloadArray,
                    ct);

                var responseCode = response.GetResponseCode();

                switch (responseCode)
                {
                    case MeshCoreResponseCode.RESP_CODE_SENT:
                        {
                            // Byte Offset  | Field                | Size
                            // ------------ | ---------------------| ------
                            // 0            | RESP_CODE(0x06)      | 1
                            // 1            | txt_type             | 1
                            // 2 - 5        | expected_ack         | 4 (little-endian uint32)
                            // 6 - 9        | suggested_timeout    | 4 (little-endian uint32, in milliseconds)

                            _logger.LogDebug(
                                "RESP_CODE_SENT payload hex: {PayloadHex}, length: {Length}",
                                Convert.ToHexString(response.Payload),
                                response.Payload.Length);

                            // Extract suggested timeout from response if available, or use provided/default timeout
                            TimeSpan effectiveTimeout = TimeSpan.FromSeconds(30);

                            if (response.Payload.Length >= 10)
                            {
                                try
                                {
                                    var suggestedTimeoutMs = BitConverter.ToUInt32(response.Payload, 6);
                                    if (suggestedTimeoutMs > 0)
                                    {
                                        effectiveTimeout = TimeSpan.FromMilliseconds(suggestedTimeoutMs * 1.2);
                                        _logger.LogDebug(
                                            "Using device suggested timeout: {TimeoutMs}ms for remote command response",
                                            effectiveTimeout.TotalMilliseconds);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Could not parse suggested timeout from command response, using default");
                                }
                            }

                            _logger.LogDebug(
                                "Remote command sent successfully to {ContactName}, waiting for RESP_CODE_CONTACT_MSG_RECV with timeout: {Timeout}ms",
                                contact.Name,
                                effectiveTimeout.TotalMilliseconds);

                            return effectiveTimeout;
                        }

                    case MeshCoreResponseCode.RESP_CODE_ERR:
                        {
                            var status = response.GetStatus();
                            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                            var errorMessage = status switch
                            {
                                MeshCoreStatus.InvalidCommand => "Send text message command not supported by this device firmware",
                                MeshCoreStatus.InvalidParameter => "Invalid contact or command parameters",
                                MeshCoreStatus.DeviceError => "Device is in an error state and cannot send command",
                                MeshCoreStatus.NetworkError => "Network error occurred while sending command",
                                MeshCoreStatus.TimeoutError => "Command sending timed out",
                                _ => $"Failed to send remote command (status: 0x{statusByte:X2})"
                            };

                            var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, statusByte, errorMessage);
                            _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SEND_TXT_MSG, statusByte);
                            MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, statusByte, ex.Message);
                            throw ex;
                        }

                    default:
                        {
                            var errorMessage = $"Unexpected response code {responseCode} for remote command. Expected RESP_CODE_SENT ({(byte)MeshCoreResponseCode.RESP_CODE_SENT:X2}).";
                            var ex = new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 0x01, errorMessage);
                            _logger.LogProtocolError(ex, (byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 0x01);
                            MeshCoreSdkEventSource.Log.ProtocolError((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 0x01, ex.Message);
                            throw ex;
                        }
                }

            }, MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV, cancellationToken);

            if (!RemoteCommandResponseFrameSerialization.Instance.TryDeserialize(frame.Payload, out var remoteCommandResponseFrame) ||
                remoteCommandResponseFrame == null)
            {
                return null;
            }

            _logger.LogDebug(
                "Received RESP_CODE_CONTACT_MSG_RECV with txt_type=0x{TxtType:X2} from {ContactName}, command response: {CommandResponse}",
                remoteCommandResponseFrame.TextType,
                contact.Name,
                remoteCommandResponseFrame.ResponseCode);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;

            MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

            return remoteCommandResponseFrame.Payload;
        }
        catch (Exception ex) when (!(ex is ProtocolException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

    /// <summary>
    /// Sends a command to the device and waits for an asynchronous frame response matching custom criteria.
    /// This is a generic helper for commands that trigger asynchronous responses (like path discovery, trace, etc.)
    /// </summary>
    /// <typeparam name="T">The type of result to return after parsing the matching frame</typeparam>
    /// <param name="command">The MeshCore command to send</param>
    /// <param name="payloadFunc">Function that generates the command payload bytes</param>
    /// <param name="frameFunc">Function that processes incoming frames and completes the TaskCompletionSource when a match is found</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The parsed result of type T from the matching frame</returns>
    /// <exception cref="ProtocolException">Thrown when the device returns an error response</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled</exception>
    private async Task<T?> SendCommandAsync<T>(
        MeshCoreCommand command,
        Func<byte[]> payloadFunc,
        Action<MeshCoreFrame, TaskCompletionSource<T?>> frameFunc,
        CancellationToken cancellationToken)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SendCommandAsync);
        var startTime = DateTimeOffset.UtcNow;

        TaskCompletionSource<T?> taskCompletionSource = new TaskCompletionSource<T?>();

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            // Let the caller's custom logic decide if this frame matches
            frameFunc(frame, taskCompletionSource);
        }

        // Subscribe to frame events BEFORE sending command
        _transport.FrameReceived += OnFrameReceived;

        try
        {
            // Generate and send the command payload
            var payload = payloadFunc();

            _logger.LogCommandSending((byte)command, deviceId);
            _logger.LogDebug("Command payload: {Payload}", Convert.ToHexString(payload));

            var response = await _transport.SendCommandAsync(
                command,
                payload,
                cancellationToken);

            var responseCode = response.GetResponseCode();

            // Check for immediate errors in the command response
            if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                var errorMessage = status switch
                {
                    MeshCoreStatus.InvalidCommand => $"Command {command} not supported by device firmware",
                    MeshCoreStatus.InvalidParameter => $"Invalid parameters for command {command}",
                    MeshCoreStatus.DeviceError => "Device is in an error state",
                    MeshCoreStatus.NetworkError => "Network error occurred",
                    MeshCoreStatus.TimeoutError => "Command timed out",
                    _ => $"Command failed with status: 0x{statusByte:X2}"
                };

                var ex = new ProtocolException((byte)command, statusByte, errorMessage);
                _logger.LogProtocolError(ex, (byte)command, statusByte);
                MeshCoreSdkEventSource.Log.ProtocolError((byte)command, statusByte, ex.Message);
                throw ex;
            }

            // Wait for the asynchronous response frame (via frameFunc callback)
            // Extract suggested timeout from RESP_CODE_SENT if available
            TimeSpan effectiveTimeout = TimeSpan.FromSeconds(30); // Default fallback
            if (responseCode == MeshCoreResponseCode.RESP_CODE_SENT && response.Payload.Length >= 10)
            {
                try
                {
                    var suggestedTimeoutMs = BitConverter.ToUInt32(response.Payload, 6) * 1.6;
                    if (suggestedTimeoutMs > 0)
                    {
                        effectiveTimeout = TimeSpan.FromMilliseconds(suggestedTimeoutMs * 1.2);
                        _logger.LogDebug(
                            "Using device suggested timeout: {TimeoutMs}ms for async response to {Command}",
                            effectiveTimeout.TotalMilliseconds, command);
                    }
                }
                catch (Exception timeoutEx)
                {
                    _logger.LogDebug(timeoutEx, "Could not parse suggested timeout from command response, using default");
                }
            }

            using (var timeoutCts = new CancellationTokenSource(effectiveTimeout))
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
                {
                    using (linkedCts.Token.Register(() => taskCompletionSource.TrySetCanceled()))
                    {
                        var result = await taskCompletionSource.Task;

                        var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                        _logger.LogDebug("Command {Command} completed for {DeviceId} in {Duration}ms",
                            command, deviceId, (long)duration);
                        MeshCoreSdkEventSource.Log.OperationCompleted(operationName, deviceId, (long)duration);

                        return result;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not ProtocolException and not OperationCanceledException)
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
        finally
        {
            // Always unsubscribe to prevent memory leaks
            _transport.FrameReceived -= OnFrameReceived;
        }
    }

    /// <summary>
    /// Sends a MeshCore command asynchronously, waits for a response event, and deserializes the resulting payload to
    /// the specified type.
    /// </summary>
    /// <remarks>If the device returns an error status, a ProtocolException is thrown with details about the
    /// failure. The method uses a device-suggested timeout if available, otherwise defaults to 30 seconds. Unexpected
    /// errors are logged and rethrown. The payload function should generate a valid payload for the specified command
    /// to avoid protocol errors.</remarks>
    /// <typeparam name="T">The type to which the response payload will be deserialized.</typeparam>
    /// <param name="meshCoreCommand">The MeshCore command to send to the device.</param>
    /// <param name="meshCoreResponseCode">The expected response code indicating the completion of the operation.</param>
    /// <param name="payloadFunc">A function that generates the payload bytes to be sent with the command. The payload must conform to the
    /// requirements of the specified command.</param>
    /// <param name="binaryDeserializer">The deserializer used to convert the response payload into an instance of type T.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the deserialized response of type T, or
    /// null if deserialization fails.</returns>
    private async Task<T?> SendAsync<T>(
        MeshCoreCommand meshCoreCommand,
        MeshCoreResponseCode meshCoreResponseCode,
        Func<byte[]> payloadFunc,
        IBinaryDeserializer<T> binaryDeserializer,
        CancellationToken cancellationToken = default)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";
        const string operationName = nameof(SendAsync);
        var startTime = DateTimeOffset.UtcNow;

        MeshCoreSdkEventSource.Log.OperationStarted(operationName, deviceId);

        try
        {
            // CMD_SEND_TRACE_PATH expects [reserved_byte][pub_key(32)]
            var payload = payloadFunc();
            _logger.LogDebug("{Payload}", Convert.ToHexString(payload));

            var frame = await WaitForEventAsync(async (cancellationToken) =>
            {
                var response = await _transport.SendCommandAsync(
                    meshCoreCommand,
                    payload,
                    cancellationToken);

                var responseCode = response.GetResponseCode();
                if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    var status = response.GetStatus();
                    var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;

                    if (status == MeshCoreStatus.InvalidCommand)
                    {
                        var investigationMessage = $"CMD_SEND_TRACE_PATH returned InvalidCommand (status 0x{statusByte:X2}). " +
                            $"This indicates a calling pattern issue requiring investigation:\n" +
                            $"  • Payload sent: {Convert.ToHexString(payload)}\n" +
                            $"  • Payload length: {payload.Length} bytes\n" +
                            $"This is typically an implementation issue, not a firmware limitation.";

                        _logger.LogError(investigationMessage);

                        var ex = new ProtocolException((byte)meshCoreCommand, statusByte, investigationMessage);
                        _logger.LogProtocolError(ex, (byte)meshCoreCommand, statusByte);
                        MeshCoreSdkEventSource.Log.ProtocolError((byte)meshCoreCommand, statusByte, ex.Message);
                        throw ex;
                    }

                    var errorMessage = status switch
                    {
                        MeshCoreStatus.InvalidParameter => "Invalid public key supplied for path discovery",
                        MeshCoreStatus.DeviceError => "Device is in an error state and cannot perform path discovery",
                        MeshCoreStatus.NetworkError => "Network error occurred during path discovery",
                        MeshCoreStatus.TimeoutError => "Device timeout during path discovery initiation",
                        _ => $"Failed to initiate path discovery (status: 0x{statusByte:X2})"
                    };

                    var ex2 = new ProtocolException((byte)meshCoreCommand, statusByte, errorMessage);
                    _logger.LogProtocolError(ex2, (byte)meshCoreCommand, statusByte);
                    MeshCoreSdkEventSource.Log.ProtocolError((byte)meshCoreCommand, statusByte, ex2.Message);
                    throw ex2;
                }

                // Extract suggested timeout from initial response (if available)
                TimeSpan effectiveTimeout = TimeSpan.FromSeconds(30); // Default fallback
                if (response.Payload.Length >= 10)
                {
                    try
                    {
                        var suggestedTimeoutMs = BitConverter.ToUInt32(response.Payload, 6) * 1.6;
                        if (suggestedTimeoutMs > 0 && suggestedTimeoutMs < 60000)
                        {
                            effectiveTimeout = TimeSpan.FromMilliseconds(suggestedTimeoutMs);
                            _logger.LogDebug("Using device suggested timeout: {TimeoutMs}ms for path discovery", effectiveTimeout.TotalMilliseconds);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not parse suggested timeout from path discovery response, using default");
                    }
                }

                _logger.LogDebug("Path discovery initiated successfully, waiting for PATH_RESPONSE event with timeout: {Timeout}ms",
                    effectiveTimeout.TotalMilliseconds);

                return effectiveTimeout;

            }, meshCoreResponseCode, cancellationToken);

            binaryDeserializer.TryDeserialize(frame.Payload, out T? result);

            return result;
        }
        catch (Exception ex) when (!(ex is ProtocolException) && !(ex is TimeoutException) && !(ex is OperationCanceledException))
        {
            _logger.LogUnexpectedError(ex, operationName);
            MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, operationName);
            throw;
        }
    }

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

    private void OnTransportError(object? sender, Exception ex)
    {
        _logger.LogUnexpectedError(ex, "Transport");
        MeshCoreSdkEventSource.Log.UnexpectedError(ex.Message, "Transport");
        ErrorOccurred?.Invoke(this, ex);
    }

    private async Task<uint> ParseContactsSequenceAsync(CancellationToken cancellationToken)
    {
        var deviceId = _transport.ConnectionId ?? "Unknown";

        _logger.LogDebug("Parsing contacts sequence for device {DeviceId}", deviceId);

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await Task.Delay(100);

                var nextResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE, cancellationToken: cancellationToken);
                var responseCode = nextResponse.GetResponseCode();
                var status = nextResponse.GetStatus();

                MeshCoreSdkEventSource.Log.SyncNextMessageResponse(
                    ((byte)responseCode).ToString("X2"),
                    status?.ToString() ?? "null",
                    nextResponse.Payload.Length);

                if (responseCode == MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS)
                {
                    if (nextResponse.Payload.Length >= 5)
                    {
                        var lastmod = BitConverter.ToUInt32(nextResponse.Payload, 1);
                        _logger.LogDebug("End of contacts for device {DeviceId}, lastmod cursor={Lastmod}", deviceId, lastmod);
                        return lastmod;
                    }

                    _logger.LogDebug("End of contacts marker received without lastmod cursor for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
                {
                    try
                    {
                        Contact? contact;
                        if (TryDeserializeContact(nextResponse.Payload, out contact) && (contact != null))
                        {
                            MeshCoreSdkEventSource.Log.ContactParsed(contact.Name, contact.PublicKey.ToString());
                            ContactStatusChanged?.Invoke(this, contact);
                        }
                        else
                        {
                            _logger.LogWarning("Received contact response could not be parsed for device {DeviceId}", deviceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogContactParsingFailed(ex);
                        MeshCoreSdkEventSource.Log.ContactParsingFailed(ex.Message);
                    }
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                {
                    _logger.LogDebug("Device reports no more messages/contacts during contact enumeration for device {DeviceId}", deviceId);
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    _logger.LogWarning("Error response during contact enumeration for device {DeviceId}: Status={Status}.", deviceId, status?.ToString() ?? "null");
                    break;
                }
                else
                {
                    _logger.LogWarning("Unexpected response during contact enumeration: {ResponseCode} (Status: {Status}) for device {DeviceId}", responseCode, status?.ToString() ?? "null", deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during contact retrieval for device {DeviceId}", deviceId);
                throw;
            }
        } while (cancellationToken.IsCancellationRequested == false);

        _logger.LogDebug("Contact retrieval summary for device {DeviceId}.", deviceId);

        return 0;
    }

    private static bool TryDeserializeContact(byte[] data, out Contact? contact)
    {
        return ContactSerialization.Instance.TryDeserialize(data, out contact);
    }

    /// <summary>
    /// Attempts to parse channel configuration from device response data
    /// </summary>
    /// <param name="data">The raw response data from the device</param>
    /// <param name="channel">When this method returns, contains the parsed channel configuration if successful; otherwise, null</param>
    /// <returns>True if parsing succeeded; otherwise, false</returns>
    private static bool TryDeserializeChannel(byte[] data, out Channel? channel)
    {
        return ChannelSerialization.Instance.TryDeserialize(data, out channel);
    }

    /// <summary>
    /// Serializes channel configuration for sending to device
    /// </summary>
    /// <param name="config">The channel configuration to serialize</param>
    /// <returns>Serialized byte array</returns>
    private static byte[] SerializeChannel(Channel config)
    {
        return ChannelSerialization.Instance.Serialize(config);
    }

    #endregion

    /// <summary>
    /// Releases all resources used by the MeshCodeClient
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            this.Disconnect();

            var deviceId = _transport.ConnectionId ?? "Unknown";
            _logger.LogDeviceDisconnected(deviceId);
            MeshCoreSdkEventSource.Log.DeviceDisconnected(deviceId);

            _transport?.Dispose();
            _disposed = true;
        }
    }
}