using Microsoft.Extensions.Logging;

namespace MeshCore.Net.SDK.Logging;

/// <summary>
/// High-performance logging extensions using source generators
/// These provide structured logging with minimal allocation overhead
/// </summary>
public static partial class LoggerExtensions
{
    #region Transport Logging

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Device discovery started for transport: {TransportType}")]
    public static partial void LogDeviceDiscoveryStarted(this ILogger logger, string transportType);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Device discovery completed. Found {DeviceCount} devices for transport: {TransportType}")]
    public static partial void LogDeviceDiscoveryCompleted(this ILogger logger, int deviceCount, string transportType);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Connecting to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionStarted(this ILogger logger, string deviceId, string transportType);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Successfully connected to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionSucceeded(this ILogger logger, string deviceId, string transportType);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Error,
        Message = "Failed to connect to device: {DeviceId} via {TransportType}")]
    public static partial void LogDeviceConnectionFailed(this ILogger logger, Exception exception, string deviceId, string transportType);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "Device disconnected: {DeviceId}")]
    public static partial void LogDeviceDisconnected(this ILogger logger, string deviceId);

    #endregion

    #region Protocol Logging

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message = "Sending command: {Command} to device: {DeviceId}")]
    public static partial void LogCommandSending(this ILogger logger, byte command, string deviceId);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Debug,
        Message = "Command sent successfully: {Command} to device: {DeviceId}")]
    public static partial void LogCommandSent(this ILogger logger, byte command, string deviceId);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Debug,
        Message = "Received response for command: {Command} with status: {Status} from device: {DeviceId}")]
    public static partial void LogResponseReceived(this ILogger logger, byte command, byte status, string deviceId);

    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Warning,
        Message = "Command timeout: {Command} for device: {DeviceId} after {TimeoutMs}ms")]
    public static partial void LogCommandTimeout(this ILogger logger, byte command, string deviceId, int timeoutMs);

    [LoggerMessage(
        EventId = 2005,
        Level = LogLevel.Error,
        Message = "Protocol error for command: {Command}, status: {Status}")]
    public static partial void LogProtocolError(this ILogger logger, Exception exception, byte command, byte status);

    [LoggerMessage(
        EventId = 2006,
        Level = LogLevel.Trace,
        Message = "Frame parsed successfully: StartByte={StartByte:X2}, Length={Length}, PayloadLength={PayloadLength}")]
    public static partial void LogFrameParsed(this ILogger logger, byte startByte, ushort length, int payloadLength);

    [LoggerMessage(
        EventId = 2007,
        Level = LogLevel.Warning,
        Message = "Frame parsing failed for {RawDataLength} bytes")]
    public static partial void LogFrameParsingFailed(this ILogger logger, Exception exception, int rawDataLength);

    #endregion

    #region Contact Logging

    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Contact retrieval started for device: {DeviceId}")]
    public static partial void LogContactRetrievalStarted(this ILogger logger, string deviceId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Contact retrieval completed for device: {DeviceId}. Found {ContactCount} contacts")]
    public static partial void LogContactRetrievalCompleted(this ILogger logger, string deviceId, int contactCount);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "Contact parsed: Name={ContactName}, NodeId={NodeId}")]
    public static partial void LogContactParsed(this ILogger logger, string contactName, string nodeId);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "Contact parsing failed")]
    public static partial void LogContactParsingFailed(this ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Information,
        Message = "Contact added: {ContactName} with NodeId: {NodeId}")]
    public static partial void LogContactAdded(this ILogger logger, string contactName, string nodeId);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Information,
        Message = "Contact removed: {ContactId}")]
    public static partial void LogContactRemoved(this ILogger logger, string contactId);

    #endregion

    #region Message Logging

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Message sending started: To={ToContactId}, Length={ContentLength}")]
    public static partial void LogMessageSendingStarted(this ILogger logger, string toContactId, int contentLength);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Message sent successfully: To={ToContactId}, MessageId={MessageId}")]
    public static partial void LogMessageSent(this ILogger logger, string toContactId, string messageId);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Error,
        Message = "Message sending failed: To={ToContactId}")]
    public static partial void LogMessageSendingFailed(this ILogger logger, Exception exception, string toContactId);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Information,
        Message = "Message received: From={FromContactId}, Length={ContentLength}")]
    public static partial void LogMessageReceived(this ILogger logger, string fromContactId, int contentLength);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Information,
        Message = "Message retrieval started for device: {DeviceId}")]
    public static partial void LogMessageRetrievalStarted(this ILogger logger, string deviceId);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Information,
        Message = "Message retrieval completed for device: {DeviceId}. Found {MessageCount} messages")]
    public static partial void LogMessageRetrievalCompleted(this ILogger logger, string deviceId, int messageCount);

    #endregion

    #region General Logging

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Information,
        Message = "SDK initialized successfully. Version: {Version}")]
    public static partial void LogSdkInitialized(this ILogger logger, string version);

    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Error,
        Message = "Unexpected error in {Source}")]
    public static partial void LogUnexpectedError(this ILogger logger, Exception exception, string source);

    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Warning,
        Message = "Operation took longer than expected: {OperationName} took {DurationMs}ms")]
    public static partial void LogSlowOperation(this ILogger logger, string operationName, long durationMs);

    #endregion
}