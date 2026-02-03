using System.Diagnostics.Tracing;

namespace MeshCore.Net.SDK.Logging;

/// <summary>
/// ETW Event Source for MeshCore.Net.SDK
/// This provides structured logging via ETW which can be consumed by various tools
/// </summary>
[EventSource(Name = "MeshCore-Net-SDK")]
public sealed class MeshCoreSdkEventSource : EventSource
{
    /// <summary>
    /// Singleton instance of the event source
    /// </summary>
    public static readonly MeshCoreSdkEventSource Log = new();

    // Private constructor to enforce singleton pattern
    private MeshCoreSdkEventSource() { }

    #region Transport Events (100-199)

    [Event(100, Level = EventLevel.Informational, Message = "Device discovery started for transport: {transportType}")]
    public void DeviceDiscoveryStarted(string transportType)
    {
        WriteEvent(100, transportType);
    }

    [Event(101, Level = EventLevel.Informational, Message = "Device discovery completed. Found {deviceCount} devices for transport: {transportType}")]
    public void DeviceDiscoveryCompleted(int deviceCount, string transportType)
    {
        WriteEvent(101, deviceCount, transportType);
    }

    [Event(102, Level = EventLevel.Informational, Message = "Connecting to device: {deviceId} via {transportType}")]
    public void DeviceConnectionStarted(string deviceId, string transportType)
    {
        WriteEvent(102, deviceId ?? "Unknown", transportType ?? "Unknown");
    }

    [Event(103, Level = EventLevel.Informational, Message = "Successfully connected to device: {deviceId} via {transportType}")]
    public void DeviceConnectionSucceeded(string deviceId, string transportType)
    {
        WriteEvent(103, deviceId ?? "Unknown", transportType ?? "Unknown");
    }

    [Event(104, Level = EventLevel.Error, Message = "Failed to connect to device: {deviceId} via {transportType}. Error: {errorMessage}")]
    public void DeviceConnectionFailed(string deviceId, string transportType, string errorMessage)
    {
        WriteEvent(104, deviceId ?? "Unknown", transportType ?? "Unknown", errorMessage ?? "Unknown error");
    }

    [Event(105, Level = EventLevel.Informational, Message = "Device disconnected: {deviceId}")]
    public void DeviceDisconnected(string deviceId)
    {
        WriteEvent(105, deviceId ?? "Unknown");
    }

    #endregion

    #region Protocol Events (200-299)

    [Event(200, Level = EventLevel.Verbose, Message = "Sending command: {command} to device: {deviceId}")]
    public void CommandSending(byte command, string deviceId)
    {
        WriteEvent(200, command, deviceId ?? "Unknown");
    }

    [Event(201, Level = EventLevel.Verbose, Message = "Command sent successfully: {command} to device: {deviceId}")]
    public void CommandSent(byte command, string deviceId)
    {
        WriteEvent(201, command, deviceId ?? "Unknown");
    }

    [Event(202, Level = EventLevel.Verbose, Message = "Received response for command: {command} with status: {status} from device: {deviceId}")]
    public void ResponseReceived(byte command, byte status, string deviceId)
    {
        WriteEvent(202, command, status, deviceId ?? "Unknown");
    }

    [Event(203, Level = EventLevel.Warning, Message = "Command timeout: {command} for device: {deviceId} after {timeoutMs}ms")]
    public void CommandTimeout(byte command, string deviceId, int timeoutMs)
    {
        WriteEvent(203, command, deviceId ?? "Unknown", timeoutMs);
    }

    [Event(204, Level = EventLevel.Error, Message = "Protocol error for command: {command}, status: {status}, message: {errorMessage}")]
    public void ProtocolError(byte command, byte status, string errorMessage)
    {
        WriteEvent(204, command, status, errorMessage ?? "Unknown error");
    }

    [Event(205, Level = EventLevel.Verbose, Message = "Frame parsed successfully: StartByte={startByte}, Length={length}, PayloadLength={payloadLength}")]
    public void FrameParsed(byte startByte, ushort length, int payloadLength)
    {
        WriteEvent(205, startByte, length, payloadLength);
    }

    [Event(206, Level = EventLevel.Warning, Message = "Frame parsing failed: {errorMessage}, RawDataLength={rawDataLength}")]
    public void FrameParsingFailed(string errorMessage, int rawDataLength)
    {
        WriteEvent(206, errorMessage ?? "Unknown error", rawDataLength);
    }

    #endregion

    #region Contact Events (300-399)

    [Event(300, Level = EventLevel.Informational, Message = "Contact retrieval started for device: {deviceId}")]
    public void ContactRetrievalStarted(string deviceId)
    {
        WriteEvent(300, deviceId ?? "Unknown");
    }

    [Event(301, Level = EventLevel.Informational, Message = "Contact retrieval completed for device: {deviceId}. Found {contactCount} contacts")]
    public void ContactRetrievalCompleted(string deviceId, int contactCount)
    {
        WriteEvent(301, deviceId ?? "Unknown", contactCount);
    }

    [Event(302, Level = EventLevel.Verbose, Message = "Contact parsed: Name={contactName}, NodeId={nodeId}")]
    public void ContactParsed(string contactName, string nodeId)
    {
        WriteEvent(302, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    [Event(303, Level = EventLevel.Warning, Message = "Contact parsing failed: {errorMessage}")]
    public void ContactParsingFailed(string errorMessage)
    {
        WriteEvent(303, errorMessage ?? "Unknown error");
    }

    [Event(304, Level = EventLevel.Informational, Message = "Contact added: {contactName} with NodeId: {nodeId}")]
    public void ContactAdded(string contactName, string nodeId)
    {
        WriteEvent(304, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    [Event(305, Level = EventLevel.Informational, Message = "Contact removed: {contactId}")]
    public void ContactRemoved(string contactId)
    {
        WriteEvent(305, contactId ?? "Unknown");
    }

    #endregion

    #region Message Events (400-499)

    [Event(400, Level = EventLevel.Informational, Message = "Message sending started: To={toContactId}, Length={contentLength}")]
    public void MessageSendingStarted(string toContactId, int contentLength)
    {
        WriteEvent(400, toContactId ?? "Unknown", contentLength);
    }

    [Event(401, Level = EventLevel.Informational, Message = "Message sent successfully: To={toContactId}, MessageId={messageId}")]
    public void MessageSent(string toContactId, string messageId)
    {
        WriteEvent(401, toContactId ?? "Unknown", messageId ?? "Unknown");
    }

    [Event(402, Level = EventLevel.Error, Message = "Message sending failed: To={toContactId}, Error={errorMessage}")]
    public void MessageSendingFailed(string toContactId, string errorMessage)
    {
        WriteEvent(402, toContactId ?? "Unknown", errorMessage ?? "Unknown error");
    }

    [Event(403, Level = EventLevel.Informational, Message = "Message received: From={fromContactId}, Length={contentLength}")]
    public void MessageReceived(string fromContactId, int contentLength)
    {
        WriteEvent(403, fromContactId ?? "Unknown", contentLength);
    }

    [Event(404, Level = EventLevel.Informational, Message = "Message retrieval started for device: {deviceId}")]
    public void MessageRetrievalStarted(string deviceId)
    {
        WriteEvent(404, deviceId ?? "Unknown");
    }

    [Event(405, Level = EventLevel.Informational, Message = "Message retrieval completed for device: {deviceId}. Found {messageCount} messages")]
    public void MessageRetrievalCompleted(string deviceId, int messageCount)
    {
        WriteEvent(405, deviceId ?? "Unknown", messageCount);
    }

    #endregion

    #region Performance Events (500-599)

    [Event(500, Level = EventLevel.Verbose, Message = "Operation started: {operationName} for device: {deviceId}")]
    public void OperationStarted(string operationName, string deviceId)
    {
        WriteEvent(500, operationName ?? "Unknown", deviceId ?? "Unknown");
    }

    [Event(501, Level = EventLevel.Verbose, Message = "Operation completed: {operationName} for device: {deviceId} in {durationMs}ms")]
    public void OperationCompleted(string operationName, string deviceId, long durationMs)
    {
        WriteEvent(501, operationName ?? "Unknown", deviceId ?? "Unknown", durationMs);
    }

    [Event(502, Level = EventLevel.Warning, Message = "Operation slow: {operationName} for device: {deviceId} took {durationMs}ms")]
    public void OperationSlow(string operationName, string deviceId, long durationMs)
    {
        WriteEvent(502, operationName ?? "Unknown", deviceId ?? "Unknown", durationMs);
    }

    #endregion

    #region General Events (600-699)

    [Event(600, Level = EventLevel.Informational, Message = "SDK initialized successfully. Version: {version}")]
    public void SdkInitialized(string version)
    {
        WriteEvent(600, version ?? "Unknown");
    }

    [Event(601, Level = EventLevel.Error, Message = "Unexpected error: {errorMessage} in {source}")]
    public void UnexpectedError(string errorMessage, string source)
    {
        WriteEvent(601, errorMessage ?? "Unknown error", source ?? "Unknown");
    }

    [Event(602, Level = EventLevel.Warning, Message = "Deprecated feature used: {featureName} in {source}. Consider upgrading.")]
    public void DeprecatedFeatureUsed(string featureName, string source)
    {
        WriteEvent(602, featureName ?? "Unknown", source ?? "Unknown");
    }

    #endregion
}