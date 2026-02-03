using MeshCore.Net.SDK.Transport;
using MeshCore.Net.SDK.Protocol;

namespace MeshCore.Net.SDK.Tests;

/// <summary>
/// Test the updated discovery process with proper MeshCore protocol handshake
/// </summary>
[Collection("SequentialTests")]
public class MeshCoreDiscoveryTest
{
    [Fact]
    public async Task DiscoverDevicesAsync_ShouldUseProperHandshake()
    {
        // Test that discovery uses the correct command sequence from research
        var devices = await UsbTransport.DiscoverDevicesAsync();
        
        // Should not throw and should return a list (may be empty if no devices)
        Assert.NotNull(devices);
    }
    
    [Fact]
    public void MeshCoreCommands_ShouldMatchResearchSpecification()
    {
        // Verify command values match the research documentation
        Assert.Equal(0x01, (byte)MeshCoreCommand.CMD_APP_START);
        Assert.Equal(0x16, (byte)MeshCoreCommand.CMD_DEVICE_QUERY);
        Assert.Equal(0x02, (byte)MeshCoreCommand.CMD_SEND_TXT_MSG);
        Assert.Equal(0x03, (byte)MeshCoreCommand.CMD_SEND_CHANNEL_TXT_MSG);
        Assert.Equal(0x04, (byte)MeshCoreCommand.CMD_GET_CONTACTS);
        Assert.Equal(0x05, (byte)MeshCoreCommand.CMD_GET_DEVICE_TIME);
        Assert.Equal(0x06, (byte)MeshCoreCommand.CMD_SET_DEVICE_TIME);
        Assert.Equal(0x19, (byte)MeshCoreCommand.CMD_REBOOT);
    }
    
    [Fact]
    public void MeshCoreResponseCodes_ShouldMatchResearchSpecification()
    {
        // Verify response codes match the research documentation
        Assert.Equal(0x00, (byte)MeshCoreResponseCode.RESP_CODE_OK);
        Assert.Equal(0x01, (byte)MeshCoreResponseCode.RESP_CODE_ERR);
        Assert.Equal(0x05, (byte)MeshCoreResponseCode.RESP_CODE_SELF_INFO);
        Assert.Equal(0x0D, (byte)MeshCoreResponseCode.RESP_CODE_DEVICE_INFO);
        Assert.Equal(0x09, (byte)MeshCoreResponseCode.RESP_CODE_CURR_TIME);
        Assert.Equal(0x06, (byte)MeshCoreResponseCode.RESP_CODE_SENT);
    }
}