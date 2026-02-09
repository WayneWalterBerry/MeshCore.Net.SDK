using System.IO.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;
using MeshCore.Net.SDK.Logging;

namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Handles low-level USB serial communication with MeshCore devices
/// </summary>
public class UsbTransport : ITransport
{
    private readonly SerialPort _serialPort;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ILogger<UsbTransport> _logger;
    private bool _disposed;

    /// <summary>
    /// Event fired when a frame is received from the MeshCore device
    /// </summary>
    public event EventHandler<MeshCoreFrame>? FrameReceived;

    /// <summary>
    /// Event fired when an error occurs during communication
    /// </summary>
    public event EventHandler<Exception>? ErrorOccurred;

    /// <summary>
    /// Gets whether the transport is currently connected to a MeshCore device
    /// </summary>
    public bool IsConnected => _serialPort?.IsOpen == true;

    /// <summary>
    /// Gets the connection identifier (port name) for this transport
    /// </summary>
    public string? ConnectionId => _serialPort?.PortName;

    /// <summary>
    /// Creates a new USB transport for the specified serial port
    /// </summary>
    /// <param name="portName">The name of the serial port (e.g., "COM3")</param>
    /// <param name="baudRate">The baud rate for communication (default: 115200)</param>
    /// <param name="loggerFactory">Optional logger factory for diagnostic logging</param>
    public UsbTransport(string portName, int baudRate = 115200, ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory?.CreateLogger<UsbTransport>() ?? NullLogger<UsbTransport>.Instance;

        _serialPort = new SerialPort(portName, baudRate)
        {
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        _logger.LogDebug("USB Transport created for port {PortName} at {BaudRate} baud", portName, baudRate);
    }

    /// <summary>
    /// Opens the connection to the MeshCore device
    /// </summary>
    public async Task ConnectAsync()
    {
        var portName = _serialPort.PortName;
        _logger.LogDeviceConnectionStarted(portName, "USB");
        MeshCoreSdkEventSource.Log.DeviceConnectionStarted(portName, "USB");

        try
        {
            _serialPort.Open();
            _logger.LogDebug("Serial port {PortName} opened successfully", portName);

            // Start the receive loop
            _ = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);

            // Wait a moment for the connection to stabilize
            await Task.Delay(100);

            _logger.LogDeviceConnectionSucceeded(portName, "USB");
            MeshCoreSdkEventSource.Log.DeviceConnectionSucceeded(portName, "USB");
        }
        catch (Exception ex)
        {
            _logger.LogDeviceConnectionFailed(ex, portName, "USB");
            MeshCoreSdkEventSource.Log.DeviceConnectionFailed(portName, "USB", ex.Message);
            throw new DeviceConnectionException(ConnectionId, ex);
        }
    }

    /// <summary>
    /// Closes the connection to the MeshCore device
    /// </summary>
    public void Disconnect()
    {
        var portName = _serialPort?.PortName ?? "Unknown";
        _logger.LogDebug("Disconnecting from {PortName}", portName);

        try
        {
            _cancellationTokenSource.Cancel();
            _serialPort?.Close();

            _logger.LogDeviceDisconnected(portName);
            MeshCoreSdkEventSource.Log.DeviceDisconnected(portName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disconnect from {PortName}", portName);
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// Sends a frame to the MeshCore device
    /// </summary>
    public async Task SendFrameAsync(MeshCoreFrame frame)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Transport is not connected");

        var frameBytes = frame.ToByteArray();
        var portName = _serialPort.PortName;

        await _writeLock.WaitAsync();

        try
        {
            var hex = BitConverter.ToString(frameBytes).Replace("-", string.Empty);
            _logger.LogInformation("Sending pkt {Hex} (len={Len})", hex, frame.Length);

            await _serialPort.BaseStream.WriteAsync(frameBytes, 0, frameBytes.Length);
            await _serialPort.BaseStream.FlushAsync();

            _logger.LogTrace("Frame sent successfully to {PortName}", portName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send frame to {PortName}", portName);
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Sends a command and waits for a response.
    /// </summary>
    /// <param name="command">The command to send to the MeshCore device.</param>
    /// <param name="data">Optional payload data to include with the command.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the command operation.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the response frame from the device.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the transport is not connected.</exception>
    /// <exception cref="MeshCoreTimeoutException">Thrown if the operation is canceled or times out before a response is received.</exception>
    public async Task<MeshCoreFrame> SendCommandAsync(
        MeshCoreCommand command,
        byte[]? data = null,
        CancellationToken cancellationToken = default)
    {
        data ??= Array.Empty<byte>();

        var portName = _serialPort.PortName;

        _logger.LogCommandSending((byte)command, portName);
        MeshCoreSdkEventSource.Log.CommandSending((byte)command, portName);

        // Build payload: command byte + data
        var payload = new byte[1 + data.Length];
        payload[0] = (byte)command;
        Array.Copy(data, 0, payload, 1, data.Length);

        var hex = BitConverter.ToString(data).Replace("-", string.Empty);
        _logger.LogInformation("Sending raw data: {CommandByte:X2}", (byte)command);

        var frame = MeshCoreFrame.CreateInbound(payload);

        // Wait for the response using the provided cancellation token
        var responseTask = WaitForResponseAsync(command, cancellationToken);

        // Send the command frame
        await SendFrameAsync(frame).ConfigureAwait(false);

        try
        {
            var response = await responseTask.ConfigureAwait(false);

            _logger.LogCommandSent((byte)command, portName);
            _logger.LogResponseReceived((byte)command, response.Payload.FirstOrDefault(), portName);

            return response;
        }
        catch (MeshCoreTimeoutException)
        {
            _logger.LogWarning(
                "Command {Command} to device {DeviceId} timed out or was canceled",
                (byte)command,
                portName);

            MeshCoreSdkEventSource.Log.CommandTimeout((byte)command, portName, 0);
            throw;
        }
    }

    /// <summary>
    /// Waits asynchronously for a response frame matching the expected command, or throws if the operation is canceled
    /// or times out.
    /// </summary>
    /// <remarks>If the operation is canceled or times out before a valid response is received, a
    /// MeshCoreTimeoutException is thrown. Only response frames with codes appropriate for the specified command are
    /// considered valid; unexpected response codes are ignored and logged for diagnostic purposes.</remarks>
    /// <param name="expectedCommand">The command for which a corresponding response frame is expected. Determines the set of valid response codes
    /// that will be accepted.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation before a matching response is received.</param>
    /// <returns>A task that represents the asynchronous operation. The result contains the received response frame that matches
    /// the expected command.</returns>
    private async Task<MeshCoreFrame> WaitForResponseAsync(MeshCoreCommand expectedCommand, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<MeshCoreFrame>(TaskCreationOptions.RunContinuationsAsynchronously);

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            if (!frame.IsOutbound)
            {
                return;
            }

            var responseCode = frame.GetResponseCode();

            bool isValidResponse = expectedCommand switch
            {
                MeshCoreCommand.CMD_DEVICE_QUERY =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_DEVICE_INFO ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_CONTACT_LIST_GET =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACTS_START ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_CONTACT_BY_KEY =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_DEVICE_TIME =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_CURR_TIME ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_BATT_AND_STORAGE =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_CHANNEL =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_INFO ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_TUNING_PARAMS =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_TUNING_PARAMS ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_ADVERT_PATH =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_ADVERT_PATH ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_STATS =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_STATS ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_GET_AUTOADD_CONFIG =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_AUTOADD_CONFIG ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_APP_START =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_SELF_INFO ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SEND_TXT_MSG =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_SENT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_SENT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SEND_PATH_DISCOVERY_REQ =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_SENT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_ADD_UPDATE_CONTACT =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_REMOVE_CONTACT =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_RADIO_PARAMS =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_RADIO_TX_POWER =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_TUNING_PARAMS =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_OTHER_PARAMS =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_AUTOADD_CONFIG =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_CHANNEL =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_SET_ADVERT_NAME =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                MeshCoreCommand.CMD_REBOOT =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                // SYNC_NEXT_MESSAGE is special: multiple response codes are valid over time
                MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3 ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3 ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR,

                // Default: simple command/ack style
                _ =>
                    responseCode == MeshCoreResponseCode.RESP_CODE_OK ||
                    responseCode == MeshCoreResponseCode.RESP_CODE_ERR
            };

            if (isValidResponse)
            {
                FrameReceived -= OnFrameReceived;
                tcs.TrySetResult(frame);
            }
            else
            {
                // Log unexpected response for debugging
                _logger.LogWarning(
                    "Unexpected response code {ResponseCode} for command {Command} on device {DeviceId}",
                    responseCode,
                    expectedCommand,
                    _serialPort?.PortName ?? "Unknown");
            }
        }

        FrameReceived += OnFrameReceived;

        using (cancellationToken.Register(
                   static state =>
                   {
                       var sourceTcs = (TaskCompletionSource<MeshCoreFrame>)state!;
                       sourceTcs.TrySetCanceled();
                   },
                   tcs))
        {
            try
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                FrameReceived -= OnFrameReceived;
            }
        }
    }

    /// <summary>
    /// Background task that continuously reads frames from the serial port
    /// </summary>
    private async Task ReceiveLoop()
    {
        var buffer = new byte[ProtocolConstants.MAX_FRAME_SIZE];
        var frameBuffer = new List<byte>();
        var portName = _serialPort.PortName;

        _logger.LogDebug("Starting receive loop for {PortName}", portName);

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (!IsConnected)
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                    continue;
                }

                var bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0)
                {
                    await Task.Delay(10, _cancellationTokenSource.Token);
                    continue;
                }

                var bytesRead = await _serialPort.BaseStream.ReadAsync(
                    buffer, 0, Math.Min(bytesToRead, buffer.Length), _cancellationTokenSource.Token);

                var hex = BitConverter.ToString(buffer, 0, bytesRead).Replace("-", string.Empty);
                _logger.LogDebug("Received data: {Data}", hex);

                for (int i = 0; i < bytesRead; i++)
                {
                    frameBuffer.Add(buffer[i]);

                    // Try to parse a complete frame
                    if (TryParseFrame(frameBuffer, out var frame) && frame != null)
                    {
                        _logger.LogFrameParsed(frame.StartByte, frame.Length, frame.Payload.Length);
                        MeshCoreSdkEventSource.Log.FrameParsed(frame.StartByte, frame.Length, frame.Payload.Length);

                        FrameReceived?.Invoke(this, frame);
                        frameBuffer.Clear();
                    }
                    else if (frameBuffer.Count > ProtocolConstants.MAX_FRAME_SIZE)
                    {
                        // Buffer too large, clear it to prevent memory issues
                        var ex = new FrameParseException("Frame buffer overflow");
                        _logger.LogFrameParsingFailed(ex, frameBuffer.Count);
                        MeshCoreSdkEventSource.Log.FrameParsingFailed(ex.Message, frameBuffer.Count);

                        frameBuffer.Clear();
                        ErrorOccurred?.Invoke(this, ex);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Receive loop cancelled for {PortName}", portName);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in receive loop for {PortName}", portName);
                ErrorOccurred?.Invoke(this, ex);
                await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before retry
            }
        }

        _logger.LogDebug("Receive loop ended for {PortName}", portName);
    }

    /// <summary>
    /// Tries to parse a complete frame from the buffer
    /// </summary>
    private bool TryParseFrame(List<byte> buffer, out MeshCoreFrame? frame)
    {
        frame = null;

        if (buffer.Count < ProtocolConstants.FRAME_HEADER_SIZE)
        {
            return false;
        }

        // Look for start byte
        var startIndex = -1;
        for (int i = 0; i < buffer.Count; i++)
        {
            if (buffer[i] == ProtocolConstants.FRAME_START_OUTBOUND)
            {
                startIndex = i;
                break;
            }
        }

        if (startIndex == -1)
        {
            // No start byte found, clear buffer
            if (buffer.Count > 0)
            {
                _logger.LogTrace("No start byte found in {BufferSize} bytes, clearing buffer", buffer.Count);
            }
            buffer.Clear();
            return false;
        }

        if (startIndex > 0)
        {
            // Remove bytes before start byte
            _logger.LogTrace("Removing {ByteCount} bytes before start byte", startIndex);
            buffer.RemoveRange(0, startIndex);
        }

        if (buffer.Count < ProtocolConstants.FRAME_HEADER_SIZE)
            return false;

        // Parse length
        var length = (ushort)(buffer[1] | (buffer[2] << 8));
        var totalFrameSize = ProtocolConstants.FRAME_HEADER_SIZE + length;

        if (buffer.Count < totalFrameSize)
        {
            _logger.LogTrace("Waiting for more data: have {BufferSize} bytes, need {TotalFrameSize}", buffer.Count, totalFrameSize);
            return false; // Not enough data yet
        }

        // Extract frame data
        var frameData = buffer.Take(totalFrameSize).ToArray();
        buffer.RemoveRange(0, totalFrameSize);

        try
        {
            frame = MeshCoreFrame.Parse(frameData);
            return frame != null;
        }
        catch (Exception ex)
        {
            _logger.LogFrameParsingFailed(ex, frameData.Length);
            MeshCoreSdkEventSource.Log.FrameParsingFailed(ex.Message, frameData.Length);
            return false;
        }
    }

    /// <summary>
    /// Discovers available MeshCore devices on serial ports
    /// </summary>
    public static async Task<List<MeshCoreDevice>> DiscoverDevicesAsync(ILoggerFactory? loggerFactory = null, CancellationToken cancellationToken = default)
    {
        var logger = loggerFactory?.CreateLogger<UsbTransport>() ?? NullLogger<UsbTransport>.Instance;
        var devices = new List<MeshCoreDevice>();
        var portNames = SerialPort.GetPortNames();

        logger.LogDeviceDiscoveryStarted("USB");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryStarted("USB");

        logger.LogDebug("Found {PortCount} serial ports: {PortNames}", portNames.Length, string.Join(", ", portNames));

        foreach (var portName in portNames)
        {
            logger.LogDebug("Testing port {PortName}...", portName);

            try
            {
                using var transport = new UsbTransport(portName, loggerFactory: loggerFactory);

                // Try to connect with timeout
                var connectTask = transport.ConnectAsync();
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                {
                    logger.LogDebug("Connection timeout for {PortName}", portName);
                    continue;
                }

                await connectTask; // This will throw if connection failed
                logger.LogDebug("Connected to {PortName}", portName);

                // According to research, try CMD_APP_START first for older firmware compatibility
                var appStartResponse = await transport.SendCommandAsync(
                    MeshCoreCommand.CMD_APP_START,
                    new byte[] { 0x08 }, // Protocol version 8
                    cancellationToken);

                // Now try device query
                var deviceQueryResponse = await transport.SendCommandAsync(
                    MeshCoreCommand.CMD_DEVICE_QUERY,
                    new byte[] { 0x08 }, // Protocol version 8
                    cancellationToken);

                // Check if we got a valid device info response (RESP_CODE_DEVICE_INFO = 0x0D)
                if (deviceQueryResponse.Payload?.Length > 0 && deviceQueryResponse.Payload[0] == 0x0D)
                {
                    logger.LogInformation("{PortName} is a MeshCore device!", portName);
                    devices.Add(new MeshCoreDevice
                    {
                        Id = portName,
                        Name = $"MeshCore USB Device ({portName})",
                        ConnectionType = DeviceConnectionType.USB,
                        Address = portName,
                        IsPaired = true
                    });
                }
                else
                {
                    logger.LogDebug("{PortName} responded but not with device info (response code: {ResponseCode:X2})",
                        portName, deviceQueryResponse.Payload?.FirstOrDefault() ?? 0);
                }
            }
            catch (DeviceConnectionException ex)
            {
                logger.LogDebug("Connection error for {PortName}: {Message}", portName, ex.Message);
            }
            catch (MeshCoreTimeoutException)
            {
                logger.LogDebug("Timeout for {PortName} - likely not a MeshCore device", portName);
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogDebug("Access denied for {PortName}: {Message}", portName, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogDebug("Error testing {PortName}: {ExceptionType} - {Message}", portName, ex.GetType().Name, ex.Message);
            }
        }

        logger.LogDeviceDiscoveryCompleted(devices.Count, "USB");
        MeshCoreSdkEventSource.Log.DeviceDiscoveryCompleted(devices.Count, "USB");

        return devices;
    }

    /// <summary>
    /// Releases all resources used by the transport
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Disconnect();
            _serialPort?.Dispose();
            _writeLock?.Dispose();
            _cancellationTokenSource?.Dispose();
            _disposed = true;
        }
    }
}