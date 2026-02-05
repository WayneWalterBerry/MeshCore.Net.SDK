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

    /// <summary>
    /// Logs when device discovery starts for a specific transport
    /// </summary>
    /// <param name="transportType">The type of transport being used for discovery</param>
    [Event(100, Level = EventLevel.Informational, Message = "Device discovery started for transport: {transportType}")]
    public void DeviceDiscoveryStarted(string transportType)
    {
        WriteEvent(100, transportType);
    }

    /// <summary>
    /// Logs when device discovery completes with the number of devices found
    /// </summary>
    /// <param name="deviceCount">The number of devices discovered</param>
    /// <param name="transportType">The type of transport used for discovery</param>
    [Event(101, Level = EventLevel.Informational, Message = "Device discovery completed. Found {deviceCount} devices for transport: {transportType}")]
    public void DeviceDiscoveryCompleted(int deviceCount, string transportType)
    {
        WriteEvent(101, deviceCount, transportType);
    }

    /// <summary>
    /// Logs when a device connection attempt starts
    /// </summary>
    /// <param name="deviceId">The identifier of the device being connected to</param>
    /// <param name="transportType">The type of transport being used for connection</param>
    [Event(102, Level = EventLevel.Informational, Message = "Connecting to device: {deviceId} via {transportType}")]
    public void DeviceConnectionStarted(string deviceId, string transportType)
    {
        WriteEvent(102, deviceId ?? "Unknown", transportType ?? "Unknown");
    }

    /// <summary>
    /// Logs when a device connection succeeds
    /// </summary>
    /// <param name="deviceId">The identifier of the device that was connected</param>
    /// <param name="transportType">The type of transport used for connection</param>
    [Event(103, Level = EventLevel.Informational, Message = "Successfully connected to device: {deviceId} via {transportType}")]
    public void DeviceConnectionSucceeded(string deviceId, string transportType)
    {
        WriteEvent(103, deviceId ?? "Unknown", transportType ?? "Unknown");
    }

    /// <summary>
    /// Logs when a device connection fails
    /// </summary>
    /// <param name="deviceId">The identifier of the device that failed to connect</param>
    /// <param name="transportType">The type of transport used for connection</param>
    /// <param name="errorMessage">The error message describing the failure</param>
    [Event(104, Level = EventLevel.Error, Message = "Failed to connect to device: {deviceId} via {transportType}. Error: {errorMessage}")]
    public void DeviceConnectionFailed(string deviceId, string transportType, string errorMessage)
    {
        WriteEvent(104, deviceId ?? "Unknown", transportType ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a device is disconnected
    /// </summary>
    /// <param name="deviceId">The identifier of the device that was disconnected</param>
    [Event(105, Level = EventLevel.Informational, Message = "Device disconnected: {deviceId}")]
    public void DeviceDisconnected(string deviceId)
    {
        WriteEvent(105, deviceId ?? "Unknown");
    }

    #endregion

    #region Protocol Events (200-299)

    /// <summary>
    /// Logs when a command is being sent to a device
    /// </summary>
    /// <param name="command">The command byte being sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [Event(200, Level = EventLevel.Verbose, Message = "Sending command: {command} to device: {deviceId}")]
    public void CommandSending(byte command, string deviceId)
    {
        WriteEvent(200, command, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a command has been sent successfully
    /// </summary>
    /// <param name="command">The command byte that was sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [Event(201, Level = EventLevel.Verbose, Message = "Command sent successfully: {command} to device: {deviceId}")]
    public void CommandSent(byte command, string deviceId)
    {
        WriteEvent(201, command, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a response is received from a device
    /// </summary>
    /// <param name="command">The command byte that the response is for</param>
    /// <param name="status">The status byte in the response</param>
    /// <param name="deviceId">The identifier of the device that sent the response</param>
    [Event(202, Level = EventLevel.Verbose, Message = "Received response for command: {command} with status: {status} from device: {deviceId}")]
    public void ResponseReceived(byte command, byte status, string deviceId)
    {
        WriteEvent(202, command, status, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a command times out
    /// </summary>
    /// <param name="command">The command byte that timed out</param>
    /// <param name="deviceId">The identifier of the target device</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds</param>
    [Event(203, Level = EventLevel.Warning, Message = "Command timeout: {command} for device: {deviceId} after {timeoutMs}ms")]
    public void CommandTimeout(byte command, string deviceId, int timeoutMs)
    {
        WriteEvent(203, command, deviceId ?? "Unknown", timeoutMs);
    }

    /// <summary>
    /// Logs when a protocol error occurs
    /// </summary>
    /// <param name="command">The command byte that caused the error</param>
    /// <param name="status">The error status byte</param>
    /// <param name="errorMessage">The error message describing the problem</param>
    [Event(204, Level = EventLevel.Error, Message = "Protocol error for command: {command}, status: {status}, message: {errorMessage}")]
    public void ProtocolError(byte command, byte status, string errorMessage)
    {
        WriteEvent(204, command, status, errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a frame is successfully parsed
    /// </summary>
    /// <param name="startByte">The start byte of the parsed frame</param>
    /// <param name="length">The length of the parsed frame</param>
    /// <param name="payloadLength">The length of the payload in the parsed frame</param>
    [Event(205, Level = EventLevel.Verbose, Message = "Frame parsed successfully: StartByte={startByte}, Length={length}, PayloadLength={payloadLength}")]
    public void FrameParsed(byte startByte, ushort length, int payloadLength)
    {
        WriteEvent(205, startByte, length, payloadLength);
    }

    /// <summary>
    /// Logs when frame parsing fails
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed</param>
    /// <param name="rawDataLength">The length of the raw data that failed to parse</param>
    [Event(206, Level = EventLevel.Warning, Message = "Frame parsing failed: {errorMessage}, RawDataLength={rawDataLength}")]
    public void FrameParsingFailed(string errorMessage, int rawDataLength)
    {
        WriteEvent(206, errorMessage ?? "Unknown error", rawDataLength);
    }

    #endregion

    #region Contact Events (300-399)

    /// <summary>
    /// Logs when contact retrieval starts for a device
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which contacts are being retrieved</param>
    [Event(300, Level = EventLevel.Informational, Message = "Contact retrieval started for device: {deviceId}")]
    public void ContactRetrievalStarted(string deviceId)
    {
        WriteEvent(300, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when contact retrieval completes
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which contacts were retrieved</param>
    /// <param name="contactCount">The number of contacts retrieved</param>
    [Event(301, Level = EventLevel.Informational, Message = "Contact retrieval completed for device: {deviceId}. Found {contactCount} contacts")]
    public void ContactRetrievalCompleted(string deviceId, int contactCount)
    {
        WriteEvent(301, deviceId ?? "Unknown", contactCount);
    }

    /// <summary>
    /// Logs when a contact is successfully parsed
    /// </summary>
    /// <param name="contactName">The name of the parsed contact</param>
    /// <param name="nodeId">The node identifier of the parsed contact</param>
    [Event(302, Level = EventLevel.Verbose, Message = "Contact parsed: Name={contactName}, NodeId={nodeId}")]
    public void ContactParsed(string contactName, string nodeId)
    {
        WriteEvent(302, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    /// <summary>
    /// Logs when contact parsing fails
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed</param>
    [Event(303, Level = EventLevel.Warning, Message = "Contact parsing failed: {errorMessage}")]
    public void ContactParsingFailed(string errorMessage)
    {
        WriteEvent(303, errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a new contact is added
    /// </summary>
    /// <param name="contactName">The name of the contact that was added</param>
    /// <param name="nodeId">The node identifier of the contact that was added</param>
    [Event(304, Level = EventLevel.Informational, Message = "Contact added: {contactName} with NodeId: {nodeId}")]
    public void ContactAdded(string contactName, string nodeId)
    {
        WriteEvent(304, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a contact is removed
    /// </summary>
    /// <param name="contactId">The identifier of the contact that was removed</param>
    [Event(305, Level = EventLevel.Informational, Message = "Contact removed: {contactId}")]
    public void ContactRemoved(string contactId)
    {
        WriteEvent(305, contactId ?? "Unknown");
    }

    #endregion

    #region Message Events (400-499)

    /// <summary>
    /// Logs when message sending begins
    /// </summary>
    /// <param name="toContactId">The identifier of the contact the message is being sent to</param>
    /// <param name="contentLength">The length of the message content</param>
    [Event(400, Level = EventLevel.Informational, Message = "Message sending started: To={toContactId}, Length={contentLength}")]
    public void MessageSendingStarted(string toContactId, int contentLength)
    {
        WriteEvent(400, toContactId ?? "Unknown", contentLength);
    }

    /// <summary>
    /// Logs when a message is sent successfully
    /// </summary>
    /// <param name="toContactId">The identifier of the contact or channel the message was sent to</param>
    [Event(401, Level = EventLevel.Informational, Message = "Message sent successfully: To={toContactId}")]
    public void MessageSent(string toContactId)
    {
        WriteEvent(401, toContactId ?? "Unknown");
    }

    /// <summary>
    /// Logs when message sending fails
    /// </summary>
    /// <param name="toContactId">The identifier of the contact the message was being sent to</param>
    /// <param name="errorMessage">The error message describing the failure</param>
    [Event(402, Level = EventLevel.Error, Message = "Message sending failed: To={toContactId}, Error={errorMessage}")]
    public void MessageSendingFailed(string toContactId, string errorMessage)
    {
        WriteEvent(402, toContactId ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a message is received
    /// </summary>
    /// <param name="fromContactId">The identifier of the contact who sent the message</param>
    /// <param name="contentLength">The length of the received message content</param>
    [Event(403, Level = EventLevel.Informational, Message = "Message received: From={fromContactId}, Length={contentLength}")]
    public void MessageReceived(string fromContactId, int contentLength)
    {
        WriteEvent(403, fromContactId ?? "Unknown", contentLength);
    }

    /// <summary>
    /// Logs when message retrieval starts for a device
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which messages are being retrieved</param>
    [Event(404, Level = EventLevel.Informational, Message = "Message retrieval started for device: {deviceId}")]
    public void MessageRetrievalStarted(string deviceId)
    {
        WriteEvent(404, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when message retrieval completes
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which messages were retrieved</param>
    /// <param name="messageCount">The number of messages retrieved</param>
    [Event(405, Level = EventLevel.Informational, Message = "Message retrieval completed for device: {deviceId}. Found {messageCount} messages")]
    public void MessageRetrievalCompleted(string deviceId, int messageCount)
    {
        WriteEvent(405, deviceId ?? "Unknown", messageCount);
    }

    #endregion

    #region Performance Events (500-599)

    /// <summary>
    /// Logs when an operation starts
    /// </summary>
    /// <param name="operationName">The name of the operation being started</param>
    /// <param name="deviceId">The identifier of the device the operation is being performed on</param>
    [Event(500, Level = EventLevel.Verbose, Message = "Operation started: {operationName} for device: {deviceId}")]
    public void OperationStarted(string operationName, string deviceId)
    {
        WriteEvent(500, operationName ?? "Unknown", deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when an operation completes successfully
    /// </summary>
    /// <param name="operationName">The name of the operation that completed</param>
    /// <param name="deviceId">The identifier of the device the operation was performed on</param>
    /// <param name="durationMs">The duration of the operation in milliseconds</param>
    [Event(501, Level = EventLevel.Verbose, Message = "Operation completed: {operationName} for device: {deviceId} in {durationMs}ms")]
    public void OperationCompleted(string operationName, string deviceId, long durationMs)
    {
        WriteEvent(501, operationName ?? "Unknown", deviceId ?? "Unknown", durationMs);
    }

    /// <summary>
    /// Logs when an operation takes longer than expected
    /// </summary>
    /// <param name="operationName">The name of the slow operation</param>
    /// <param name="deviceId">The identifier of the device the operation was performed on</param>
    /// <param name="durationMs">The duration of the operation in milliseconds</param>
    [Event(502, Level = EventLevel.Warning, Message = "Operation slow: {operationName} for device: {deviceId} took {durationMs}ms")]
    public void OperationSlow(string operationName, string deviceId, long durationMs)
    {
        WriteEvent(502, operationName ?? "Unknown", deviceId ?? "Unknown", durationMs);
    }

    #endregion

    #region General Events (600-699)

    /// <summary>
    /// Logs when the SDK is successfully initialized
    /// </summary>
    /// <param name="version">The version of the SDK that was initialized</param>
    [Event(600, Level = EventLevel.Informational, Message = "SDK initialized successfully. Version: {version}")]
    public void SdkInitialized(string version)
    {
        WriteEvent(600, version ?? "Unknown");
    }

    /// <summary>
    /// Logs when an unexpected error occurs
    /// </summary>
    /// <param name="errorMessage">The error message describing the unexpected error</param>
    /// <param name="source">The source or context where the error occurred</param>
    [Event(601, Level = EventLevel.Error, Message = "Unexpected error: {errorMessage} in {source}")]
    public void UnexpectedError(string errorMessage, string source)
    {
        WriteEvent(601, errorMessage ?? "Unknown error", source ?? "Unknown");
    }

    /// <summary>
    /// Logs when a deprecated feature is used
    /// </summary>
    /// <param name="featureName">The name of the deprecated feature that was used</param>
    /// <param name="source">The source or context where the deprecated feature was used</param>
    [Event(602, Level = EventLevel.Warning, Message = "Deprecated feature used: {featureName} in {source}. Consider upgrading.")]
    public void DeprecatedFeatureUsed(string featureName, string source)
    {
        WriteEvent(602, featureName ?? "Unknown", source ?? "Unknown");
    }

    #endregion
}