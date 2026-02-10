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
    [Event(100, Level = EventLevel.Informational, Message = "Device discovery started for transport: {0}")]
    public void DeviceDiscoveryStarted(string transportType)
    {
        WriteEvent(100, transportType);
    }

    /// <summary>
    /// Logs when device discovery completes with the number of devices found
    /// </summary>
    /// <param name="deviceCount">The number of devices discovered</param>
    /// <param name="transportType">The type of transport used for discovery</param>
    [Event(101, Level = EventLevel.Informational, Message = "Device discovery completed. Found {0} devices for transport: {1}")]
    public void DeviceDiscoveryCompleted(int deviceCount, string transportType)
    {
        WriteEvent(101, deviceCount, transportType);
    }

    /// <summary>
    /// Logs when a device connection attempt starts
    /// </summary>
    /// <param name="deviceId">The identifier of the device being connected to</param>
    /// <param name="transportType">The type of transport being used for connection</param>
    [Event(102, Level = EventLevel.Informational, Message = "Connecting to device: {0} via {1}")]
    public void DeviceConnectionStarted(string deviceId, string transportType)
    {
        WriteEvent(102, deviceId ?? "Unknown", transportType ?? "Unknown");
    }

    /// <summary>
    /// Logs when a device connection succeeds
    /// </summary>
    /// <param name="deviceId">The identifier of the device that was connected</param>
    /// <param name="transportType">The type of transport used for connection</param>
    [Event(103, Level = EventLevel.Informational, Message = "Successfully connected to device: {0} via {1}")]
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
    [Event(104, Level = EventLevel.Error, Message = "Failed to connect to device: {0} via {1}. Error: {2}")]
    public void DeviceConnectionFailed(string deviceId, string transportType, string errorMessage)
    {
        WriteEvent(104, deviceId ?? "Unknown", transportType ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a device is disconnected
    /// </summary>
    /// <param name="deviceId">The identifier of the device that was disconnected</param>
    [Event(105, Level = EventLevel.Informational, Message = "Device disconnected: {0}")]
    public void DeviceDisconnected(string deviceId)
    {
        WriteEvent(105, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when USB transport is created
    /// </summary>
    /// <param name="portName">The serial port name</param>
    /// <param name="baudRate">The baud rate for communication</param>
    [Event(106, Level = EventLevel.Verbose, Message = "USB Transport created for port {0} at {1} baud")]
    public void UsbTransportCreated(string portName, int baudRate)
    {
        WriteEvent(106, portName ?? "Unknown", baudRate);
    }

    /// <summary>
    /// Logs when serial port is opened successfully
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(107, Level = EventLevel.Verbose, Message = "Serial port {0} opened successfully")]
    public void SerialPortOpened(string portName)
    {
        WriteEvent(107, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when disconnection starts
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(108, Level = EventLevel.Verbose, Message = "Disconnecting from {0}")]
    public void DisconnectingFrom(string portName)
    {
        WriteEvent(108, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs error during disconnect
    /// </summary>
    /// <param name="portName">The serial port name</param>
    /// <param name="errorMessage">The error message</param>
    [Event(109, Level = EventLevel.Error, Message = "Error during disconnect from {0}: {1}")]
    public void DisconnectError(string portName, string errorMessage)
    {
        WriteEvent(109, portName ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when frame is sent successfully
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(110, Level = EventLevel.Verbose, Message = "Frame sent successfully to {0}")]
    public void FrameSentSuccessfully(string portName)
    {
        WriteEvent(110, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when frame send fails
    /// </summary>
    /// <param name="portName">The serial port name</param>
    /// <param name="errorMessage">The error message</param>
    [Event(111, Level = EventLevel.Error, Message = "Failed to send frame to {0}: {1}")]
    public void FrameSendFailed(string portName, string errorMessage)
    {
        WriteEvent(111, portName ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when receive loop starts
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(112, Level = EventLevel.Verbose, Message = "Starting receive loop for {0}")]
    public void ReceiveLoopStarted(string portName)
    {
        WriteEvent(112, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when receive loop is cancelled
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(113, Level = EventLevel.Verbose, Message = "Receive loop cancelled for {0}")]
    public void ReceiveLoopCancelled(string portName)
    {
        WriteEvent(113, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when receive loop ends
    /// </summary>
    /// <param name="portName">The serial port name</param>
    [Event(114, Level = EventLevel.Verbose, Message = "Receive loop ended for {0}")]
    public void ReceiveLoopEnded(string portName)
    {
        WriteEvent(114, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs error in receive loop
    /// </summary>
    /// <param name="portName">The serial port name</param>
    /// <param name="errorMessage">The error message</param>
    [Event(115, Level = EventLevel.Error, Message = "Error in receive loop for {0}: {1}")]
    public void ReceiveLoopError(string portName, string errorMessage)
    {
        WriteEvent(115, portName ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when no start byte found in buffer
    /// </summary>
    /// <param name="bufferSize">Size of the buffer being cleared</param>
    [Event(116, Level = EventLevel.Verbose, Message = "No start byte found in {0} bytes, clearing buffer")]
    public void NoStartByteFound(int bufferSize)
    {
        WriteEvent(116, bufferSize);
    }

    /// <summary>
    /// Logs when bytes are removed before start byte
    /// </summary>
    /// <param name="byteCount">Number of bytes removed</param>
    [Event(117, Level = EventLevel.Verbose, Message = "Removing {0} bytes before start byte")]
    public void RemovingBytesBeforeStartByte(int byteCount)
    {
        WriteEvent(117, byteCount);
    }

    /// <summary>
    /// Logs when waiting for more data
    /// </summary>
    /// <param name="bufferSize">Current buffer size</param>
    /// <param name="totalFrameSize">Required frame size</param>
    [Event(118, Level = EventLevel.Verbose, Message = "Waiting for more data: have {0} bytes, need {1}")]
    public void WaitingForMoreData(int bufferSize, int totalFrameSize)
    {
        WriteEvent(118, bufferSize, totalFrameSize);
    }

    /// <summary>
    /// Logs unexpected response code
    /// </summary>
    /// <param name="responseCode">The unexpected response code</param>
    /// <param name="expectedCommand">The expected command</param>
    /// <param name="deviceId">The device identifier</param>
    [Event(119, Level = EventLevel.Warning, Message = "Unexpected response code {0} for command {1} on device {2}")]
    public void UnexpectedResponseCode(byte responseCode, byte expectedCommand, string deviceId)
    {
        WriteEvent(119, responseCode, expectedCommand, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when testing port during discovery
    /// </summary>
    /// <param name="portName">The port name being tested</param>
    [Event(120, Level = EventLevel.Verbose, Message = "Testing port {0}...")]
    public void TestingPort(string portName)
    {
        WriteEvent(120, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs connection timeout during discovery
    /// </summary>
    /// <param name="portName">The port name that timed out</param>
    [Event(121, Level = EventLevel.Verbose, Message = "Connection timeout for {0}")]
    public void ConnectionTimeout(string portName)
    {
        WriteEvent(121, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when port responds but not with device info
    /// </summary>
    /// <param name="portName">The port name</param>
    /// <param name="responseCode">The response code received</param>
    [Event(122, Level = EventLevel.Verbose, Message = "{0} responded but not with device info (response code: {1})")]
    public void PortRespondedWithoutDeviceInfo(string portName, byte responseCode)
    {
        WriteEvent(122, portName ?? "Unknown", responseCode);
    }

    /// <summary>
    /// Logs connection error during discovery
    /// </summary>
    /// <param name="portName">The port name</param>
    /// <param name="errorMessage">The error message</param>
    [Event(123, Level = EventLevel.Verbose, Message = "Connection error for {0}: {1}")]
    public void DiscoveryConnectionError(string portName, string errorMessage)
    {
        WriteEvent(123, portName ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs timeout during discovery
    /// </summary>
    /// <param name="portName">The port name</param>
    [Event(124, Level = EventLevel.Verbose, Message = "Timeout for {0} - likely not a MeshCore device")]
    public void DiscoveryTimeout(string portName)
    {
        WriteEvent(124, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs access denied during discovery
    /// </summary>
    /// <param name="portName">The port name</param>
    /// <param name="errorMessage">The error message</param>
    [Event(125, Level = EventLevel.Verbose, Message = "Access denied for {0}: {1}")]
    public void DiscoveryAccessDenied(string portName, string errorMessage)
    {
        WriteEvent(125, portName ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs general error during discovery
    /// </summary>
    /// <param name="portName">The port name</param>
    /// <param name="exceptionType">The exception type</param>
    /// <param name="errorMessage">The error message</param>
    [Event(126, Level = EventLevel.Verbose, Message = "Error testing {0}: {1} - {2}")]
    public void DiscoveryGeneralError(string portName, string exceptionType, string errorMessage)
    {
        WriteEvent(126, portName ?? "Unknown", exceptionType ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs found serial ports count
    /// </summary>
    /// <param name="portCount">Number of ports found</param>
    /// <param name="portNames">Comma-separated list of port names</param>
    [Event(127, Level = EventLevel.Verbose, Message = "Found {0} serial ports: {1}")]
    public void FoundSerialPorts(int portCount, string portNames)
    {
        WriteEvent(127, portCount, portNames ?? string.Empty);
    }

    /// <summary>
    /// Logs when connected to port during discovery
    /// </summary>
    /// <param name="portName">The port name</param>
    [Event(128, Level = EventLevel.Verbose, Message = "Connected to {0}")]
    public void ConnectedToPort(string portName)
    {
        WriteEvent(128, portName ?? "Unknown");
    }

    /// <summary>
    /// Logs when port is identified as MeshCore device
    /// </summary>
    /// <param name="portName">The port name</param>
    [Event(129, Level = EventLevel.Informational, Message = "{0} is a MeshCore device!")]
    public void MeshCoreDeviceIdentified(string portName)
    {
        WriteEvent(129, portName ?? "Unknown");
    }

    #endregion

    #region Protocol Events (200-299)

    /// <summary>
    /// Logs when a command is being sent to a device
    /// </summary>
    /// <param name="command">The command byte being sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [Event(200, Level = EventLevel.Verbose, Message = "Sending command: {0} to device: {1}")]
    public void CommandSending(byte command, string deviceId)
    {
        WriteEvent(200, command, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a command has been sent successfully
    /// </summary>
    /// <param name="command">The command byte that was sent</param>
    /// <param name="deviceId">The identifier of the target device</param>
    [Event(201, Level = EventLevel.Verbose, Message = "Command sent successfully: {0} to device: {1}")]
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
    [Event(202, Level = EventLevel.Verbose, Message = "Received response for command: {0} with status: {1} from device: {2}")]
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
    [Event(203, Level = EventLevel.Warning, Message = "Command timeout: {0} for device: {1} after {2}ms")]
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
    [Event(204, Level = EventLevel.Error, Message = "Protocol error for command: {0}, status: {1}, message: {2}")]
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
    [Event(205, Level = EventLevel.Verbose, Message = "Frame parsed successfully: StartByte={0}, Length={1}, PayloadLength={2}")]
    public void FrameParsed(byte startByte, ushort length, int payloadLength)
    {
        WriteEvent(205, startByte, length, payloadLength);
    }

    /// <summary>
    /// Logs when frame parsing fails
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed</param>
    /// <param name="rawDataLength">The length of the raw data that failed to parse</param>
    [Event(206, Level = EventLevel.Warning, Message = "Frame parsing failed: {0}, RawDataLength={1}")]
    public void FrameParsingFailed(string errorMessage, int rawDataLength)
    {
        WriteEvent(206, errorMessage ?? "Unknown error", rawDataLength);
    }

    /// <summary>
    /// Logs raw data received from device
    /// </summary>
    [Event(207, Level = EventLevel.Verbose, Message = "Received data: {0}")]
    public void RawDataReceived(string rawData)
    {
        WriteEvent(207, rawData ?? string.Empty);
    }

    /// <summary>
    /// Logs raw data being sent to device
    /// </summary>
    [Event(208, Level = EventLevel.Verbose, Message = "Sending raw data: {0}")]
    public void RawDataSending(string rawData)
    {
        WriteEvent(208, rawData ?? string.Empty);
    }

    /// <summary>
    /// Logs sending packet
    /// </summary>
    [Event(209, Level = EventLevel.Verbose, Message = "Sending pkt {0} (len={1})")]
    public void SendingPacket(string packet, int length)
    {
        WriteEvent(209, packet ?? string.Empty, length);
    }

    /// <summary>
    /// Logs SYNC_NEXT_MESSAGE response
    /// </summary>
    [Event(210, Level = EventLevel.Verbose, Message = "SYNC_NEXT_MESSAGE response: {0} (Status: {1}) payloadLen={2}")]
    public void SyncNextMessageResponse(string responseCode, string status, int payloadLength)
    {
        WriteEvent(210, responseCode ?? "Unknown", status ?? "null", payloadLength);
    }

    #endregion

    #region Contact Events (300-399)

    /// <summary>
    /// Logs when contact retrieval starts for a device
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which contacts are being retrieved</param>
    [Event(300, Level = EventLevel.Informational, Message = "Contact retrieval started for device: {0}")]
    public void ContactRetrievalStarted(string deviceId)
    {
        WriteEvent(300, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when contact retrieval completes
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which contacts were retrieved</param>
    [Event(301, Level = EventLevel.Informational, Message = "Contact retrieval completed for device: {0}.")]
    public void ContactRetrievalCompleted(string deviceId)
    {
        WriteEvent(301, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a contact is successfully parsed
    /// </summary>
    /// <param name="contactName">The name of the parsed contact</param>
    /// <param name="nodeId">The node identifier of the parsed contact</param>
    [Event(302, Level = EventLevel.Verbose, Message = "Contact parsed: Name={0}, NodeId={1}")]
    public void ContactParsed(string contactName, string nodeId)
    {
        WriteEvent(302, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    /// <summary>
    /// Logs when contact parsing fails
    /// </summary>
    /// <param name="errorMessage">The error message describing why parsing failed</param>
    [Event(303, Level = EventLevel.Warning, Message = "Contact parsing failed: {0}")]
    public void ContactParsingFailed(string errorMessage)
    {
        WriteEvent(303, errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a new contact is added
    /// </summary>
    /// <param name="contactName">The name of the contact that was added</param>
    /// <param name="nodeId">The node identifier of the contact that was added</param>
    [Event(304, Level = EventLevel.Informational, Message = "Contact added: {0} with NodeId: {1}")]
    public void ContactAdded(string contactName, string nodeId)
    {
        WriteEvent(304, contactName ?? "Unknown", nodeId ?? "Unknown");
    }

    /// <summary>
    /// Logs when a contact is removed
    /// </summary>
    /// <param name="contactId">The identifier of the contact that was removed</param>
    [Event(305, Level = EventLevel.Informational, Message = "Contact removed: {0}")]
    public void ContactRemoved(string contactId)
    {
        WriteEvent(305, contactId ?? "Unknown");
    }

    /// <summary>
    /// Logs detailed contact dispatching matching CLI format
    /// </summary>
    [Event(306, Level = EventLevel.Verbose, Message = "Dispatching contact: PublicKey={0}, Type={1}, Flags={2}, OutPathLen={3}, Name='{4}', LastAdvert={5}, Lat={6}, Lon={7}, LastMod={8}")]
    public void DispatchingContact(
        string publicKey,
        int type,
        int flags,
        int outPathLen,
        string name,
        long lastAdvert,
        double lat,
        double lon,
        long lastMod)
    {
        WriteEvent(306, publicKey ?? "Unknown", type, flags, outPathLen, name ?? "Unknown", lastAdvert, lat, lon, lastMod);
    }

    /// <summary>
    /// Logs contact retrieval protocol mode
    /// </summary>
    [Event(307, Level = EventLevel.Verbose, Message = "Device {0} using standard contact retrieval protocol with total count: {1}")]
    public void ContactRetrievalProtocol(string deviceId, int totalCount)
    {
        WriteEvent(307, deviceId ?? "Unknown", totalCount);
    }

    /// <summary>
    /// Logs parsing contacts sequence
    /// </summary>
    [Event(308, Level = EventLevel.Verbose, Message = "Parsing contacts sequence for device {0}")]
    public void ParsingContactsSequence(string deviceId)
    {
        WriteEvent(308, deviceId ?? "Unknown");
    }

    #endregion

    #region Message Events (400-499)

    /// <summary>
    /// Logs when message sending begins
    /// </summary>
    /// <param name="toContactId">The identifier of the contact the message is being sent to</param>
    /// <param name="contentLength">The length of the message content</param>
    [Event(400, Level = EventLevel.Informational, Message = "Message sending started: To={0}, Length={1}")]
    public void MessageSendingStarted(string toContactId, int contentLength)
    {
        WriteEvent(400, toContactId ?? "Unknown", contentLength);
    }

    /// <summary>
    /// Logs when a message is sent successfully
    /// </summary>
    /// <param name="toContactId">The identifier of the contact or channel the message was sent to</param>
    [Event(401, Level = EventLevel.Informational, Message = "Message sent successfully: To={0}")]
    public void MessageSent(string toContactId)
    {
        WriteEvent(401, toContactId ?? "Unknown");
    }

    /// <summary>
    /// Logs when message sending fails
    /// </summary>
    /// <param name="toContactId">The identifier of the contact the message was being sent to</param>
    /// <param name="errorMessage">The error message describing the failure</param>
    [Event(402, Level = EventLevel.Error, Message = "Message sending failed: To={0}, Error={1}")]
    public void MessageSendingFailed(string toContactId, string errorMessage)
    {
        WriteEvent(402, toContactId ?? "Unknown", errorMessage ?? "Unknown error");
    }

    /// <summary>
    /// Logs when a message is received
    /// </summary>
    /// <param name="fromContactId">The identifier of the contact who sent the message</param>
    /// <param name="contentLength">The length of the received message content</param>
    [Event(403, Level = EventLevel.Informational, Message = "Message received: From={0}, Length={1}")]
    public void MessageReceived(string fromContactId, int contentLength)
    {
        WriteEvent(403, fromContactId ?? "Unknown", contentLength);
    }

    /// <summary>
    /// Logs when message retrieval starts for a device
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which messages are being retrieved</param>
    [Event(404, Level = EventLevel.Informational, Message = "Message retrieval started for device: {0}")]
    public void MessageRetrievalStarted(string deviceId)
    {
        WriteEvent(404, deviceId ?? "Unknown");
    }

    /// <summary>
    /// Logs when message retrieval completes
    /// </summary>
    /// <param name="deviceId">The identifier of the device from which messages were retrieved</param>
    /// <param name="messageCount">The number of messages retrieved</param>
    [Event(405, Level = EventLevel.Informational, Message = "Message retrieval completed for device: {0}. Found {1} messages")]
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
    [Event(500, Level = EventLevel.Verbose, Message = "Operation started: {0} for device: {1}")]
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
    [Event(501, Level = EventLevel.Verbose, Message = "Operation completed: {0} for device: {1} in {2}ms")]
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
    [Event(502, Level = EventLevel.Warning, Message = "Operation slow: {0} for device: {1} took {2}ms")]
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
    [Event(600, Level = EventLevel.Informational, Message = "SDK initialized successfully. Version: {0}")]
    public void SdkInitialized(string version)
    {
        WriteEvent(600, version ?? "Unknown");
    }

    /// <summary>
    /// Logs when an unexpected error occurs
    /// </summary>
    /// <param name="errorMessage">The error message describing the unexpected error</param>
    /// <param name="source">The source or context where the error occurred</param>
    [Event(601, Level = EventLevel.Error, Message = "Unexpected error: {0} in {1}")]
    public void UnexpectedError(string errorMessage, string source)
    {
        WriteEvent(601, errorMessage ?? "Unknown error", source ?? "Unknown");
    }

    /// <summary>
    /// Logs when a deprecated feature is used
    /// </summary>
    /// <param name="featureName">The name of the deprecated feature that was used</param>
    /// <param name="source">The source or context where the deprecated feature was used</param>
    [Event(602, Level = EventLevel.Warning, Message = "Deprecated feature used: {0} in {1}. Consider upgrading.")]
    public void DeprecatedFeatureUsed(string featureName, string source)
    {
        WriteEvent(602, featureName ?? "Unknown", source ?? "Unknown");
    }

    #endregion
}