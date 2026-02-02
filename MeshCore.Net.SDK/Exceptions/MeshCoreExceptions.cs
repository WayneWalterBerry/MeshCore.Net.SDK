namespace MeshCore.Net.SDK.Exceptions;

/// <summary>
/// Base exception for all MeshCore SDK exceptions
/// </summary>
public class MeshCoreException : Exception
{
    public MeshCoreException() { }
    public MeshCoreException(string message) : base(message) { }
    public MeshCoreException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when a device connection fails
/// </summary>
public class DeviceConnectionException : MeshCoreException
{
    public string? PortName { get; }
    
    public DeviceConnectionException(string? portName) : base($"Failed to connect to device on port {portName}")
    {
        PortName = portName;
    }
    
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
    public byte? Command { get; }
    public byte? Status { get; }
    
    public ProtocolException(string message) : base(message) { }
    
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
    public TimeSpan Timeout { get; }
    
    public MeshCoreTimeoutException(TimeSpan timeout) : base($"Operation timed out after {timeout.TotalSeconds:F1} seconds")
    {
        Timeout = timeout;
    }
    
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
    public byte[]? RawData { get; }
    
    public FrameParseException(string message) : base(message) { }
    
    public FrameParseException(string message, byte[] rawData) : base(message)
    {
        RawData = rawData;
    }
}