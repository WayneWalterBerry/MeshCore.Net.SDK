using MeshCore.Net.SDK;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK.Tests;

public class MeshCoreFrameTests
{
    [Fact]
    public void CreateInbound_ShouldCreateCorrectFrame()
    {
        // Arrange
        var payload = new byte[] { 0x16, 0x01, 0x02 }; // CMD_DEVICE_QUERY + data
        
        // Act
        var frame = MeshCoreFrame.CreateInbound(payload);
        
        // Assert
        Assert.Equal(ProtocolConstants.FRAME_START_INBOUND, frame.StartByte);
        Assert.Equal((ushort)payload.Length, frame.Length);
        Assert.Equal(payload, frame.Payload);
        Assert.True(frame.IsInbound);
        Assert.False(frame.IsOutbound);
    }
    
    [Fact]
    public void CreateOutbound_ShouldCreateCorrectFrame()
    {
        // Arrange
        var payload = new byte[] { 0x16, 0x00, 0x01, 0x02 }; // CMD_DEVICE_QUERY + success + data
        
        // Act
        var frame = MeshCoreFrame.CreateOutbound(payload);
        
        // Assert
        Assert.Equal(ProtocolConstants.FRAME_START_OUTBOUND, frame.StartByte);
        Assert.Equal((ushort)payload.Length, frame.Length);
        Assert.Equal(payload, frame.Payload);
        Assert.False(frame.IsInbound);
        Assert.True(frame.IsOutbound);
    }
    
    [Fact]
    public void ToByteArray_ShouldCreateCorrectFrameBytes()
    {
        // Arrange
        var payload = new byte[] { 0x16, 0x01, 0x02 };
        var frame = MeshCoreFrame.CreateInbound(payload);
        
        // Act
        var bytes = frame.ToByteArray();
        
        // Assert
        Assert.Equal(6, bytes.Length); // 1 start + 2 length + 3 payload
        Assert.Equal(ProtocolConstants.FRAME_START_INBOUND, bytes[0]);
        Assert.Equal(3, bytes[1]); // Length low byte
        Assert.Equal(0, bytes[2]); // Length high byte
        Assert.Equal(0x16, bytes[3]); // Command
        Assert.Equal(0x01, bytes[4]); // Data
        Assert.Equal(0x02, bytes[5]); // Data
    }
    
    [Fact]
    public void Parse_ShouldParseValidFrame()
    {
        // Arrange
        var frameBytes = new byte[] { 0x3E, 0x04, 0x00, 0x16, 0x00, 0x01, 0x02 };
        
        // Act
        var frame = MeshCoreFrame.Parse(frameBytes);
        
        // Assert
        Assert.NotNull(frame);
        Assert.Equal(ProtocolConstants.FRAME_START_OUTBOUND, frame.StartByte);
        Assert.Equal(4, frame.Length);
        Assert.Equal(4, frame.Payload.Length);
        Assert.Equal(0x16, frame.Payload[0]);
        Assert.Equal(0x00, frame.Payload[1]);
    }
    
    [Fact]
    public void Parse_ShouldReturnNull_ForInvalidFrame()
    {
        // Arrange
        var invalidBytes = new byte[] { 0xFF, 0x04, 0x00, 0x16 }; // Invalid start byte
        
        // Act
        var frame = MeshCoreFrame.Parse(invalidBytes);
        
        // Assert
        Assert.Null(frame);
    }
    
    [Fact]
    public void GetCommand_ShouldReturnCorrectCommand()
    {
        // Arrange
        var payload = new byte[] { (byte)MeshCoreCommand.CMD_DEVICE_QUERY, 0x00 };
        var frame = MeshCoreFrame.CreateOutbound(payload);
        
        // Act
        var command = frame.GetCommand();
        
        // Assert
        Assert.Equal(MeshCoreCommand.CMD_DEVICE_QUERY, command);
    }
    
    [Fact]
    public void GetStatus_ShouldReturnCorrectStatus()
    {
        // Arrange - Create an error response frame
        var payload = new byte[] { (byte)MeshCoreResponseCode.RESP_CODE_ERR, (byte)MeshCoreStatus.InvalidCommand };
        var frame = MeshCoreFrame.CreateOutbound(payload);
        
        // Act
        var status = frame.GetStatus();
        
        // Assert
        Assert.Equal(MeshCoreStatus.InvalidCommand, status);
    }
    
    [Fact]
    public void GetStatus_ShouldReturnSuccess_ForOkResponse()
    {
        // Arrange - Create an OK response frame
        var payload = new byte[] { (byte)MeshCoreResponseCode.RESP_CODE_OK };
        var frame = MeshCoreFrame.CreateOutbound(payload);
        
        // Act
        var status = frame.GetStatus();
        
        // Assert
        Assert.Equal(MeshCoreStatus.Success, status);
    }
    
    [Fact]
    public void GetDataPayload_ShouldReturnDataWithoutCommandAndStatus()
    {
        // Arrange
        var payload = new byte[] { (byte)MeshCoreCommand.CMD_DEVICE_QUERY, (byte)MeshCoreStatus.Success, 0x01, 0x02, 0x03 };
        var frame = MeshCoreFrame.CreateOutbound(payload);
        
        // Act
        var data = frame.GetDataPayload();
        
        // Assert
        Assert.Equal(3, data.Length);
        Assert.Equal(0x01, data[0]);
        Assert.Equal(0x02, data[1]);
        Assert.Equal(0x03, data[2]);
    }
}

public class MeshCoreCommandTests
{
    [Fact]
    public void CommandEnums_ShouldHaveCorrectValues()
    {
        // Test command values match research specifications  
        Assert.Equal(0x16, (byte)MeshCoreCommand.CMD_DEVICE_QUERY);
        Assert.Equal(0x01, (byte)MeshCoreCommand.CMD_APP_START);
        Assert.Equal(0x02, (byte)MeshCoreCommand.CMD_SEND_TXT_MSG);
    }
    
    [Fact]
    public void StatusEnums_ShouldHaveCorrectValues()
    {
        Assert.Equal(0x00, (byte)MeshCoreStatus.Success);
        Assert.Equal(0x01, (byte)MeshCoreStatus.InvalidCommand);
        Assert.Equal(0xFF, (byte)MeshCoreStatus.UnknownError);
    }
}

public class MeshCoreModelsTests
{
   
    [Fact]
    public void Message_ShouldInitializeWithDefaults()
    {
        // Act
        var message = new Message();
        
        // Assert
        Assert.Equal(string.Empty, message.FromContactId);
        Assert.Equal(string.Empty, message.Content);
        Assert.True(message.IsTextMessage);
    }
    
    [Fact]
    public void DeviceInfo_ShouldInitializeCorrectly()
    {
        // Act
        var deviceInfo = new DeviceInfo
        {
            DeviceId = "TEST123",
            FirmwareVersion = "1.0.0",
            BatteryLevel = 85,
            IsConnected = true
        };
        
        // Assert
        Assert.Equal("TEST123", deviceInfo.DeviceId);
        Assert.Equal("1.0.0", deviceInfo.FirmwareVersion);
        Assert.Equal(85, deviceInfo.BatteryLevel);
        Assert.True(deviceInfo.IsConnected);
    }
}

public class MeshCoreExceptionTests
{
    [Fact]
    public void DeviceConnectionException_ShouldIncludePortName()
    {
        // Arrange & Act
        var ex = new DeviceConnectionException("COM5");
        
        // Assert
        Assert.Contains("COM5", ex.Message);
        Assert.Equal("COM5", ex.PortName);
    }
    
    [Fact]
    public void ProtocolException_ShouldIncludeCommandAndStatus()
    {
        // Arrange & Act
        var ex = new ProtocolException(22, 1, "Test error");
        
        // Assert
        Assert.Contains("22", ex.Message);
        Assert.Contains("1", ex.Message);
        Assert.Contains("Test error", ex.Message);
        Assert.Equal((byte)22, ex.Command);
        Assert.Equal((byte)1, ex.Status);
    }
    
    [Fact]
    public void MeshCoreTimeoutException_ShouldIncludeTimeout()
    {
        // Arrange & Act
        var timeout = TimeSpan.FromSeconds(5);
        var ex = new MeshCoreTimeoutException(timeout);
        
        // Assert
        Assert.Contains("5", ex.Message);
        Assert.Equal(timeout, ex.Timeout);
    }
}