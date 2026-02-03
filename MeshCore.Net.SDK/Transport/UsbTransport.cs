using System.IO.Ports;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK.Transport;

/// <summary>
/// Handles low-level USB serial communication with MeshCore devices
/// </summary>
public class UsbTransport : ITransport
{
    private readonly SerialPort _serialPort;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;

    public event EventHandler<MeshCoreFrame>? FrameReceived;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _serialPort?.IsOpen == true;
    public string? ConnectionId => _serialPort?.PortName;

    public UsbTransport(string portName, int baudRate = 115200)
    {
        _serialPort = new SerialPort(portName, baudRate)
        {
            DataBits = 8,
            Parity = Parity.None,
            StopBits = StopBits.One,
            Handshake = Handshake.None,
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };
    }

    /// <summary>
    /// Opens the connection to the MeshCore device
    /// </summary>
    public async Task ConnectAsync()
    {
        try
        {
            _serialPort.Open();
            
            // Start the receive loop
            _ = Task.Run(ReceiveLoop, _cancellationTokenSource.Token);
            
            // Wait a moment for the connection to stabilize
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            throw new DeviceConnectionException(ConnectionId, ex);
        }
    }

    /// <summary>
    /// Closes the connection to the MeshCore device
    /// </summary>
    public void Disconnect()
    {
        try
        {
            _cancellationTokenSource.Cancel();
            _serialPort?.Close();
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// Sends a frame to the MeshCore device
    /// </summary>
    public async Task SendFrameAsync(MeshCoreFrame frame
)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Transport is not connected");

        var frameBytes = frame.ToByteArray();
        
        await _writeLock.WaitAsync();
        try
        {
            await _serialPort.BaseStream.WriteAsync(frameBytes, 0, frameBytes.Length);
            await _serialPort.BaseStream.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>
    /// Sends a command and waits for a response
    /// </summary>
    public async Task<MeshCoreFrame> SendCommandAsync(MeshCoreCommand command, byte[]? data = null, 
        TimeSpan? timeout = null)
    {
        data ??= Array.Empty<byte>();
        timeout ??= TimeSpan.FromMilliseconds(ProtocolConstants.DEFAULT_TIMEOUT_MS);

        // Build payload: command byte + data
        var payload = new byte[1 + data.Length];
        payload[0] = (byte)command;
        Array.Copy(data, 0, payload, 1, data.Length);

        var frame = MeshCoreFrame.CreateInbound(payload);
        
        var responseTask = WaitForResponseAsync(command, timeout.Value);
        await SendFrameAsync(frame);
        
        return await responseTask;
    }

    /// <summary>
    /// Waits for a response frame with the specified command
    /// </summary>
    private async Task<MeshCoreFrame> WaitForResponseAsync(MeshCoreCommand expectedCommand, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<MeshCoreFrame>();
        var timer = new Timer(_ => tcs.TrySetException(new MeshCoreTimeoutException(timeout)), 
            null, timeout, Timeout.InfiniteTimeSpan);

        void OnFrameReceived(object? sender, MeshCoreFrame frame)
        {
            // Accept any outbound frame as a response - the device may respond with a different command
            // or status frame rather than echoing the original command
            if (frame.IsOutbound)
            {
                timer.Dispose();
                FrameReceived -= OnFrameReceived;
                tcs.TrySetResult(frame);
            }
        }

        FrameReceived += OnFrameReceived;

        try
        {
            return await tcs.Task;
        }
        catch
        {
            FrameReceived -= OnFrameReceived;
            timer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Background task that continuously reads frames from the serial port
    /// </summary>
    private async Task ReceiveLoop()
    {
        var buffer = new byte[ProtocolConstants.MAX_FRAME_SIZE];
        var frameBuffer = new List<byte>();
        
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

                for (int i = 0; i < bytesRead; i++)
                {
                    frameBuffer.Add(buffer[i]);
                    
                    // Try to parse a complete frame
                    if (TryParseFrame(frameBuffer, out var frame))
                    {
                        FrameReceived?.Invoke(this, frame);
                        frameBuffer.Clear();
                    }
                    else if (frameBuffer.Count > ProtocolConstants.MAX_FRAME_SIZE)
                    {
                        // Buffer too large, clear it to prevent memory issues
                        frameBuffer.Clear();
                        ErrorOccurred?.Invoke(this, new FrameParseException("Frame buffer overflow"));
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                ErrorOccurred?.Invoke(this, ex);
                await Task.Delay(1000, _cancellationTokenSource.Token); // Wait before retry
            }
        }
    }

    /// <summary>
    /// Tries to parse a complete frame from the buffer
    /// </summary>
    private static bool TryParseFrame(List<byte> buffer, out MeshCoreFrame? frame)
    {
        frame = null;
        
        if (buffer.Count < ProtocolConstants.FRAME_HEADER_SIZE)
            return false;

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
            buffer.Clear();
            return false;
        }

        if (startIndex > 0)
        {
            // Remove bytes before start byte
            buffer.RemoveRange(0, startIndex);
        }

        if (buffer.Count < ProtocolConstants.FRAME_HEADER_SIZE)
            return false;

        // Parse length
        var length = (ushort)(buffer[1] | (buffer[2] << 8));
        var totalFrameSize = ProtocolConstants.FRAME_HEADER_SIZE + length;

        if (buffer.Count < totalFrameSize)
            return false; // Not enough data yet

        // Extract frame data
        var frameData = buffer.Take(totalFrameSize).ToArray();
        buffer.RemoveRange(0, totalFrameSize);

        frame = MeshCoreFrame.Parse(frameData);
        return frame != null;
    }

    /// <summary>
    /// Discovers available MeshCore devices on serial ports
    /// </summary>
    public static async Task<List<MeshCoreDevice>> DiscoverDevicesAsync()
    {
        var devices = new List<MeshCoreDevice>();
        var portNames = SerialPort.GetPortNames();

        foreach (var portName in portNames)
        {
            Console.WriteLine($"Testing port {portName}...");
            
            try
            {
                using var transport = new UsbTransport(portName);
                
                // Try to connect with timeout
                var connectTask = transport.ConnectAsync();
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                {
                    Console.WriteLine($"  Connection timeout for {portName}");
                    continue;
                }
                
                await connectTask; // This will throw if connection failed
                Console.WriteLine($"  Connected to {portName}");
                
                // According to research, try CMD_APP_START first for older firmware compatibility
                var appStartResponse = await transport.SendCommandAsync(
                    MeshCoreCommand.CMD_APP_START, 
                    new byte[] { 0x08 }, // Protocol version 8
                    timeout: TimeSpan.FromMilliseconds(3000));
                
                // Now try device query
                var deviceQueryResponse = await transport.SendCommandAsync(
                    MeshCoreCommand.CMD_DEVICE_QUERY, 
                    new byte[] { 0x08 }, // Protocol version 8
                    timeout: TimeSpan.FromMilliseconds(3000));
                
                // Check if we got a valid device info response (RESP_CODE_DEVICE_INFO = 0x0D)
                if (deviceQueryResponse.Payload?.Length > 0 && deviceQueryResponse.Payload[0] == 0x0D)
                {
                    Console.WriteLine($"  {portName} is a MeshCore device!");
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
                    Console.WriteLine($"  {portName} responded but not with device info");
                }
            }
            catch (DeviceConnectionException ex)
            {
                Console.WriteLine($"  Connection error for {portName}: {ex.Message}");
            }
            catch (MeshCoreTimeoutException ex)
            {
                Console.WriteLine($"  Timeout for {portName}: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"  Access denied for {portName}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Error for {portName}: {ex.GetType().Name} - {ex.Message}");
            }
        }

        Console.WriteLine($"Discovery complete. Found {devices.Count} MeshCore devices.");
        return devices;
    }

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