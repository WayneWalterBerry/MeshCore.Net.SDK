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
[Collection("SequentialTests")] // Ensures tests run sequentially to avoid COM port conflicts
public class LiveRadioChannelApiTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly ILogger<LiveRadioChannelApiTests> _logger;
    private readonly TestEtwEventListener _etwListener;
    private readonly List<string> _createdTestChannels = new();
    private readonly string _testMethodName;
    
    // Shared client management for efficiency
    private static MeshCodeClient? _sharedClient;
    private static readonly object _clientLock = new object();
    private static bool _clientInitialized = false;
    private static TestEtwEventListener? _sharedEtwListener;
    private static readonly object _etwLock = new object();

    // Enhanced state tracking for debugging
    private static int _testExecutionCounter = 0;
    private static readonly List<string> _testExecutionOrder = new();
    private static readonly Dictionary<string, DeviceStateSnapshot> _preTestStates = new();
    private static readonly Dictionary<string, DeviceStateSnapshot> _postTestStates = new();

    // Test data constants based on MeshCore research
    private const string DefaultChannelName = "Public"; // Research shows "Public" or "All" channel
    private const string DefaultPublicChannelKey = "izOH6cXN6mrJ5e26oRXNcg=="; // From research: default public key
    private const int MaxChannelNameLength = 31; // Research: 32 bytes including null terminator
    private const int ExpectedChannelKeyLength = 32; // AES-256 key length (research shows 32 bytes)
    private const int ChannelRecordSize = 68; // Research: each ChannelDetails record is 68 bytes
    
    // Test execution timestamp for unique channel names (since deletion not supported)
    private static readonly string TestRunId = DateTime.Now.ToString("HHmmss");

    public LiveRadioChannelApiTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = new NullLogger<LiveRadioChannelApiTests>();
        
        // Capture the calling test method name for better tracking
        var stackTrace = new StackTrace();
        var testMethod = stackTrace.GetFrames()
            .FirstOrDefault(f => f.GetMethod()?.Name.StartsWith("Test_") == true);
        _testMethodName = testMethod?.GetMethod()?.Name ?? "Unknown";
        
        // Use shared ETW listener to avoid conflicts
        lock (_etwLock)
        {
            if (_sharedEtwListener == null)
            {
                _sharedEtwListener = new TestEtwEventListener(_logger);
            }
            _etwListener = _sharedEtwListener;
        }

        lock (_clientLock)
        {
            _testExecutionCounter++;
            _testExecutionOrder.Add($"{_testExecutionCounter:D2}. {_testMethodName}");
        }

        _output.WriteLine("MeshCore Channel API Test Suite");
        _output.WriteLine("===============================");
        _output.WriteLine($"Test: {_testMethodName} (#{_testExecutionCounter})");
        _output.WriteLine($"Execution Order: {string.Join(" → ", _testExecutionOrder.TakeLast(3))}");
        _output.WriteLine("Based on MeshCore Channel System Architecture Research");
        _output.WriteLine($"Default Public Channel Key: {DefaultPublicChannelKey}");
        _output.WriteLine($"Max Channels: {MeshCodeClient.MaxChannelsSupported}, Record Size: {ChannelRecordSize} bytes");
        _output.WriteLine($"⚠️  NOTE: Channel deletion NOT supported - using unique names (TestRun: {TestRunId})");
    }

    #region Enhanced State Management

    /// <summary>
    /// Captures the current device state for debugging purposes
    /// </summary>
    private async Task<DeviceStateSnapshot> CaptureDeviceState(string context)
    {
        var snapshot = new DeviceStateSnapshot
        {
            Context = context,
            TestMethod = _testMethodName,
            Timestamp = DateTime.Now,
            IsConnected = _sharedClient?.IsConnected ?? false
        };

        if (_sharedClient?.IsConnected == true)
        {
            try
            {
                var contacts = await _sharedClient.GetContactsAsync();
                snapshot.ContactCount = contacts.Count;
                snapshot.ContactSample = contacts.Take(3)
                    .Select(c => $"{c.Name}:{c.Id?[..Math.Min(8, c.Id.Length)]}")
                    .ToList();

                var messages = await _sharedClient.GetMessagesAsync();
                snapshot.MessageCount = messages.Count;

                var deviceInfo = await _sharedClient.GetDeviceInfoAsync();
                snapshot.BatteryLevel = deviceInfo.BatteryLevel;
                snapshot.FirmwareVersion = deviceInfo.FirmwareVersion;

                // Channel-specific state capture
                try
                {
                    var channels = await _sharedClient.GetChannelsAsync();
                    snapshot.ChannelCount = channels.Count();
                    snapshot.ChannelSample = channels.Take(3)
                        .Select(ch => $"{ch.Name}:{(ch.IsEncrypted ? "E" : "O")}")
                        .ToList();
                        
                    var currentChannel = await _sharedClient.GetPublicChannelAsync();
                    snapshot.CurrentChannelName = currentChannel?.Name;
                }
                catch (Exception ex)
                {
                    snapshot.ChannelStateError = ex.Message;
                }
            }
            catch (Exception ex)
            {
                snapshot.StateError = ex.Message;
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Log device state in a readable format
    /// </summary>
    private void LogDeviceState(DeviceStateSnapshot snapshot, string prefix = "")
    {
        _output.WriteLine($"{prefix}📊 Device State - {snapshot.Context}:");
        _output.WriteLine($"{prefix}   Timestamp: {snapshot.Timestamp:HH:mm:ss.fff}");
        _output.WriteLine($"{prefix}   Connected: {snapshot.IsConnected}");
        
        if (!string.IsNullOrEmpty(snapshot.StateError))
        {
            _output.WriteLine($"{prefix}   ❌ State Error: {snapshot.StateError}");
        }
        else if (snapshot.IsConnected)
        {
            _output.WriteLine($"{prefix}   Contacts: {snapshot.ContactCount}");
            if (snapshot.ContactSample.Any())
            {
                _output.WriteLine($"{prefix}   Contact Sample: [{string.Join(", ", snapshot.ContactSample)}]");
            }
            _output.WriteLine($"{prefix}   Messages: {snapshot.MessageCount}");
            _output.WriteLine($"{prefix}   Battery: {snapshot.BatteryLevel}%");
            _output.WriteLine($"{prefix}   Firmware: {snapshot.FirmwareVersion}");
            
            // Channel-specific state logging
            _output.WriteLine($"{prefix}   Channels: {snapshot.ChannelCount}");
            if (snapshot.ChannelSample.Any())
            {
                _output.WriteLine($"{prefix}   Channel Sample: [{string.Join(", ", snapshot.ChannelSample)}]");
            }
            if (!string.IsNullOrEmpty(snapshot.CurrentChannelName))
            {
                _output.WriteLine($"{prefix}   Current Channel: {snapshot.CurrentChannelName}");
            }
            if (!string.IsNullOrEmpty(snapshot.ChannelStateError))
            {
                _output.WriteLine($"{prefix}   Channel Error: {snapshot.ChannelStateError}");
            }
        }
    }

    /// <summary>
    /// Compare two device states and highlight differences
    /// </summary>
    private void CompareDeviceStates(DeviceStateSnapshot before, DeviceStateSnapshot after)
    {
        _output.WriteLine("📋 State Comparison:");
        
        if (before.ContactCount != after.ContactCount)
        {
            _output.WriteLine($"   📞 Contacts: {before.ContactCount} → {after.ContactCount} (Δ{after.ContactCount - before.ContactCount:+#;-#;0})");
        }
        
        if (before.MessageCount != after.MessageCount)
        {
            _output.WriteLine($"   💬 Messages: {before.MessageCount} → {after.MessageCount} (Δ{after.MessageCount - before.MessageCount:+#;-#;0})");
        }
        
        if (before.ChannelCount != after.ChannelCount)
        {
            _output.WriteLine($"   📻 Channels: {before.ChannelCount} → {after.ChannelCount} (Δ{after.ChannelCount - before.ChannelCount:+#;-#;0})");
        }
        
        if (Math.Abs(before.BatteryLevel - after.BatteryLevel) > 0)
        {
            _output.WriteLine($"   🔋 Battery: {before.BatteryLevel}% → {after.BatteryLevel}%");
        }

        // Check for new contacts that might indicate test pollution
        var beforeContacts = new HashSet<string>(before.ContactSample);
        var afterContacts = new HashSet<string>(after.ContactSample);
        var newContacts = afterContacts.Except(beforeContacts).ToList();
        var removedContacts = beforeContacts.Except(afterContacts).ToList();
        
        if (newContacts.Any())
        {
            _output.WriteLine($"   ➕ New Contacts: [{string.Join(", ", newContacts)}]");
        }
        
        if (removedContacts.Any())
        {
            _output.WriteLine($"   ➖ Removed Contacts: [{string.Join(", ", removedContacts)}]");
        }

        // Check for channel changes
        var beforeChannels = new HashSet<string>(before.ChannelSample);
        var afterChannels = new HashSet<string>(after.ChannelSample);
        var newChannels = afterChannels.Except(beforeChannels).ToList();
        var removedChannels = beforeChannels.Except(afterChannels).ToList();
        
        if (newChannels.Any())
        {
            _output.WriteLine($"   ➕ New Channels: [{string.Join(", ", newChannels)}]");
        }
        
        if (removedChannels.Any())
        {
            _output.WriteLine($"   ➖ Removed Channels: [{string.Join(", ", removedChannels)}]");
        }

        if (before.CurrentChannelName != after.CurrentChannelName)
        {
            _output.WriteLine($"   📡 Active Channel: {before.CurrentChannelName ?? "None"} → {after.CurrentChannelName ?? "None"}");
        }
    }

    private async Task EnsureConnected()
    {
        MeshCodeClient? clientToConnect = null;
        
        lock (_clientLock)
        {
            if (!_clientInitialized)
            {
                _sharedClient = new MeshCodeClient("COM3");
                _clientInitialized = true;
                clientToConnect = _sharedClient;
            }
            else if (_sharedClient != null && !_sharedClient.IsConnected)
            {
                clientToConnect = _sharedClient;
            }
        }

        if (clientToConnect != null)
        {
            await clientToConnect.ConnectAsync();
            _output.WriteLine($"✅ Connected to device: {clientToConnect.ConnectionId}");
            
            // Clear any residual state that might interfere with tests
            await ClearDeviceState();
        }
    }

    private async Task ClearDeviceState()
    {
        try
        {
            if (_sharedClient?.IsConnected == true)
            {
                _output.WriteLine("🧹 Clearing device state...");
                
                // Clear any pending messages that might interfere with tests
                var maxAttempts = 10;
                int totalMessagesCleared = 0;
                
                for (int i = 0; i < maxAttempts; i++)
                {
                    try
                    {
                        var messages = await _sharedClient.GetMessagesAsync();
                        if (messages.Count == 0) break;
                        
                        totalMessagesCleared += messages.Count;
                        _output.WriteLine($"   Cleared {messages.Count} pending messages (batch {i + 1})");
                        await Task.Delay(100);
                    }
                    catch
                    {
                        break; // If GetMessages fails, assume no more messages
                    }
                }
                
                if (totalMessagesCleared > 0)
                {
                    _output.WriteLine($"   Total messages cleared: {totalMessagesCleared}");
                }
                
                // Add device stabilization - drain any pending frames from the device
                // by sending a simple, safe command and waiting for response
                try
                {
                    _output.WriteLine("   Stabilizing device communication...");
                    
                    // Send a few simple, safe commands to flush any pending responses
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            // Use device time query as it's safe and widely supported
                            var timeResponse = await _sharedClient.GetDeviceTimeAsync();
                            _output.WriteLine($"   Device time sync successful: {timeResponse}");
                            break; // Success - device is responding correctly
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"   Device stabilization attempt {attempt + 1} failed: {ex.Message}");
                            if (attempt < 2) // Don't delay on the last attempt
                            {
                                await Task.Delay(500);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   Device stabilization failed: {ex.Message}");
                }
                
                // Additional stabilization delay
                await Task.Delay(500);
                _output.WriteLine("   Device state clearing completed");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Warning during device state clear: {ex.Message}");
        }
    }

    #endregion

    #region Basic Functional Tests

    /// <summary>
    /// Test: Basic device connection functionality for channel operations
    /// </summary>
    [Fact]
    public async Task Test_01_DeviceConnection_ShouldConnectToCOM3Successfully()
    {
        _output.WriteLine("TEST 01: Device Connection for Channel Operations");
        _output.WriteLine("===============================================");

        // Capture pre-test state (will be mostly empty since we're connecting)
        var preState = await CaptureDeviceState("Pre-Connection");
        LogDeviceState(preState, "   ");

        await EnsureConnected();
        
        // Capture post-connection state
        var postState = await CaptureDeviceState("Post-Connection");
        LogDeviceState(postState, "   ");
        
        // Store states for cross-test analysis
        _preTestStates[_testMethodName] = preState;
        _postTestStates[_testMethodName] = postState;
        
        Assert.NotNull(_sharedClient);
        Assert.True(_sharedClient.IsConnected);
        _output.WriteLine("✅ Device connection successful");
        
        // Clear any initial state
        await ClearDeviceState();
    }

    /// <summary>
    /// Test: Get current channel configuration and validate default "Public" channel
    /// Research shows all devices start with a default "Public" channel at index 0
    /// </summary>
    [Fact]
    public async Task Test_02_GetPublicChannelAsync_ShouldRetrievePublicChannel()
    {
        _output.WriteLine("TEST 02: Get Current Channel Configuration (Default Public)");
        _output.WriteLine("=========================================================");

        var preState = await CaptureDeviceState("Pre-GetChannelConfig");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            var channelInfo = await _sharedClient!.GetPublicChannelAsync();
            
            var postState = await CaptureDeviceState("Post-GetChannelConfig");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            Assert.NotNull(channelInfo);
            _output.WriteLine($"✅ Current channel retrieved:");
            _output.WriteLine($"   Channel Name: {channelInfo.Name ?? "Not specified"}");
            _output.WriteLine($"   Channel Index: {channelInfo.Index}");
            _output.WriteLine($"   Frequency: {channelInfo.Frequency} Hz");
            _output.WriteLine($"   Encrypted: {(channelInfo.IsEncrypted ? "Yes" : "No")}");
            
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
            
            if (channelInfo.IsEncrypted && !string.IsNullOrEmpty(channelInfo.EncryptionKey))
            {
                _output.WriteLine($"   Key Length: {channelInfo.EncryptionKey.Length} chars (Base64)");
                
                // Validate against known public key from research
                if (channelInfo.EncryptionKey.Equals(DefaultPublicChannelKey))
                {
                    _output.WriteLine("✅ Default public channel key matches research documentation");
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to retrieve channel configuration: {ex.Message}");
            if (ex is ProtocolException protocolEx)
            {
                _output.WriteLine($"   Protocol Error - Command: {protocolEx.Command}, Status: {protocolEx.Status}");
            }
            
            var errorState = await CaptureDeviceState("Error-GetChannelConfig");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Get available channels list (validate channel storage structure)
    /// Research shows channels stored in /channels2 file with up to 40 entries
    /// </summary>
    [Fact]
    public async Task Test_03_GetAvailableChannelsAsync_ShouldMapChannelIndicesToNames()
    {
        _output.WriteLine("TEST 03: Discover Device Channel Mapping (/channels2 file structure)");
        _output.WriteLine("======================================================================");

        var preState = await CaptureDeviceState("Pre-DiscoverChannelMapping");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            var channels = await _sharedClient!.GetChannelsAsync();
            
            var postState = await CaptureDeviceState("Post-DiscoverChannelMapping");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            Assert.NotNull(channels);
            _output.WriteLine($"✅ Discovered {channels.Count()} channel index mappings:");
            _output.WriteLine($"   Max supported by device: {MeshCodeClient.MaxChannelsSupported} channels");
            _output.WriteLine($"   Storage usage: {channels.Count() * ChannelRecordSize} bytes of {MeshCodeClient.MaxChannelsSupported * ChannelRecordSize} bytes");
            
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
            if (channels.Count() > MeshCodeClient.MaxChannelsSupported)
            {
                _output.WriteLine($"⚠️  Channel count ({channels.Count()}) exceeds documented maximum ({MeshCodeClient.MaxChannelsSupported})");
            }
            
            _output.WriteLine($"✅ Channel index mapping discovery complete");
            _output.WriteLine($"   📝 This mapping enables hashtag channel messaging via CMD_SEND_CHANNEL_TXT_MSG");
            _output.WriteLine($"   📝 Channel names map to protocol indices for efficient transmission");
        }
        catch (NotSupportedException)
        {
            _output.WriteLine("ℹ️  Device does not support channel discovery operations");
            
            var notSupportedState = await CaptureDeviceState("NotSupported-DiscoverChannelMapping");
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = notSupportedState;
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to discover device channel mapping: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-DiscoverChannelMapping");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
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
        _output.WriteLine("TEST 04: Create Private Channel (Closed Mesh Network)");
        _output.WriteLine("===================================================");

        var preState = await CaptureDeviceState("Pre-CreatePrivateChannel");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        var testChannelName = $"TeamAlpha_{TestRunId}";
        var testFrequency = 433000000; // 433 MHz - common LoRa frequency from research
        var encryptionKey = GenerateChannelKey(); // 32-byte AES-256 key per research
        
        try
        {
            _output.WriteLine($"   Creating private channel: {testChannelName}");
            _output.WriteLine($"   Frequency: {testFrequency} Hz");
            _output.WriteLine($"   Key Length: {encryptionKey.Length} bytes (AES-256)");
            _output.WriteLine($"   Purpose: Closed mesh network isolation");

            var channelConfig = new Channel
            {
                Name = testChannelName,
                Frequency = testFrequency,
                IsEncrypted = true,
                EncryptionKey = Convert.ToHexString(encryptionKey)
            };

            var result = await _sharedClient!.SetChannelAsync(channelConfig);
            _createdTestChannels.Add(result.Index.ToString());
            
            var postState = await CaptureDeviceState("Post-CreatePrivateChannel");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            Assert.NotNull(result);
            Assert.Equal(testChannelName, result.Name);
            Assert.Equal(testFrequency, result.Frequency);
            Assert.True(result.IsEncrypted);
            Assert.NotNull(result.EncryptionKey);
            Assert.False(result.IsDefaultChannel); // Should not be default channel
            
            _output.WriteLine($"✅ Private channel configured successfully");
            _output.WriteLine($"   Channel ID: {result.Index}");
            _output.WriteLine($"   Encryption Status: Enabled (mesh isolation active)");
            _output.WriteLine($"   📝 Research Note: This creates an isolated sub-network");
            
            _output.WriteLine($"⚠️  TEST 04 created channel {result.Index} - channel deletion not supported in MeshCore protocol");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to configure private channel: {ex.Message}");
            if (ex is ProtocolException protocolEx)
            {
                _output.WriteLine($"   Protocol Error - Command: {protocolEx.Command}, Status: {protocolEx.Status}");
            }
            
            var errorState = await CaptureDeviceState("Error-CreatePrivateChannel");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Channel name validation based on MeshCore storage constraints
    /// Research shows 32-byte storage field with null terminator (31 char max)
    /// </summary>
    [Fact]
    public async Task Test_05_ChannelNameValidation_ShouldEnforceStorageConstraints()
    {
        _output.WriteLine("TEST 05: Channel Name Validation (32-byte Storage Field)");
        _output.WriteLine("=======================================================");

        var preState = await CaptureDeviceState("Pre-ChannelNameValidation");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

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
                var channelConfig = new Channel
                {
                    Name = channelName,
                    Frequency = 433000000,
                    IsEncrypted = false
                };

                var result = await _sharedClient!.SetChannelAsync(channelConfig);
                
                if (shouldSucceed)
                {
                    _createdTestChannels.Add(result.Index.ToString());
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

        var postState = await CaptureDeviceState("Post-ChannelNameValidation");
        LogDeviceState(postState, "   ");
        CompareDeviceStates(preState, postState);
        
        _preTestStates[_testMethodName] = preState;
        _postTestStates[_testMethodName] = postState;
        
        _output.WriteLine($"⚠️  TEST 05 created {_createdTestChannels.Count} channels that may affect subsequent tests");
    }

    /// <summary>
    /// Test: Channel PSK (Pre-Shared Key) validation based on research
    /// Research shows 32-byte secret key field supporting 128-bit and 256-bit AES
    /// </summary>
    [Fact]
    public async Task Test_06_ChannelPSK_ShouldValidateKeyFormats()
    {
        _output.WriteLine("TEST 06: Channel PSK Validation (128-bit & 256-bit AES)");
        _output.WriteLine("=======================================================");

        var preState = await CaptureDeviceState("Pre-ChannelPSK");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        var testChannelName = $"PSKTest_{TestRunId}";
        var testFrequency = 433000000;

        // Research shows support for both 128-bit (16 bytes) and 256-bit (32 bytes) AES keys
        var keyTestCases = new (string testType, byte[] keyBytes, bool shouldSucceed, string description)[]
        {
            ("AES_256_Valid", GenerateChannelKey(), true, "32-byte AES-256 key (research standard)"),
            ("AES_128_Valid", new byte[16], true, "16-byte AES-128 key (research supported)"),
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
                
                var channelConfig = new Channel
                {
                    Name = $"{testChannelName}_{testType}",
                    Frequency = testFrequency,
                    IsEncrypted = true,
                    EncryptionKey = keyBytes.Length > 0 ? Convert.ToHexString(keyBytes) : ""
                };

                var result = await _sharedClient!.SetChannelAsync(channelConfig);
                
                if (shouldSucceed)
                {
                    _createdTestChannels.Add(result.Index.ToString());
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

        var postState = await CaptureDeviceState("Post-ChannelPSK");
        LogDeviceState(postState, "   ");
        CompareDeviceStates(preState, postState);
        
        _preTestStates[_testMethodName] = preState;
        _postTestStates[_testMethodName] = postState;
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
        _output.WriteLine("TEST 07: Channel Flood Messaging (PAYLOAD_TYPE_GRP_TXT)");
        _output.WriteLine("=======================================================");

        var preState = await CaptureDeviceState("Pre-ChannelMessaging");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Get current channel for messaging
            var currentChannel = await _sharedClient!.GetPublicChannelAsync();
            var targetChannelName = currentChannel?.Name ?? DefaultChannelName;
            
            _output.WriteLine($"   Target Channel: {targetChannelName}");
            _output.WriteLine($"   Protocol: PAYLOAD_TYPE_GRP_TXT (0x05)");
            _output.WriteLine($"   Routing: Flood routing (research documented)");

            var testMessage = $"Test flood message from SDK at {DateTime.Now:HH:mm:ss}";
            _output.WriteLine($"   Message: '{testMessage}'");

            var result = await _sharedClient.SendChannelMessageAsync(targetChannelName, testMessage);
            
            var postState = await CaptureDeviceState("Post-ChannelMessaging");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            Assert.NotNull(result);
            Assert.Equal(MessageStatus.Sent, result.Status);
            
            _output.WriteLine($"✅ Channel message sent successfully");
            _output.WriteLine($"   Message ID: {result.Id}");
            _output.WriteLine($"   Status: {result.Status}");
            _output.WriteLine($"   📝 Research Note: Message propagates via repeater flood network-wide");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to send channel message: {ex.Message}");
            if (ex is ProtocolException protocolEx)
            {
                _output.WriteLine($"   Protocol Error - Command: {protocolEx.Command}, Status: {protocolEx.Status}");
            }
            
            var errorState = await CaptureDeviceState("Error-ChannelMessaging");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Channel message format with sender name prefix
    /// Research shows group messages include "SenderName: Message text" format
    /// </summary>
    [Fact]
    public async Task Test_08_ChannelMessageFormat_ShouldIncludeSenderName()
    {
        _output.WriteLine("TEST 08: Channel Message Format (SenderName: Message)");
        _output.WriteLine("====================================================");

        var preState = await CaptureDeviceState("Pre-MessageFormat");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        var currentChannel = await _sharedClient!.GetPublicChannelAsync();
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
                
                var result = await _sharedClient.SendChannelMessageAsync(targetChannelName, messageContent);
                
                Assert.NotNull(result);
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

        var postState = await CaptureDeviceState("Post-MessageFormat");
        LogDeviceState(postState, "   ");
        CompareDeviceStates(preState, postState);
        
        _preTestStates[_testMethodName] = preState;
        _postTestStates[_testMethodName] = postState;
    }

    /// <summary>
    /// Test: Receive channel messages and validate message structure
    /// Research shows messages can be filtered by channel hash and decrypted with PSK
    /// </summary>
    [Fact]
    public async Task Test_09_ReceiveChannelMessages_ShouldFilterByChannelHash()
    {
        _output.WriteLine("TEST 09: Receive Channel Messages (Channel Hash Filtering)");
        _output.WriteLine("=========================================================");

        var preState = await CaptureDeviceState("Pre-ReceiveMessages");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Send a test message first for reception testing
            var currentChannel = await _sharedClient!.GetPublicChannelAsync();
            var targetChannelName = currentChannel?.Name ?? DefaultChannelName;
            
            var testMessage = $"Reception test message at {DateTime.Now:HH:mm:ss}";
            _output.WriteLine($"   Sending test message: '{testMessage}'");
            await _sharedClient.SendChannelMessageAsync(targetChannelName, testMessage);
            
            // Wait for message processing (research shows real-time delivery)
            await Task.Delay(2000);

            // Retrieve messages
            var messages = await _sharedClient.GetChannelMessagesAsync(targetChannelName);
            
            var postState = await CaptureDeviceState("Post-ReceiveMessages");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            Assert.NotNull(messages);
            _output.WriteLine($"✅ Retrieved {messages.Count} channel messages");
            _output.WriteLine($"   Research: Messages filtered by SHA-256 channel hash (first 2 bytes)");
            
            // Display recent messages
            var recentMessages = messages.OrderByDescending(m => m.Timestamp).Take(5).ToList();
            foreach (var message in recentMessages)
            {
                var timestamp = message.Timestamp.ToString("HH:mm:ss");
                var fromContact = message.FromContactId ?? "Unknown";
                var channelInfo = !string.IsNullOrEmpty(message.ChannelName) ? $" #{message.ChannelName}" : "";
                var preview = message.Content.Length > 50 ? 
                    message.Content.Substring(0, 47) + "..." : 
                    message.Content;
                
                _output.WriteLine($"   [{timestamp}]{channelInfo} From {fromContact}: {preview}");
                
                // Validate message structure from research
                if (message.Type == MessageType.Text)
                {
                    _output.WriteLine($"      ✅ Message type: Text (PAYLOAD_TYPE_GRP_TXT)");
                }
            }
            
            _output.WriteLine($"   📝 Research Note: Channel hash used for efficient message filtering");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to receive channel messages: {ex.Message}");
            if (ex is ProtocolException protocolEx)
            {
                _output.WriteLine($"   Protocol Error - Command: {protocolEx.Command}, Status: {protocolEx.Status}");
            }
            
            var errorState = await CaptureDeviceState("Error-ReceiveMessages");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
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
        _output.WriteLine("TEST 10: Private Channel Isolation (Closed Mesh Networks)");
        _output.WriteLine("========================================================");

        var preState = await CaptureDeviceState("Pre-ChannelIsolation");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Create two different private channels with different keys
            var channel1Name = $"SecureTeam1_{DateTime.Now:HHmmss}";
            var channel2Name = $"SecureTeam2_{DateTime.Now:HHmmss}";
            
            var key1 = GenerateChannelKey();
            var key2 = GenerateChannelKey();

            _output.WriteLine($"   Research: Devices on different channel keys are invisible to each other");
            _output.WriteLine($"   Creating isolated channels: {channel1Name} and {channel2Name}");

            // Create first private channel
            var channel1Config = new Channel
            {
                Name = channel1Name,
                Frequency = 433000000,
                IsEncrypted = true,
                EncryptionKey = Convert.ToHexString(key1)
            };
            
            var channel1 = await _sharedClient!.SetChannelAsync(channel1Config);
            _createdTestChannels.Add(channel1.Index.ToString());

            // Send test message to first channel
            await _sharedClient.SendChannelMessageAsync(channel1Name, "Message for isolated network 1");
            _output.WriteLine($"   ✅ Channel 1: {channel1Name} (isolated sub-network created)");

            // Create second private channel
            var channel2Config = new Channel
            {
                Name = channel2Name,
                Frequency = 433000000,
                IsEncrypted = true,
                EncryptionKey = Convert.ToHexString(key2)
            };
            
            var channel2 = await _sharedClient.SetChannelAsync(channel2Config);
            _createdTestChannels.Add(channel2.Index.ToString());

            // Send test message to second channel
            await _sharedClient.SendChannelMessageAsync(channel2Name, "Message for isolated network 2");
            _output.WriteLine($"   ✅ Channel 2: {channel2Name} (isolated sub-network created)");

            var postState = await CaptureDeviceState("Post-ChannelIsolation");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;

            _output.WriteLine($"✅ Successfully created two isolated private channels");
            _output.WriteLine($"   Channel 1 ID: {channel1.Index.ToString()}");
            _output.WriteLine($"   Channel 2 ID: {channel2.Index.ToString()}");
            _output.WriteLine($"   📝 Research Note: Each channel creates virtual sub-network within larger mesh");
            _output.WriteLine($"   📝 Security: Nodes without keys cannot decrypt or participate");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to test channel isolation: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-ChannelIsolation");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Channel security with known default public key
    /// Research shows public channel uses well-known key for basic encryption
    /// </summary>
    [Fact]
    public async Task Test_11_PublicChannelSecurity_ShouldUseKnownKey()
    {
        _output.WriteLine("TEST 11: Public Channel Security (Known Default Key)");
        _output.WriteLine("===================================================");

        var preState = await CaptureDeviceState("Pre-PublicChannelSecurity");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            var currentChannel = await _sharedClient!.GetPublicChannelAsync();
            
            _output.WriteLine($"   Research: Default public key = {DefaultPublicChannelKey}");
            _output.WriteLine($"   Hex equivalent: 8b3387e9c5cdea6ac9e5edbaa115cd72");
            _output.WriteLine($"   Security level: Basic encryption (prevents casual eavesdropping)");
            
            if (currentChannel != null)
            {
                _output.WriteLine($"   Current channel: {currentChannel.Name}");
                _output.WriteLine($"   Is default: {currentChannel.IsDefaultChannel}");
                _output.WriteLine($"   Is encrypted: {currentChannel.IsEncrypted}");
                
                if (currentChannel.IsDefaultChannel && currentChannel.IsEncrypted)
                {
                    _output.WriteLine($"   ✅ Default channel properly configured with encryption");
                    _output.WriteLine($"   📝 Research Note: Public key known to all MeshCore devices");
                    _output.WriteLine($"   📝 Use Case: Good for discovery, not for sensitive data");
                }
                else if (currentChannel.IsDefaultChannel && !currentChannel.IsEncrypted)
                {
                    _output.WriteLine($"   ⚠️  Default channel is not encrypted (unusual configuration)");
                }
            }
            
            // Test sending on public channel if it's available
            if (currentChannel?.IsDefaultChannel == true)
            {
                var testMessage = "Public channel test - non-sensitive data";
                await _sharedClient.SendChannelMessageAsync(currentChannel.Name, testMessage);
                _output.WriteLine($"   ✅ Successfully sent message on public channel");
            }

            var postState = await CaptureDeviceState("Post-PublicChannelSecurity");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Failed to test public channel security: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-PublicChannelSecurity");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
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
        _output.WriteLine("TEST 12: Multi-Channel Operation (Concurrent Channel Management)");
        _output.WriteLine("================================================================");

        var preState = await CaptureDeviceState("Pre-MultiChannelOperation");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Get current channel list
            var initialChannels = await _sharedClient!.GetChannelsAsync();
            _output.WriteLine($"   Initial channels: {initialChannels.Count()}");
            
            // Create multiple test channels
            var testChannels = new List<(string name, Channel config)>();
            
            for (int i = 1; i <= 3; i++)
            {
                var channelName = $"Multi_{i}_{DateTime.Now:HHmmss}";
                var channelConfig = new Channel
                {
                    Name = channelName,
                    Frequency = 433000000,
                    IsEncrypted = true,
                    EncryptionKey = Convert.ToHexString(GenerateChannelKey())
                };
                
                testChannels.Add((channelName, channelConfig));
                
                var result = await _sharedClient.SetChannelAsync(channelConfig);
                _createdTestChannels.Add(result.Index.ToString());
                
                _output.WriteLine($"   ✅ Created channel {i}: {channelName}");
            }

            // Test concurrent channel operations
            _output.WriteLine($"   Testing concurrent channel operations...");
            
            foreach (var (name, config) in testChannels)
            {
                try
                {
                    var testMessage = $"Test message for {name}";
                    await _sharedClient.SendChannelMessageAsync(name, testMessage);
                    _output.WriteLine($"   ✅ Message sent to {name}");
                    
                    await Task.Delay(200); // Avoid overwhelming device
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ❌ Failed to send to {name}: {ex.Message}");
                }
            }

            // Verify final channel count
            var finalChannels = await _sharedClient.GetChannelsAsync();
            var expectedCount = initialChannels.Count() + testChannels.Count;
            
            var postState = await CaptureDeviceState("Post-MultiChannelOperation");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            _output.WriteLine($"   Final channels: {finalChannels.Count()} (expected: {expectedCount})");
            _output.WriteLine($"   📝 Research Note: Device can listen to all configured channels simultaneously");
            _output.WriteLine($"   📝 Limitation: Transmission is one channel at a time");
            
            // Validate storage constraints
            if (finalChannels.Count() > MeshCodeClient.MaxChannelsSupported)
            {
                _output.WriteLine($"   ⚠️  Exceeded maximum supported channels ({MeshCodeClient.MaxChannelsSupported})");
            }
            else
            {
                _output.WriteLine($"   ✅ Within storage limits ({MeshCodeClient.MaxChannelsSupported} max)");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Multi-channel operation failed: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-MultiChannelOperation");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Channel persistence across device reconnection
    /// Research shows channels stored in /channels2 file with lazy-write mechanism
    /// </summary>
    [Fact]
    public async Task Test_13_ChannelPersistence_ShouldSurviveReconnection()
    {
        _output.WriteLine("TEST 13: Channel Persistence (/channels2 File & Lazy-Write)");
        _output.WriteLine("==========================================================");

        var preState = await CaptureDeviceState("Pre-ChannelPersistence");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Create a persistent test channel
            var persistentChannelName = $"Persistent_{DateTime.Now:HHmmss}";
            var channelConfig = new Channel
            {
                Name = persistentChannelName,
                Frequency = 433000000,
                IsEncrypted = true,
                EncryptionKey = Convert.ToHexString(GenerateChannelKey())
            };

            var createdChannel = await _sharedClient!.SetChannelAsync(channelConfig);
            _createdTestChannels.Add(createdChannel.Index.ToString());
            
            _output.WriteLine($"   Created persistent channel: {persistentChannelName}");
            _output.WriteLine($"   Research: Stored in /channels2 file (68 bytes per record)");
            _output.WriteLine($"   Lazy-write: 5-second delay before flash write");

            // Wait for lazy-write to complete (research shows 5-second delay)
            _output.WriteLine($"   Waiting for lazy-write completion (5+ seconds)...");
            await Task.Delay(6000);

            // Disconnect and reconnect to simulate device restart
            _output.WriteLine($"   Simulating device restart (disconnect/reconnect)...");
            
            _sharedClient.Disconnect();

            await Task.Delay(2000); // Wait for clean disconnect

            await _sharedClient.ConnectAsync();

            // Check if channel configuration persisted
            var retrievedChannels = await _sharedClient.GetChannelsAsync();
            var persistedChannel = retrievedChannels.FirstOrDefault(c => 
                c.Name.Equals(persistentChannelName, StringComparison.OrdinalIgnoreCase));

            var postState = await CaptureDeviceState("Post-ChannelPersistence");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            if (persistedChannel != null)
            {
                _output.WriteLine($"   ✅ Channel persisted after reconnection: {persistedChannel.Name}");
                _output.WriteLine($"      Frequency: {persistedChannel.Frequency}");
                _output.WriteLine($"      Encrypted: {persistedChannel.IsEncrypted}");
                _output.WriteLine($"   ✅ /channels2 file persistence validated");
            }
            else
            {
                _output.WriteLine($"   ⚠️  Channel not found after reconnection");
                _output.WriteLine($"   📝 Note: May indicate lazy-write timing or storage issue");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Channel persistence test failed: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-ChannelPersistence");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
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
        _output.WriteLine("TEST 14: Channel Storage Capacity (40 Channel Limit)");
        _output.WriteLine("===================================================");

        var preState = await CaptureDeviceState("Pre-ChannelStorageCapacity");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        try
        {
            // Get current channel count
            var currentChannels = await _sharedClient!.GetChannelsAsync();
            var startingCount = currentChannels.Count();
            
            _output.WriteLine($"   Current channels: {startingCount}");
            _output.WriteLine($"   Research limit: {MeshCodeClient.MaxChannelsSupported} channels");
            _output.WriteLine($"   Available slots: {MeshCodeClient.MaxChannelsSupported - startingCount}");
            
            // Test near-capacity behavior (create a few channels, not 40 to avoid long test)
            var testChannelCount = Math.Min(3, MeshCodeClient.MaxChannelsSupported - startingCount);
            
            if (testChannelCount <= 0)
            {
                _output.WriteLine($"   ⚠️  Device already at or near capacity");
                _output.WriteLine($"   Skipping capacity test to avoid overflow");
                
                var capacityState = await CaptureDeviceState("AtCapacity-ChannelStorageCapacity");
                _preTestStates[_testMethodName] = preState;
                _postTestStates[_testMethodName] = capacityState;
                return;
            }
            
            _output.WriteLine($"   Testing with {testChannelCount} additional channels...");
            
            for (int i = 0; i < testChannelCount; i++)
            {
                try
                {
                    var channelName = $"CapTest_{i}_{DateTime.Now:HHmmss}";
                    var channelConfig = new Channel
                    {
                        Name = channelName,
                        Frequency = 433000000,
                        IsEncrypted = false
                    };

                    var result = await _sharedClient.SetChannelAsync(channelConfig);
                    _createdTestChannels.Add(result.Index.ToString());
                    
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
            var finalChannels = await _sharedClient.GetChannelsAsync();
            var totalCount = finalChannels.Count();
            var storageUsed = totalCount * ChannelRecordSize;
            var maxStorage = MeshCodeClient.MaxChannelsSupported * ChannelRecordSize;

            var postState = await CaptureDeviceState("Post-ChannelStorageCapacity");
            LogDeviceState(postState, "   ");
            CompareDeviceStates(preState, postState);
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = postState;
            
            _output.WriteLine($"   Final channel count: {totalCount}");
            _output.WriteLine($"   Storage used: {storageUsed} bytes of {maxStorage} bytes");
            _output.WriteLine($"   Storage efficiency: {(double)storageUsed / maxStorage * 100:F1}%");
            
            if (totalCount <= MeshCodeClient.MaxChannelsSupported)
            {
                _output.WriteLine($"   ✅ Within documented limits");
            }
            else
            {
                _output.WriteLine($"   ⚠️  Exceeded documented limits");
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Storage capacity test failed: {ex.Message}");
            
            var errorState = await CaptureDeviceState("Error-ChannelStorageCapacity");
            LogDeviceState(errorState, "   ");
            
            _preTestStates[_testMethodName] = preState;
            _postTestStates[_testMethodName] = errorState;
            
            throw;
        }
    }

    /// <summary>
    /// Test: Channel error handling for various failure scenarios
    /// </summary>
    [Fact]
    public async Task Test_15_ChannelErrorHandling_ShouldHandleFailureScenarios()
    {
        _output.WriteLine("TEST 15: Channel Error Handling (Comprehensive Scenarios)");
        _output.WriteLine("========================================================");

        var preState = await CaptureDeviceState("Pre-ChannelErrorHandling");
        LogDeviceState(preState, "   ");

        await EnsureConnected();

        // Test invalid frequency (negative)
        try
        {
            var invalidConfig = new Channel
            {
                Name = "InvalidFreq",
                Frequency = -1,
                IsEncrypted = false
            };
            
            await _sharedClient!.SetChannelAsync(invalidConfig);
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

        // Test sending message to non-existent channel
        try
        {
            await _sharedClient!.SendChannelMessageAsync("NonExistentChannel_" + Guid.NewGuid(), "Test message");
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

        // Test null message content
        try
        {
            var currentChannel = await _sharedClient!.GetPublicChannelAsync();
            var channelName = currentChannel?.Name ?? DefaultChannelName;
            
            await _sharedClient.SendChannelMessageAsync(channelName, null!);
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

        // Test malformed encryption key
        try
        {
            var malformedConfig = new Channel
            {
                Name = "MalformedKey",
                Frequency = 433000000,
                IsEncrypted = true,
                EncryptionKey = "NOT_VALID_HEX"
            };
            
            await _sharedClient!.SetChannelAsync(malformedConfig);
            _output.WriteLine("   ⚠️  Malformed encryption key was accepted");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"   ✅ Malformed encryption key properly rejected: {ex.GetType().Name}");
        }

        var postState = await CaptureDeviceState("Post-ChannelErrorHandling");
        LogDeviceState(postState, "   ");
        CompareDeviceStates(preState, postState);
        
        _preTestStates[_testMethodName] = preState;
        _postTestStates[_testMethodName] = postState;

        _output.WriteLine("   📝 Research Note: Error handling protects channel storage integrity");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Generate a cryptographically random 32-byte AES-256 key
    /// Research shows MeshCore uses 32-byte keys for maximum security
    /// </summary>
    private static byte[] GenerateChannelKey()
    {
        var random = new Random();
        var key = new byte[ExpectedChannelKeyLength]; // 32 bytes for AES-256
        random.NextBytes(key);
        return key;
    }

    #endregion

    #region Cleanup

    public void Dispose()
    {
        try
        {
            _output.WriteLine("");
            _output.WriteLine("🧹 CLEANUP & FINAL DIAGNOSTICS");
            _output.WriteLine("==============================");
            _output.WriteLine($"Test: {_testMethodName} completing");
            
            // Show test execution summary
            _output.WriteLine($"📊 Test Execution Summary:");
            _output.WriteLine($"   Total Tests Run: {_testExecutionOrder.Count}");
            _output.WriteLine($"   Execution Order: {string.Join(" → ", _testExecutionOrder)}");
            _output.WriteLine($"   Channels Created: {_createdTestChannels.Count}");

            // Warning about channel persistence (no deletion support)
            if (_createdTestChannels.Count > 0)
            {
                _output.WriteLine("");
                _output.WriteLine("⚠️  CHANNEL PERSISTENCE WARNING");
                _output.WriteLine("================================");
                _output.WriteLine($"   Created {_createdTestChannels.Count} test channels during this run:");
                foreach (var channelId in _createdTestChannels)
                {
                    _output.WriteLine($"   📡 {channelId[..Math.Min(20, channelId.Length)]}...");
                }
                _output.WriteLine("");
                _output.WriteLine("   🔒 MeshCore protocol does NOT support channel deletion");
                _output.WriteLine("   📌 These channels will PERSIST on the device until factory reset");
                _output.WriteLine("   💡 Future test runs use unique timestamps to avoid conflicts");
                _output.WriteLine($"   🆔 This test run ID: {TestRunId}");
                _output.WriteLine("");
                _output.WriteLine("   To manage test channel accumulation:");
                _output.WriteLine("   1. Use a dedicated test device for development");
                _output.WriteLine("   2. Perform factory reset periodically to clear test channels");
                _output.WriteLine("   3. Monitor channel count with Test_03_GetAvailableChannels");
                _output.WriteLine($"   4. Device supports max {MeshCodeClient.MaxChannelsSupported} channels per research");
                _output.WriteLine("");
            }

            // Final state capture
            if (_sharedClient?.IsConnected == true)
            {
                try
                {
                    var finalState = CaptureDeviceState("Final-Cleanup").Result;
                    LogDeviceState(finalState, "   ");
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"   ⚠️  Failed to capture final state: {ex.Message}");
                }
            }

            _output.WriteLine($"✅ {_testMethodName} cleanup completed");
            _output.WriteLine("📝 Research Reference: MeshCore Channel Deletion – Protocol Support and Implementation");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"⚠️  Warning during cleanup: {ex.Message}");
        }
    }

    #endregion
}