namespace MeshCore.Net.SDK.Exceptions;

/// <summary>
/// Base exception for all MeshCore SDK exceptions
/// </summary>
public class MeshCoreException : Exception
{
    /// <summary>
    /// Initializes a new instance of the MeshCoreException class
    /// </summary>
    public MeshCoreException() { }
    
    /// <summary>
    /// Initializes a new instance of the MeshCoreException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public MeshCoreException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the MeshCoreException class with a specified error message and inner exception
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public MeshCoreException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a device connection fails
/// </summary>
public class DeviceConnectionException : MeshCoreException
{
    /// <summary>
    /// Gets the port name that failed to connect
    /// </summary>
    public string? PortName { get; }
    
    /// <summary>
    /// Initializes a new instance of the DeviceConnectionException class for the specified port
    /// </summary>
    /// <param name="portName">The name of the port that failed to connect</param>
    public DeviceConnectionException(string? portName) : base($"Failed to connect to device on port {portName}")
    {
        PortName = portName;
    }
    
    /// <summary>
    /// Initializes a new instance of the DeviceConnectionException class for the specified port with an inner exception
    /// </summary>
    /// <param name="portName">The name of the port that failed to connect</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public DeviceConnectionException(string? portName, Exception innerException) 
        : base($"Failed to connect to device on port {portName}", innerException)
    {
        PortName = portName;
    }
}

/// <summary>
/// Exception thrown when a protocol operation fails
/// </summary>
public class ProtocolException : MeshCoreException
{
    /// <summary>
    /// Gets the command that failed
    /// </summary>
    public byte? Command { get; }
    
    /// <summary>
    /// Gets the status code returned
    /// </summary>
    public byte? Status { get; }
    
    /// <summary>
    /// Initializes a new instance of the ProtocolException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public ProtocolException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the ProtocolException class with command and status information
    /// </summary>
    /// <param name="command">The command that failed</param>
    /// <param name="status">The status code returned</param>
    /// <param name="message">Additional error message</param>
    public ProtocolException(byte command, byte status, string message) 
        : base($"Command {command} failed with status {status}: {message}")
    {
        Command = command;
        Status = status;
    }
}

/// <summary>
/// Exception thrown when an operation times out
/// </summary>
public class MeshCoreTimeoutException : MeshCoreException
{
    /// <summary>
    /// Gets the timeout duration that was exceeded
    /// </summary>
    public TimeSpan Timeout { get; }
    
    /// <summary>
    /// Initializes a new instance of the MeshCoreTimeoutException class with a timeout duration
    /// </summary>
    /// <param name="timeout">The timeout duration that was exceeded</param>
    public MeshCoreTimeoutException(TimeSpan timeout) : base($"Operation timed out after {timeout.TotalSeconds:F1} seconds")
    {
        Timeout = timeout;
    }
    
    /// <summary>
    /// Initializes a new instance of the MeshCoreTimeoutException class with a timeout duration and operation name
    /// </summary>
    /// <param name="timeout">The timeout duration that was exceeded</param>
    /// <param name="operation">The name of the operation that timed out</param>
    public MeshCoreTimeoutException(TimeSpan timeout, string operation) 
        : base($"{operation} timed out after {timeout.TotalSeconds:F1} seconds")
    {
        Timeout = timeout;
    }
}

/// <summary>
/// Exception thrown when frame parsing fails
/// </summary>
public class FrameParseException : MeshCoreException
{
    /// <summary>
    /// Gets the raw data that failed to parse
    /// </summary>
    public byte[]? RawData { get; }
    
    /// <summary>
    /// Initializes a new instance of the FrameParseException class with a specified error message
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    public FrameParseException(string message) : base(message) { }
    
    /// <summary>
    /// Initializes a new instance of the FrameParseException class with error message and raw data
    /// </summary>
    /// <param name="message">The message that describes the error</param>
    /// <param name="rawData">The raw data that failed to parse</param>
    public FrameParseException(string message, byte[] rawData) : base(message)
    {
        RawData = rawData;
    }
}