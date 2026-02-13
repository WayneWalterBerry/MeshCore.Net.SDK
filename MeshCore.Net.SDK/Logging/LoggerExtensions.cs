using MeshCore.Net.SDK.Protocol;
using Microsoft.Extensions.Logging;

namespace MeshCore.Net.SDK.Logging;

/// <summary>
/// High-performance logging extensions using source generators
/// These provide structured logging with minimal allocation overhead
/// </summary>
public static partial class LoggerExtensions
{
    #region Transport Logging

    /// <summary>
    /// Logs when device discovery starts for a specific transport type
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="transportType">The type of transport being used for discovery</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Device discovery started for transport: {TransportType}")]
    public static partial void LogDeviceDiscoveryStarted(this ILogger logger, string transportType);

    /// <summary>
    /// Logs when device discovery completes with the number of devices found
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceCount">The number of devices discovered</param>
    /// <param name="transportType">The type of transport used for discovery</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Device discovery completed. Found {DeviceCount} devices for transport: {TransportType}")]
    public static partial void LogDeviceDiscoveryCompleted(this ILogger logger, int deviceCount, string transportType);

    /// <summary>
    /// Logs when a device connection attempt starts
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device being connected to</param>
    /// <param name="transportType">The type of transport being used for connection</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Connecting to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionStarted(this ILogger logger, string deviceId, string transportType);

    /// <summary>
    /// Logs when a device connection succeeds
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device that was connected</param>
    /// <param name="transportType">The type of transport used for connection</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Successfully connected to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionSucceeded(this ILogger logger, string deviceId, string transportType);

    /// <summary>
    /// Logs when a device connection fails
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred during connection</param>
    /// <param name="deviceId">The identifier of the device that failed to connect</param>
    /// <param name="transportType">The type of transport used for connection</param>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Failed to connect to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionFailed(this ILogger logger, Exception exception, string deviceId, string transportType);

    /// <summary>
    /// Logs when a device is disconnected
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device that was disconnected</param>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "Device disconnected: {DeviceId}")]
    public static partial void LogDeviceDisconnected(this ILogger logger, string deviceId);

    #endregion

    #region Protocol Logging

    /// <summary>
    /// Logs when a command is being sent to a device
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="command">The command byte being sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Sending command: {Command} to device: {DeviceId}")]
    public static partial void LogCommandSending(this ILogger logger, byte command, string deviceId);

    /// <summary>
    /// Logs when a command has been sent successfully
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="command">The command byte that was sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Command sent successfully: {Command} to device: {DeviceId}")]
    public static partial void LogCommandSent(this ILogger logger, MeshCoreCommand command, string deviceId);

    /// <summary>
    /// Logs when a response is received from a device
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="command">The command byte that the response is for</param>
    /// <param name="status">The status byte in the response</param>
    /// <param name="deviceId">The identifier of the device that sent the response</param>
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Received response for command: {Command} with status: {Status} from device: {DeviceId}")]
    public static partial void LogResponseReceived(this ILogger logger, byte command, byte status, string deviceId);

    /// <summary>
    /// Logs when a command times out
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="command">The command byte that timed out</param>
    /// <param name="deviceId">The identifier of the target device</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds</param>
    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Command timeout: {Command} for device: {DeviceId} after {TimeoutMs}ms")]
    public static partial void LogCommandTimeout(this ILogger logger, byte command, string deviceId, int timeoutMs);

    /// <summary>
    /// Logs when a protocol error occurs
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="command">The command byte that caused the error</param>
    /// <param name="status">The error status byte</param>
    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Error,
        Message = "Protocol error for command: {Command}, status: {Status}")]
    public static partial void LogProtocolError(this ILogger logger, Exception exception, byte command, byte status);

    /// <summary>
    /// Logs when a frame is successfully parsed
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="startByte">The start byte of the parsed frame</param>
    /// <param name="length">The length of the parsed frame</param>
    /// <param name="payloadLength">The length of the payload in the parsed frame</param>
    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Trace,
        Message = "Frame parsed successfully: StartByte={StartByte:X2}, Length={Length}, PayloadLength={PayloadLength}")]
    public static partial void LogFrameParsed(this ILogger logger, byte startByte, ushort length, int payloadLength);

    /// <summary>
    /// Logs when frame parsing fails
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred during parsing</param>
    /// <param name="rawDataLength">The length of the raw data that failed to parse</param>
    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Warning,
        Message = "Frame parsing failed for {RawDataLength} bytes")]
    public static partial void LogFrameParsingFailed(this ILogger logger, Exception exception, int rawDataLength);

    /// <summary>
    /// Logs raw data received from device (matches CLI's "Received data:" output)
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="rawData">The raw data as hex string</param>
    [LoggerMessage(
        EventId = 2008,
        Level = LogLevel.Debug,
        Message = "Received data: {RawData}")]
    public static partial void LogRawDataReceived(this ILogger logger, string rawData);

    /// <summary>
    /// Logs raw data being sent to device
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="rawData">The raw data as hex string</param>
    [LoggerMessage(
        EventId = 2009,
        Level = LogLevel.Debug,
        Message = "Sending raw data: {RawData}")]
    public static partial void LogRawDataSending(this ILogger logger, string rawData);

    /// <summary>
    /// Logs sending packet with hex representation
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="packet">The packet as hex string</param>
    /// <param name="length">The packet length</param>
    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Debug,
        Message = "Sending pkt {Packet} (len={Length})")]
    public static partial void LogSendingPacket(this ILogger logger, string packet, int length);

    /// <summary>
    /// Logs SYNC_NEXT_MESSAGE response type
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="responseCode">The response code description</param>
    /// <param name="status">The status value or null</param>
    /// <param name="payloadLength">The payload length</param>
    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Debug,
        Message = "SYNC_NEXT_MESSAGE response: {ResponseCode} (Status: {Status}) payloadLen={PayloadLength}")]
    public static partial void LogSyncNextMessageResponse(this ILogger logger, string responseCode, string? status, int payloadLength);

    #endregion

    #region Contact Logging

    /// <summary>
    /// Logs when contact retrieval starts for a device
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device from which contacts are being retrieved</param>
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Contact retrieval started for device: {DeviceId}")]
    public static partial void LogContactRetrievalStarted(this ILogger logger, string deviceId);

    /// <summary>
    /// Logs when contact retrieval completes
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device from which contacts were retrieved</param>
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Contact retrieval completed for device: {DeviceId}")]
    public static partial void LogContactRetrievalCompleted(this ILogger logger, string deviceId);

    /// <summary>
    /// Logs when a contact is successfully parsed
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="contactName">The name of the parsed contact</param>
    /// <param name="publicKey">The Public Key of the Contact</param>
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "Contact parsed: Name={ContactName}, PublicKey={PublicKey}")]
    public static partial void LogContactParsed(this ILogger logger, string contactName, string publicKey);

    /// <summary>
    /// Logs when contact parsing fails
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred during parsing</param>
    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "Contact parsing failed")]
    public static partial void LogContactParsingFailed(this ILogger logger, Exception exception);

    /// <summary>
    /// Logs when a new contact is added
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="contactName">The name of the contact that was added</param>
    /// <param name="nodeId">The node identifier of the contact that was added</param>
    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Contact added: {ContactName} with NodeId: {NodeId}")]
    public static partial void LogContactAdded(this ILogger logger, string contactName, string nodeId);

    /// <summary>
    /// Logs when a contact is removed
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="contactId">The identifier of the contact that was removed</param>
    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Information,
        Message = "Contact removed: {ContactId}")]
    public static partial void LogContactRemoved(this ILogger logger, string contactId);

    /// <summary>
    /// Logs detailed contact information matching CLI format
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="publicKey">The contact's public key</param>
    /// <param name="type">The contact type</param>
    /// <param name="flags">The contact flags</param>
    /// <param name="outPathLen">The outbound path length</param>
    /// <param name="name">The contact name</param>
    /// <param name="lastAdvert">Last advertisement timestamp</param>
    /// <param name="lat">Latitude</param>
    /// <param name="lon">Longitude</param>
    /// <param name="lastMod">Last modified timestamp</param>
    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Debug,
        Message = "Dispatching contact: PublicKey={PublicKey}, Type={Type}, Flags={Flags}, OutPathLen={OutPathLen}, Name='{Name}', LastAdvert={LastAdvert}, Lat={Lat}, Lon={Lon}, LastMod={LastMod}")]
    public static partial void LogDispatchingContact(
        this ILogger logger,
        string publicKey,
        int type,
        int flags,
        int outPathLen,
        string name,
        long lastAdvert,
        double lat,
        double lon,
        long lastMod);

    /// <summary>
    /// Logs contact retrieval protocol mode
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The device identifier</param>
    /// <param name="totalCount">Total contact count if known</param>
    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Debug,
        Message = "Device {DeviceId} using standard contact retrieval protocol with total count: {TotalCount}")]
    public static partial void LogContactRetrievalProtocol(this ILogger logger, string deviceId, int totalCount);

    /// <summary>
    /// Logs parsing contacts sequence
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The device identifier</param>
    [LoggerMessage(
        EventId = 3009,
        Level = LogLevel.Debug,
        Message = "Parsing contacts sequence for device {DeviceId}")]
    public static partial void LogParsingContactsSequence(this ILogger logger, string deviceId);

    #endregion

    #region Message Logging

    /// <summary>
    /// Logs when message sending begins
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="toContactId">The identifier of the contact the message is being sent to</param>
    /// <param name="contentLength">The length of the message content</param>
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Message sending started: To={ToContactId}, Length={ContentLength}")]
    public static partial void LogMessageSendingStarted(this ILogger logger, string toContactId, int contentLength);

    /// <summary>
    /// Logs when a message is sent successfully
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="toContactId">The identifier of the contact the message was sent to</param>
    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Message sent successfully: To={ToContactId}")]
    public static partial void LogMessageSent(this ILogger logger, string toContactId);

    /// <summary>
    /// Logs when message sending fails
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred during sending</param>
    /// <param name="toContactId">The identifier of the contact the message was being sent to</param>
    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Error,
        Message = "Message sending failed: To={ToContactId}")]
    public static partial void LogMessageSendingFailed(this ILogger logger, Exception exception, string toContactId);

    /// <summary>
    /// Logs when a message is received
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="fromContactId">The identifier of the contact who sent the message</param>
    /// <param name="contentLength">The length of the received message content</param>
    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Information,
        Message = "Message received: From={FromContactId}, Length={ContentLength}")]
    public static partial void LogMessageReceived(this ILogger logger, string fromContactId, int contentLength);

    /// <summary>
    /// Logs when message retrieval starts for a device
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device from which messages are being retrieved</param>
    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Information,
        Message = "Message retrieval started for device: {DeviceId}")]
    public static partial void LogMessageRetrievalStarted(this ILogger logger, string deviceId);

    /// <summary>
    /// Logs when sync queue completes
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="deviceId">The identifier of the device from which messages were retrieved</param>
    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Information,
        Message = "Message sync queue completed for device: {DeviceId}")]
    public static partial void LogSyncQueueCompleted(this ILogger logger, string deviceId);

    #endregion

    #region General Logging

    /// <summary>
    /// Logs when the SDK is successfully initialized
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="version">The version of the SDK that was initialized</param>
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "SDK initialized successfully. Version: {Version}")]
    public static partial void LogSdkInitialized(this ILogger logger, string version);

    /// <summary>
    /// Logs when an unexpected error occurs
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="source">The source or context where the error occurred</param>
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Unexpected error in {Source}")]
    public static partial void LogUnexpectedError(this ILogger logger, Exception exception, string source);

    /// <summary>
    /// Logs when an operation takes longer than expected
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <param name="operationName">The name of the operation that was slow</param>
    /// <param name="durationMs">The duration of the operation in milliseconds</param>
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Warning,
        Message = "Operation took longer than expected: {OperationName} took {DurationMs}ms")]
    public static partial void LogSlowOperation(this ILogger logger, string operationName, long durationMs);

    #endregion
}