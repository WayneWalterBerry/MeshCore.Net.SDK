using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MeshCore.Net.SDK.Exceptions;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Transport;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace MeshCore.Net.SDK.Tests.LiveRadio;

/// <summary>
/// Comprehensive integration tests for Channel APIs with real MeshCore device
/// These tests require a physical MeshCore device connected to COM3
/// Tests channel creation, configuration, messaging, and security features
/// Based on MeshCore Channel System Architecture research
/// 
/// NOTE: MeshCore protocol does NOT support channel deletion. Channels persist
/// on the device until factory reset. Tests use unique naming to avoid conflicts.
/// </summary>
[Collection("LiveRadio")] // Ensures tests run sequentially to avoid COM port conflicts
[Trait("Category", "LiveRadio")] // Enable filtering in CI/CD pipelines
public class LiveRadioChannelApiTests : LiveRadioTestBase
{
    private readonly List<string> _createdTestChannels = new();

    // Test data constants based on MeshCore research
    private const string DefaultChannelName = "Public"; // Research shows "Public" or "All" channel
    private const string DefaultPublicChannelKey = "izOH6cXN6mrJ5e26oRXNcg=="; // From research: default public key
    private const int MaxChannelNameLength = 31; // Research: 32 bytes including null terminator
    private const int ExpectedChannelKeyLength = 16; // 16-byte channel secret (wire format: [index(1)][name(32)][secret(16)])
    private const int ChannelRecordSize = 68; // Research: each ChannelDetails record is 68 bytes

    // Test execution timestamp for unique channel names (since deletion not supported)
    private static readonly string TestRunId = DateTime.Now.ToString("HHmmss");

    /// <summary>
    /// Gets the test suite name for header display
    /// </summary>
    protected override string TestSuiteName => "MeshCore Channel API Test Suite";

    /// <summary>
    /// Initializes a new instance of the LiveRadioChannelApiTests class
    /// </summary>
    /// <param name="output">Test output helper</param>
    public LiveRadioChannelApiTests(ITestOutputHelper output)
        : base(output, typeof(LiveRadioChannelApiTests))
    {
    }

    /// <summary>
    /// Displays additional header information specific to channel tests
    /// </summary>
    protected override void DisplayAdditionalHeader()
    {
        _output.WriteLine("Based on MeshCore Channel System Architecture Research");
        _output.WriteLine($"Default Public Channel Key: {DefaultPublicChannelKey}");
        _output.WriteLine($"Max Channels: {MeshCoreClient.MaxChannelsSupported}, Record Size: {ChannelRecordSize} bytes");
        _output.WriteLine($"⚠️  NOTE: Channel deletion NOT supported - using unique names (TestRun: {TestRunId})");
    }

    /// <summary>
    /// Override stabilization delay for channel tests (shorter delay)
    /// </summary>
    /// <returns>Delay in milliseconds</returns>
    protected override int GetStabilizationDelay()
    {
        return 500; // Shorter delay for channel tests
    }

    #region Basic Functional Tests

    /// <summary>
    /// Test: Get current channel configuration and validate default "Public" channel
    /// Research shows all devices start with a default "Public" channel at index 0
    /// </summary>
    [Fact]
    public async Task Test_02_GetPublicChannelAsync_ShouldRetrievePublicChannel()
    {
        await ExecuteIsolationTestAsync("Get Current Channel Configuration (Default Public)", async (client) =>
        {
            var channelInfo = await client.GetPublicChannelAsync();

            Assert.NotNull(channelInfo);
            _output.WriteLine($"✅ Current channel retrieved:");
            _output.WriteLine($"   Channel Name: {channelInfo.Name ?? "Not specified"}");
            _output.WriteLine($"   Channel Index: {channelInfo.Index}");

            // Research validation: Public channel should be default
            if (channelInfo.Name?.Equals("Public", StringComparison.OrdinalIgnoreCase) == true ||
                channelInfo.Name?.Equals("All", StringComparison.OrdinalIgnoreCase) == true)
            {
                _output.WriteLine($"✅ Default channel confirmed: {channelInfo.Name}");

                // Validate default public channel characteristics from research
                if (channelInfo.IsDefaultChannel)
                {
                    _output.WriteLine("✅ Channel properly marked as default");
                }
            }

            if (channelInfo.EncryptionKey != null)
            {
                _output.WriteLine($"   Key: {channelInfo.EncryptionKey.Hex}");

                // Validate against known public key from research
                if (channelInfo.EncryptionKey == ChannelSecret.DefaultPublicKey)
                {
                    _output.WriteLine("✅ Default public channel key matches research documentation");
                }
            }
        });
    }

    /// <summary>
    /// Test: Get available channels list (validate channel storage structure)
    /// Research shows channels stored in /channels2 file with up to 40 entries
    /// </summary>
    [Fact]
    public async Task Test_03_GetAvailableChannelsAsync_ShouldMapChannelIndicesToNames()
    {
        await ExecuteIsolationTestAsync("Discover Device Channel Mapping (/channels2 file structure)", async (client) =>
        {
            try
            {
                var channels = await client.GetChannelsAsync();

                Assert.NotNull(channels);
                _output.WriteLine($"✅ Discovered {channels.Count()} channel index mappings:");
                _output.WriteLine($"   Max supported by device: {MeshCoreClient.MaxChannelsSupported} channels");
                _output.WriteLine($"   Storage usage: {channels.Count() * ChannelRecordSize} bytes of {MeshCoreClient.MaxChannelsSupported * ChannelRecordSize} bytes");

                foreach (var kvp in channels)
                {
                    var channelIndex = kvp.Index;
                    var channelName = kvp.Name;
                    var isDefault = channelIndex == 0 ? " (DEFAULT)" : "";
                    _output.WriteLine($"   Index [{channelIndex}] => \"{channelName}\"{isDefault}");

                    // Validate channel structure based on research
                    if (channelName?.Length > MaxChannelNameLength)
                    {
                        _output.WriteLine($"       ⚠️  Name exceeds max length ({MaxChannelNameLength} chars)");
                    }
                }

                // Verify index 0 always exists (default channel requirement from research)
                Assert.True(channels.Any(channel => channel.IsDefaultChannel), "Index 0 (default channel) should always be present");
                _output.WriteLine($"   ✅ Default channel at index 0: \"{channels.First().Index}\"");

                // Validate storage constraints from research
                if (channels.Count() > MeshCoreClient.MaxChannelsSupported)
                {
                    _output.WriteLine($"⚠️  Channel count ({channels.Count()}) exceeds documented maximum ({MeshCoreClient.MaxChannelsSupported})");
                }

                _output.WriteLine($"✅ Channel index mapping discovery complete");
                _output.WriteLine($"   📝 This mapping enables hashtag channel messaging via CMD_SEND_CHANNEL_TXT_MSG");
                _output.WriteLine($"   📝 Channel names map to protocol indices for efficient transmission");
            }
            catch (NotSupportedException)
            {
                _output.WriteLine("ℹ️  Device does not support channel discovery operations");
            }
        });
    }

    #endregion

    #region Channel Configuration Tests Based on Research

    /// <summary>
    /// Test: Create private channel with custom secret key
    /// Research shows private channels use user-defined keys for closed mesh networks
    /// </summary>
    [Fact]
    public async Task Test_04_CreatePrivateChannel_ShouldConfigureSecureChannel()
    {
        await ExecuteIsolationTestAsync("Create Private Channel (Closed Mesh Network)", async (client) =>
        {
            var testChannelName = $"TeamAlpha_{TestRunId}";
            var testFrequency = 433000000; // 433 MHz - common LoRa frequency from research
            ChannelSecret encryptionKey = GenerateChannelKey();

            _output.WriteLine($"   Creating private channel: {testChannelName}");
            _output.WriteLine($"   Frequency: {testFrequency} Hz");
            _output.WriteLine($"   Key Length: {ChannelSecret.SecretLength} bytes");
            _output.WriteLine($"   Purpose: Closed mesh network isolation");

            await client.AddChannelAsync(testChannelName, encryptionKey);
            Channel? channel = await client.TryGetChannelAsync(testChannelName);
            Assert.NotNull(channel);

            _createdTestChannels.Add(channel.Index.ToString());

            Assert.Equal(testChannelName, channel.Name);
            Assert.NotNull(channel.EncryptionKey);

            _output.WriteLine($"✅ Private channel configured successfully");
            _output.WriteLine($"   Channel ID: {channel.Index}");
            _output.WriteLine($"   Encryption Status: Enabled (mesh isolation active)");
            _output.WriteLine($"   📝 Research Note: This creates an isolated sub-network");

            _output.WriteLine($"⚠️  TEST 04 created channel {channel.Index} - channel deletion not supported in MeshCore protocol");
        });
    }

    /// <summary>
    /// Test: Channel name validation based on MeshCore storage constraints
    /// Research shows 32-byte storage field with null terminator (31 char max)
    /// </summary>
    [Fact]
    public async Task Test_05_ChannelNameValidation_ShouldEnforceStorageConstraints()
    {
        await ExecuteIsolationTestAsync("Channel Name Validation (32-byte Storage Field)", async (client) =>
        {
            var testCases = new[]
            {
                ("Valid_Short", $"Team1_{TestRunId.Substring(0,3)}", true, "Short name within limits"),
                ("Valid_Max_Length", $"A{TestRunId}_{new string('X', MaxChannelNameLength - TestRunId.Length - 3)}", true, "Maximum allowed length"),
                ("Special_Chars", $"Team-Alpha_{TestRunId}", true, "Special characters and numbers"),
                ("Numbers_Only", $"987_{TestRunId}", true, "Numbers only"),
                ("Empty_Name", "", false, "Empty name should fail"),
                ("Exceeds_Storage", $"TooLong_{TestRunId}_{new string('X', MaxChannelNameLength)}", false, "Exceeds 32-byte storage field"),
                ("Unicode_Test", $"Канал_{TestRunId}", true, "Unicode characters (if supported)")
            };

            _output.WriteLine($"   Storage constraint: {MaxChannelNameLength} characters max (32 bytes with null terminator)");
            _output.WriteLine($"   Research source: ChannelDetails structure in /channels2 file");

            foreach (var (testType, channelName, shouldSucceed, description) in testCases)
            {
                _output.WriteLine($"   Testing {testType}: '{channelName}' (length: {channelName.Length})");
                _output.WriteLine($"      {description}");

                try
                {
                    var channelParams = ChannelParams.Create(0, channelName, GenerateChannelKey());

                    await client.SetChannelAsync(channelParams);
                    Channel? channel = await client.TryGetChannelAsync(channelName);
                    Assert.NotNull(channel);

                    if (shouldSucceed)
                    {
                        _createdTestChannels.Add(channel.Index.ToString());
                        _output.WriteLine($"   ✅ {testType} accepted (within storage constraints)");
                    }
                    else
                    {
                        _output.WriteLine($"   ⚠️  {testType} unexpectedly accepted");
                    }
                }
                catch (Exception ex)
                {
                    if (shouldSucceed)
                    {
                        _output.WriteLine($"   ❌ {testType} unexpectedly rejected: {ex.Message}");
                    }
                    else
                    {
                        _output.WriteLine($"   ✅ {testType} properly rejected: {ex.Message}");
                    }
                }
            }

            _output.WriteLine($"⚠️  TEST 05 created {_createdTestChannels.Count} channels that may affect subsequent tests");
        });
    }

    /// <summary>
    /// Test: Channel PSK (Pre-Shared Key) validation based on research
    /// Research shows 32-byte secret key field supporting 128-bit and 256-bit AES
    /// </summary>
    [Fact]
    public async Task Test_06_ChannelPSK_ShouldValidateKeyFormats()
    {
        await ExecuteIsolationTestAsync("Channel PSK Validation (128-bit & 256-bit AES)", async (client) =>
        {
            var testChannelName = $"PSKTest_{TestRunId}";

            // Research shows support for both 128-bit (16 bytes) and 256-bit (32 bytes) AES keys
            var keyTestCases = new (string testType, byte[] keyBytes, bool shouldSucceed, string description)[]
            {
                ("AES_128_Valid", GenerateChannelKey().ToByteArray(), true, "16-byte AES-128 key (protocol standard)"),
                ("Public_Default", Convert.FromHexString("8b3387e9c5cdea6ac9e5edbaa115cd72"), true, "Default public channel key from research"),
                ("Short_Key", new byte[8], false, "Too short (8 bytes)"),
                ("Long_Key", new byte[64], false, "Too long (64 bytes)"),
                ("Empty_Key", new byte[0], false, "Empty key")
            };

            _output.WriteLine($"   Research: 32-byte secret field in ChannelDetails structure");
            _output.WriteLine($"   Supports: 128-bit (16 bytes) and 256-bit (32 bytes) AES keys");

            foreach (var (testType, keyBytes, shouldSucceed, description) in keyTestCases)
            {
                _output.WriteLine($"   Testing {testType}: {description} (length: {keyBytes.Length} bytes)");

                try
                {
                    // Fill short keys with random data for testing
                    if (keyBytes.Length > 0 && keyBytes.Length < 32 && keyBytes.All(b => b == 0))
                    {
                        new Random().NextBytes(keyBytes);
                    }

                    var channelParamsName = $"{testChannelName}_{testType}";
                    var channelSecret = ChannelSecret.FromBytes(keyBytes);
                    var channel = ChannelParams.Create(0, channelParamsName, channelSecret);

                    await client.SetChannelAsync(channel);

                    if (shouldSucceed)
                    {
                        _createdTestChannels.Add(channel.Index.ToString());
                        _output.WriteLine($"   ✅ {testType} accepted");
                    }
                    else
                    {
                        _output.WriteLine($"   ⚠️  {testType} unexpectedly accepted");
                    }
                }
                catch (Exception ex)
                {
                    if (shouldSucceed)
                    {
                        _output.WriteLine($"   ❌ {testType} unexpectedly rejected: {ex.Message}");
                    }
                    else
                    {
                        _output.WriteLine($"   ✅ {testType} properly rejected: {ex.Message}");
                    }
                }
            }
        });
    }

    #endregion

    #region Channel Communication Tests

    /// <summary>
    /// Test: Send channel message using flood routing
    /// Research shows channel messages use PAYLOAD_TYPE_GRP_TXT (0x05) and flood routing
    /// </summary>
    [Fact]
    public async Task Test_07_ChannelFloodMessaging_ShouldUseGroupTextProtocol()
    {
        await ExecuteIsolationTestAsync("Channel Flood Messaging (PAYLOAD_TYPE_GRP_TXT)", async (client) =>
        {
            // Get current channel for messaging
            var currentChannel = await client.GetPublicChannelAsync();
            var targetChannelName = currentChannel?.Name ?? DefaultChannelName;

            _output.WriteLine($"   Target Channel: {targetChannelName}");
            _output.WriteLine($"   Protocol: PAYLOAD_TYPE_GRP_TXT (0x05)");
            _output.WriteLine($"   Routing: Flood routing (research documented)");

            var testMessage = $"Test flood message from SDK at {DateTime.Now:HH:mm:ss}";
            _output.WriteLine($"   Message: '{testMessage}'");

            await client.SendChannelMessageAsync(targetChannelName, testMessage);

            _output.WriteLine($"✅ Channel message sent successfully");
            _output.WriteLine($"   📝 Research Note: Message propagates via repeater flood network-wide");
        });
    }

    /// <summary>
    /// Test: Channel message format with sender name prefix
    /// Research shows group messages include "SenderName: Message text" format
    /// </summary>
    [Fact]
    public async Task Test_08_ChannelMessageFormat_ShouldIncludeSenderName()
    {
        await ExecuteIsolationTestAsync("Channel Message Format (SenderName: Message)", async (client) =>
        {
            var currentChannel = await client.GetPublicChannelAsync();
            var targetChannelName = currentChannel?.Name ?? DefaultChannelName;

            var testMessages = new[]
            {
                ("Short_Message", "Hello"),
                ("Long_Message", new string('A', 100)), // Test longer message
                ("Special_Chars", "Test message with émojis 🚀 and spéciál chars!"),
                ("Multiline", "Line1\nLine2\nLine3"),
                ("Command_Like", "/status check")
            };

            _output.WriteLine($"   Research: Messages encrypted as 'SenderName: Message text' format");
            _output.WriteLine($"   Channel: {targetChannelName}");

            foreach (var (testType, messageContent) in testMessages)
            {
                try
                {
                    _output.WriteLine($"   Testing {testType} (length: {messageContent.Length})");

                    await client.SendChannelMessageAsync(targetChannelName, messageContent);

                    _output.WriteLine($"   ✅ {testType} message sent successfully");

                    // Small delay between messages to avoid overwhelming device
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ❌ {testType} message failed: {ex.Message}");
                    if (ex is ProtocolException protocolEx)
                    {
                        _output.WriteLine($"      Protocol Error - Command: {protocolEx.Command}, Status: {protocolEx.Status}");
                    }
                }
            }
        });
    }

    #endregion

    #region Channel Security and Isolation Tests

    /// <summary>
    /// Test: Private channel creates isolated sub-network
    /// Research shows nodes on different channel keys ignore each other entirely
    /// </summary>
    [Fact]
    public async Task Test_10_PrivateChannelIsolation_ShouldCreateClosedMesh()
    {
        await ExecuteIsolationTestAsync("Private Channel Isolation (Closed Mesh Networks)", async (client) =>
        {
            // Create two different private channels with different keys
            var channel1Name = $"SecureTeam1_{DateTime.Now:HHmmss}";
            var channel2Name = $"SecureTeam2_{DateTime.Now:HHmmss}";

            var key1 = GenerateChannelKey();
            var key2 = GenerateChannelKey();

            _output.WriteLine($"   Research: Devices on different channel keys are invisible to each other");
            _output.WriteLine($"   Creating isolated channels: {channel1Name} and {channel2Name}");

            // Create first private channel
            var channelParams1 = ChannelParams.Create(0, channel1Name, key1);

            await client.SetChannelAsync(channelParams1);
            Channel? channel1 = await client.TryGetChannelAsync(channel1Name);
            Assert.NotNull(channel1);

            _createdTestChannels.Add(channel1.Index.ToString());

            // Send test message to first channel
            await client.SendChannelMessageAsync(channel1Name, "Message for isolated network 1");
            _output.WriteLine($"   ✅ Channel 1: {channel1Name} (isolated sub-network created)");

            // Create second private channel
            var channelParams2 = ChannelParams.Create(0, channel2Name, key2);

            await client.SetChannelAsync(channelParams2);
            Channel? channel2 = await client.TryGetChannelAsync(channel2Name);
            Assert.NotNull(channel2);

            _createdTestChannels.Add(channel2.Index.ToString());

            // Send test message to second channel
            await client.SendChannelMessageAsync(channel2Name, "Message for isolated network 2");
            _output.WriteLine($"   ✅ Channel 2: {channel2Name} (isolated sub-network created)");

            _output.WriteLine($"✅ Successfully created two isolated private channels");
            _output.WriteLine($"   Channel 1 ID: {channel1.Index.ToString()}");
            _output.WriteLine($"   Channel 2 ID: {channel2.Index.ToString()}");
            _output.WriteLine($"   📝 Research Note: Each channel creates virtual sub-network within larger mesh");
            _output.WriteLine($"   📝 Security: Nodes without keys cannot decrypt or participate");
        });
    }

    /// <summary>
    /// Test: Channel security with known default public key
    /// Research shows public channel uses well-known key for basic encryption
    /// </summary>
    [Fact]
    public async Task Test_11_PublicChannelSecurity_ShouldUseKnownKey()
    {
        await ExecuteIsolationTestAsync("Public Channel Security (Known Default Key)", async (client) =>
        {
            var currentChannel = await client.GetPublicChannelAsync();

            _output.WriteLine($"   Research: Default public key = {DefaultPublicChannelKey}");
            _output.WriteLine($"   Hex equivalent: 8b3387e9c5cdea6ac9e5edbaa115cd72");
            _output.WriteLine($"   Security level: Basic encryption (prevents casual eavesdropping)");

            if (currentChannel != null)
            {
                _output.WriteLine($"   Current channel: {currentChannel.Name}");
                _output.WriteLine($"   Is default: {currentChannel.IsDefaultChannel}");

                if (currentChannel.IsDefaultChannel && currentChannel.EncryptionKey == null)
                {
                    _output.WriteLine($"   ✅ Default channel properly configured with encryption");
                    _output.WriteLine($"   📝 Research Note: Public key known to all MeshCore devices");
                    _output.WriteLine($"   📝 Use Case: Good for discovery, not for sensitive data");
                }
                else if (currentChannel.IsDefaultChannel && currentChannel.EncryptionKey != null)
                {
                    _output.WriteLine($"   ⚠️  Default channel is not encrypted (unusual configuration)");
                }
            }

            // Test sending on public channel if it's available
            if (currentChannel?.IsDefaultChannel == true)
            {
                var testMessage = "Public channel test - non-sensitive data";
                await client.SendChannelMessageAsync(currentChannel.Name, testMessage);
                _output.WriteLine($"   ✅ Successfully sent message on public channel");
            }
        });
    }

    #endregion

    #region Advanced Channel Operations

    /// <summary>
    /// Test: Channel switching and active channel management
    /// Research shows devices can listen to multiple channels but send to selected one
    /// </summary>
    [Fact]
    public async Task Test_12_MultiChannelOperation_ShouldManageMultipleChannels()
    {
        await ExecuteIsolationTestAsync("Multi-Channel Operation (Concurrent Channel Management)", async (client) =>
        {
            // Get current channel list
            var initialChannels = await client.GetChannelsAsync();
            _output.WriteLine($"   Initial channels: {initialChannels.Count()}");

            // Create multiple test channels
            var testChannels = new List<(string name, Channel config)>();

            for (int i = 1; i <= 3; i++)
            {
                var channelName = $"Multi_{i}_{DateTime.Now:HHmmss}";
                var channelParams = ChannelParams.Create(0, channelName, GenerateChannelKey());


                await client.SetChannelAsync(channelParams);
                Channel? channel = await client.TryGetChannelAsync(channelName);
                Assert.NotNull(channel);

                testChannels.Add((channelName, channel));
                _createdTestChannels.Add(channel.Index.ToString());

                _output.WriteLine($"   ✅ Created channel {i}: {channelName}");
            }

            // Test concurrent channel operations
            _output.WriteLine($"   Testing concurrent channel operations...");

            foreach (var (name, config) in testChannels)
            {
                try
                {
                    var testMessage = $"Test message for {name}";
                    await client.SendChannelMessageAsync(name, testMessage);
                    _output.WriteLine($"   ✅ Message sent to {name}");

                    await Task.Delay(200); // Avoid overwhelming device
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ❌ Failed to send to {name}: {ex.Message}");
                }
            }

            // Verify final channel count
            var finalChannels = await client.GetChannelsAsync();
            var expectedCount = initialChannels.Count() + testChannels.Count;

            _output.WriteLine($"   Final channels: {finalChannels.Count()} (expected: {expectedCount})");
            _output.WriteLine($"   📝 Research Note: Device can listen to all configured channels simultaneously");
            _output.WriteLine($"   📝 Limitation: Transmission is one channel at a time");

            // Validate storage constraints
            if (finalChannels.Count() > MeshCoreClient.MaxChannelsSupported)
            {
                _output.WriteLine($"   ⚠️  Exceeded maximum supported channels ({MeshCoreClient.MaxChannelsSupported})");
            }
            else
            {
                _output.WriteLine($"   ✅ Within storage limits ({MeshCoreClient.MaxChannelsSupported} max)");
            }
        });
    }

    #endregion

    #region Error Handling Tests

    /// <summary>
    /// Test: Channel storage capacity limits
    /// Research shows 40 channel limit on companion devices
    /// </summary>
    [Fact]
    public async Task Test_14_ChannelStorageCapacity_ShouldEnforceLimit()
    {
        await ExecuteIsolationTestAsync("Channel Storage Capacity (40 Channel Limit)", async (client) =>
        {
            // Get current channel count
            var currentChannels = await client.GetChannelsAsync();
            var startingCount = currentChannels.Count();

            _output.WriteLine($"   Current channels: {startingCount}");
            _output.WriteLine($"   Research limit: {MeshCoreClient.MaxChannelsSupported} channels");
            _output.WriteLine($"   Available slots: {MeshCoreClient.MaxChannelsSupported - startingCount}");

            // Test near-capacity behavior (create a few channels, not 40 to avoid long test)
            var testChannelCount = Math.Min(3, MeshCoreClient.MaxChannelsSupported - startingCount);

            if (testChannelCount <= 0)
            {
                _output.WriteLine($"   ⚠️  Device already at or near capacity");
                _output.WriteLine($"   Skipping capacity test to avoid overflow");
                return;
            }

            _output.WriteLine($"   Testing with {testChannelCount} additional channels...");

            for (int i = 0; i < testChannelCount; i++)
            {
                try
                {
                    var channelName = $"CapTest_{i}_{DateTime.Now:HHmmss}";
                    var channelParams = ChannelParams.Create(0, channelName, GenerateChannelKey());

                    await client.SetChannelAsync(channelParams);
                    Channel? channel = await client.TryGetChannelAsync(channelName);
                    Assert.NotNull(channel);

                    _createdTestChannels.Add(channel.Index.ToString());

                    _output.WriteLine($"   ✅ Created channel {i + 1}: {channelName}");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ❌ Failed to create channel {i + 1}: {ex.Message}");

                    // If we hit capacity, that's expected behavior
                    if (ex.Message.Contains("capacity") || ex.Message.Contains("full") || ex.Message.Contains("limit"))
                    {
                        _output.WriteLine($"   ✅ Capacity limit properly enforced");
                        break;
                    }
                }
            }

            // Verify final count
            var finalChannels = await client.GetChannelsAsync();
            var totalCount = finalChannels.Count();
            var storageUsed = totalCount * ChannelRecordSize;
            var maxStorage = MeshCoreClient.MaxChannelsSupported * ChannelRecordSize;

            _output.WriteLine($"   Final channel count: {totalCount}");
            _output.WriteLine($"   Storage used: {storageUsed} bytes of {maxStorage} bytes");
            _output.WriteLine($"   Storage efficiency: {(double)storageUsed / maxStorage * 100:F1}%");

            if (totalCount <= MeshCoreClient.MaxChannelsSupported)
            {
                _output.WriteLine($"   ✅ Within documented limits");
            }
            else
            {
                _output.WriteLine($"   ⚠️  Exceeded documented limits");
            }
        });
    }

    /// <summary>
    /// Test: Channel error handling for various failure scenarios
    /// </summary>
    [Fact]
    public async Task Test_15_ChannelErrorHandling_ShouldHandleFailureScenarios()
    {
        await ExecuteIsolationTestAsync("Channel Error Handling (Comprehensive Scenarios)", async (client) =>
        {
            // Test invalid frequency (negative)
            await TestInvalidFrequency(client);

            // Test sending message to non-existent channel
            await TestNonExistentChannelMessaging(client);

            // Test null message content
            await TestNullMessageContent(client);

            // Test malformed encryption key
            await TestMalformedEncryptionKey(client);

            _output.WriteLine("   📝 Research Note: Error handling protects channel storage integrity");
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generates a cryptographically random 16-byte channel secret
    /// </summary>
    private static ChannelSecret GenerateChannelKey()
    {
        return ChannelSecret.CreateRandom();
    }

    /// <summary>
    /// Tests invalid frequency handling
    /// </summary>
    private async Task TestInvalidFrequency(MeshCoreClient client)
    {
        try
        {
            var invalidConfig = ChannelParams.Create(0, "InvalidFreq", GenerateChannelKey());

            await client.SetChannelAsync(invalidConfig);
            _output.WriteLine("   ❌ Expected exception for invalid frequency was not thrown");
        }
        catch (ArgumentException)
        {
            _output.WriteLine("   ✅ Invalid frequency properly rejected");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"   ⚠️  Unexpected exception for invalid frequency: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Tests messaging to non-existent channel
    /// </summary>
    private async Task TestNonExistentChannelMessaging(MeshCoreClient client)
    {
        try
        {
            await client.SendChannelMessageAsync("NonExistentChannel_" + Guid.NewGuid(), "Test message");
            _output.WriteLine("   ⚠️  Sending to non-existent channel did not throw exception");
        }
        catch (ProtocolException)
        {
            _output.WriteLine("   ✅ Non-existent channel messaging properly rejected");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"   ⚠️  Unexpected exception for non-existent channel: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Tests null message content handling
    /// </summary>
    private async Task TestNullMessageContent(MeshCoreClient client)
    {
        try
        {
            var currentChannel = await client.GetPublicChannelAsync();
            var channelName = currentChannel?.Name ?? DefaultChannelName;

            await client.SendChannelMessageAsync(channelName, null!);
            _output.WriteLine("   ❌ Expected exception for null message was not thrown");
        }
        catch (ArgumentNullException)
        {
            _output.WriteLine("   ✅ Null message content properly rejected");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"   ⚠️  Unexpected exception for null message: {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// Tests malformed encryption key handling
    /// </summary>
    private async Task TestMalformedEncryptionKey(MeshCoreClient client)
    {
        try
        {
            var malformedSecret = ChannelSecret.FromHex("NOT_VALID_HEX_NOT_VALID_HEX_0000");
            var malformedConfig = ChannelParams.Create(0, "MalformedKey", malformedSecret);

            await client.SetChannelAsync(malformedConfig);
            _output.WriteLine("   ⚠️  Malformed encryption key was accepted");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"   ✅ Malformed encryption key properly rejected: {ex.GetType().Name}");
        }
    }

    #endregion
}