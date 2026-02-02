using System.Text;

namespace MeshCore.Net.SDK.Protocol;

/// <summary>
/// Represents a MeshCore protocol frame
/// </summary>
public class MeshCoreFrame
{
    public byte StartByte { get; set; }
    public ushort Length { get; set; }
    public byte[] Payload { get; set; } = Array.Empty<byte>();
    
    public bool IsInbound => StartByte == ProtocolConstants.FRAME_START_INBOUND;
    public bool IsOutbound => StartByte == ProtocolConstants.FRAME_START_OUTBOUND;
    
    /// <summary>
    /// Creates a new inbound frame (PC to radio)
    /// </summary>
    public static MeshCoreFrame CreateInbound(byte[] payload)
    {
        return new MeshCoreFrame
        {
            StartByte = ProtocolConstants.FRAME_START_INBOUND,
            Length = (ushort)payload.Length,
            Payload = payload
        };
    }
    
    /// <summary>
    /// Creates a new outbound frame (radio to PC)
    /// </summary>
    public static MeshCoreFrame CreateOutbound(byte[] payload)
    {
        return new MeshCoreFrame
        {
            StartByte = ProtocolConstants.FRAME_START_OUTBOUND,
            Length = (ushort)payload.Length,
            Payload = payload
        };
    }
    
    /// <summary>
    /// Converts the frame to a byte array for transmission
    /// </summary>
    public byte[] ToByteArray()
    {
        var result = new List<byte>
        {
            StartByte,
            (byte)(Length & 0xFF),        // Little-endian low byte
            (byte)((Length >> 8) & 0xFF)  // Little-endian high byte
        };
        result.AddRange(Payload);
        return result.ToArray();
    }
    
    /// <summary>
    /// Parses a byte array into a frame
    /// </summary>
    public static MeshCoreFrame? Parse(byte[] data)
    {
        if (data.Length < ProtocolConstants.FRAME_HEADER_SIZE)
            return null;
            
        var startByte = data[0];
        if (startByte != ProtocolConstants.FRAME_START_INBOUND && 
            startByte != ProtocolConstants.FRAME_START_OUTBOUND)
            return null;
            
        var length = (ushort)(data[1] | (data[2] << 8)); // Little-endian
        
        if (data.Length < ProtocolConstants.FRAME_HEADER_SIZE + length)
            return null;
            
        var payload = new byte[length];
        Array.Copy(data, ProtocolConstants.FRAME_HEADER_SIZE, payload, 0, length);
        
        return new MeshCoreFrame
        {
            StartByte = startByte,
            Length = length,
            Payload = payload
        };
    }
    
    /// <summary>
    /// Gets the command from the payload (first byte)
    /// </summary>
    public MeshCoreCommand? GetCommand()
    {
        if (Payload.Length == 0)
            return null;
            
        if (Enum.IsDefined(typeof(MeshCoreCommand), Payload[0]))
            return (MeshCoreCommand)Payload[0];
            
        return null;
    }
    
    /// <summary>
    /// Gets the status from the payload (second byte for responses)
    /// </summary>
    public MeshCoreStatus? GetStatus()
    {
        if (Payload.Length < 2)
            return null;
            
        if (Enum.IsDefined(typeof(MeshCoreStatus), Payload[1]))
            return (MeshCoreStatus)Payload[1];
            
        return null;
    }
    
    /// <summary>
    /// Gets the data payload (excluding command and status bytes)
    /// </summary>
    public byte[] GetDataPayload()
    {
        if (Payload.Length <= 2)
            return Array.Empty<byte>();
            
        var data = new byte[Payload.Length - 2];
        Array.Copy(Payload, 2, data, 0, data.Length);
        return data;
    }
    
    public override string ToString()
    {
        var direction = IsInbound ? "?" : "?";
        var command = GetCommand()?.ToString() ?? "Unknown";
        var status = GetStatus()?.ToString() ?? "N/A";
        return $"{direction} {command} ({Payload.Length} bytes) Status: {status}";
    }
}