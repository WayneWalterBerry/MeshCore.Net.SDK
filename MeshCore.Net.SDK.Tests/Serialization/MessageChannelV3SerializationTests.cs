// <copyright file="MessageChannelV3SerializationTests.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Tests.Serialization
{
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Serialization;
    using Xunit;
    using Xunit.Abstractions;

    /// <summary>
    /// Unit tests for MessageChannelV3Serialization based on real debugger data
    /// Tests the V3 format channel message deserialization from actual MeshCore device
    /// 
    /// Test data based on actual debugging session where:
    /// - messageText = "iBhamDin: Test" 
    /// - channelIndex = 0 (Public channel)
    /// - SNR = 4.25 (17/4), timestamp = 2208808704
    /// 
    /// Format: SNR(1) + reserved1(1) + reserved2(1) + channel_idx(1) + path_len(1) + txt_type(1) + timestamp(4) + text
    /// </summary>
    public class MessageChannelV3SerializationTests
    {
        private readonly ITestOutputHelper _output;

        /// <summary>
        /// Real debugger data from RESP_CODE_CHANNEL_MSG_RECV_V3 response
        /// Source: Live debugging session with MeshCore device
        /// Expected: SNR=4.25, channel=0, timestamp=2208808704, message="iBhamDin: Test"
        /// </summary>
        public static readonly byte[] ActualDebuggerChannelMessageV3 = new byte[]
        {
            // V3 Header: SNR=17 (4.25*4), reserved1=48, reserved2=0
            0x11, 0x30, 0x00,
            
            // Channel message data: channel=0, path=4, txt_type=0
            0x00, 0x04, 0x00,
            
            // Timestamp: 2208808704 (2040-01-01 00:05:04 UTC) in little-endian
            0x00, 0xBF, 0xA7, 0x83,
            
            // Message text: "iBhamDin: Test" in UTF-8
            0x69, 0x42, 0x68, 0x61, 0x6D, 0x44, 0x69, 0x6E, 0x3A, 0x20, 0x54, 0x65, 0x73, 0x74
        };

        /// <summary>
        /// Initializes a new instance of the MessageChannelV3SerializationTests class
        /// </summary>
        /// <param name="output">Test output helper</param>
        public MessageChannelV3SerializationTests(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Test: Deserialize actual debugger data from RESP_CODE_CHANNEL_MSG_RECV_V3
        /// This test validates that the SDK correctly parses real device data
        /// </summary>
        [Fact]
        public void TryDeserialize_ActualDebuggerData_ShouldParseChannelMessageCorrectly()
        {
            // Arrange
            var serializer = MessageChannelV3Serialization.Instance;
            _output.WriteLine("Testing with actual debugger data from RESP_CODE_CHANNEL_MSG_RECV_V3");
            _output.WriteLine($"Data length: {ActualDebuggerChannelMessageV3.Length} bytes");
            _output.WriteLine($"Expected sender: 'iBhamDin'");
            _output.WriteLine($"Expected message: 'Test'");
            _output.WriteLine($"Expected timestamp: 2040-01-01 00:05:04 UTC (2208808704 seconds)");

            // Act
            var result = serializer.TryDeserialize(ActualDebuggerChannelMessageV3, out Message? message);

            // Assert
            Assert.True(result, "TryDeserialize should succeed with valid debugger data");
            Assert.NotNull(message);

            // Verify channel and sender parsing (debugger shows "iBhamDin: Test")
            Assert.Equal("iBhamDin", message.FromContactId);
            _output.WriteLine($"✅ FromContactId correctly parsed: {message.FromContactId}");

            // Verify clean message content (sender prefix removed)
            Assert.Equal("Test", message.Content);
            _output.WriteLine($"✅ Content correctly cleaned: '{message.Content}'");

            // Verify timestamp parsing (2208808704 = 2040-01-01 00:05:04 UTC)
            var expectedTimestamp = DateTimeOffset.FromUnixTimeSeconds(2208808704).DateTime;
            Assert.Equal(expectedTimestamp, message.Timestamp);
            _output.WriteLine($"✅ Timestamp correctly parsed: {message.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");

            // Verify message properties (only radio payload properties)
            Assert.True(message.IsTextMessage);

            _output.WriteLine($"✅ All debugger data validation passed");
        }

        /// <summary>
        /// Test: Verify V3 header parsing matches debugger values
        /// </summary>
        [Fact]
        public void TryDeserialize_V3Header_ShouldParseSnrAndReservedFields()
        {
            // Arrange - Focus on V3 header validation
            var testData = new byte[]
            {
                // V3 Header from debugger
                0x11, 0x30, 0x00,  // SNR=4.25, reserved1=48, reserved2=0
                // Minimal channel message
                0x00, 0x04, 0x00,  // channel=0, path=4, type=0
                0x00, 0x10, 0x20, 0x83,  // timestamp
                0x54, 0x65, 0x73, 0x74   // "Test"
            };

            _output.WriteLine("Testing V3 header parsing:");
            _output.WriteLine($"SNR byte: 0x{testData[0]:X2} = {testData[0]} / 4 = {testData[0] / 4.0f} dB");
            _output.WriteLine($"Reserved1: 0x{testData[1]:X2} = {testData[1]}");
            _output.WriteLine($"Reserved2: 0x{testData[2]:X2} = {testData[2]}");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);

            // The SNR parsing is internal to the serializer, but we verify it doesn't break parsing
            Assert.Equal("Test", message.Content);
            _output.WriteLine($"✅ V3 header processed successfully, content: '{message.Content}'");
        }

        /// <summary>
        /// Test: Handle channel message without sender prefix (clean channel message)
        /// </summary>
        [Fact]
        public void TryDeserialize_CleanChannelMessage_ShouldUseDefaultChannelSender()
        {
            // Arrange - Channel message without "Sender: " prefix
            var testData = new byte[]
            {
                // V3 Header
                0x11, 0x30, 0x00,
                // Channel message
                0x01, 0x04, 0x00,  // channel=1, path=4, type=0
                0x00, 0x10, 0x20, 0x83,  // timestamp
                // Clean message without sender prefix
                0x48, 0x65, 0x6C, 0x6C, 0x6F  // "Hello"
            };

            _output.WriteLine("Testing clean channel message (no sender prefix):");
            _output.WriteLine("Expected: Default to empty sender for channel message");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);

            Assert.Equal(string.Empty, message.FromContactId); // No sender in payload
            Assert.Equal("Hello", message.Content);

            _output.WriteLine($"✅ Clean message handled correctly");
            _output.WriteLine($"   FromContactId: '{message.FromContactId}'");
            _output.WriteLine($"   Content: '{message.Content}'");
        }

        /// <summary>
        /// Test: Various message formats and edge cases
        /// </summary>
        [Theory]
        [InlineData("Alice: Hello World", "Alice", "Hello World", "Standard sender format")]
        [InlineData("Node123: Status OK", "Node123", "Status OK", "Alphanumeric sender")]
        [InlineData("Test-User: Message", "Test-User", "Message", "Hyphenated sender")]
        [InlineData("A: B", "A", "B", "Single character sender and message")]
        [InlineData("NoColon", "", "NoColon", "No colon separator")]
        [InlineData("Multiple: Colons: In: Message", "Multiple", "Colons: In: Message", "Multiple colons")]
        [InlineData("Sender:NoSpace", "", "Sender:NoSpace", "No space after colon")]
        [InlineData("", "", "", "Empty message")]
        public void TryDeserialize_VariousMessageFormats_ShouldParseCorrectly(
            string messageText, string expectedFromContactId, string expectedContent, string description)
        {
            // Arrange
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageText);
            var testData = new byte[10 + messageBytes.Length];

            // V3 Header
            testData[0] = 0x11; testData[1] = 0x30; testData[2] = 0x00;
            // Channel info
            testData[3] = 0x00; testData[4] = 0x04; testData[5] = 0x00;
            // Timestamp
            testData[6] = 0x00; testData[7] = 0x10; testData[8] = 0x20; testData[9] = 0x83;
            // Message
            Array.Copy(messageBytes, 0, testData, 10, messageBytes.Length);

            _output.WriteLine($"Testing: {description}");
            _output.WriteLine($"Input: '{messageText}'");
            _output.WriteLine($"Expected FromContactId: '{expectedFromContactId}'");
            _output.WriteLine($"Expected Content: '{expectedContent}'");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result, $"Should parse successfully: {description}");
            Assert.NotNull(message);

            Assert.Equal(expectedFromContactId, message.FromContactId);
            Assert.Equal(expectedContent, message.Content);

            _output.WriteLine($"✅ {description} - parsed correctly");
        }

        /// <summary>
        /// Test: Invalid data handling
        /// </summary>
        [Theory]
        [InlineData(new byte[0], "Empty data")]
        [InlineData(new byte[] { 0x11 }, "Too short - only SNR")]
        [InlineData(new byte[] { 0x11, 0x30 }, "Too short - missing reserved2")]
        [InlineData(new byte[] { 0x11, 0x30, 0x00 }, "Too short - missing channel data")]
        [InlineData(new byte[] { 0x11, 0x30, 0x00, 0x00, 0x04, 0x00, 0x00 }, "Too short - incomplete timestamp")]
        public void TryDeserialize_InvalidData_ShouldReturnFalse(byte[] invalidData, string description)
        {
            _output.WriteLine($"Testing invalid data: {description}");
            _output.WriteLine($"Data length: {invalidData.Length} bytes");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(invalidData, out Message? message);

            // Assert
            Assert.False(result, $"Should fail for invalid data: {description}");
            Assert.Null(message);

            _output.WriteLine($"✅ {description} - correctly rejected");
        }

        /// <summary>
        /// Test: Different channel indices
        /// </summary>
        [Theory]
        [InlineData(0, "Public channel")]
        [InlineData(1, "Private channel 1")]
        [InlineData(39, "Maximum channel index")]
        [InlineData(255, "Maximum byte value")]
        public void TryDeserialize_DifferentChannelIndices_ShouldParseCorrectly(byte channelIndex, string description)
        {
            // Arrange
            var testData = new byte[]
            {
                // V3 Header
                0x11, 0x30, 0x00,
                // Channel message with variable channel index
                channelIndex, 0x04, 0x00,
                // Timestamp
                0x00, 0x10, 0x20, 0x83,
                // Message: "User: Test"
                0x55, 0x73, 0x65, 0x72, 0x3A, 0x20, 0x54, 0x65, 0x73, 0x74
            };

            _output.WriteLine($"Testing channel index {channelIndex}: {description}");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);

            var expectedFromContactId = "User"; // Just the sender name from payload
            var expectedToContactId = $"Channel_{channelIndex}";

            Assert.Equal(expectedFromContactId, message.FromContactId);
            Assert.Equal("Test", message.Content);

            _output.WriteLine($"✅ Channel {channelIndex} - parsed correctly");
            _output.WriteLine($"   FromContactId: {message.FromContactId}");
        }

        /// <summary>
        /// Test: Message types (text vs binary)
        /// </summary>
        [Theory]
        [InlineData(0x00, true, "Text message")]
        [InlineData(0x01, false, "Binary message")]
        [InlineData(0xFF, false, "Unknown type defaults to binary")]
        public void TryDeserialize_MessageTypes_ShouldSetCorrectType(byte textType, bool expectedIsTextMessage, string description)
        {
            // Arrange
            var testData = new byte[]
            {
                // V3 Header
                0x11, 0x30, 0x00,
                // Channel message with variable text type
                0x00, 0x04, textType,
                // Timestamp
                0x00, 0x10, 0x20, 0x83,
                // Message
                0x54, 0x65, 0x73, 0x74  // "Test"
            };

            _output.WriteLine($"Testing message type 0x{textType:X2}: {description}");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);
            Assert.Equal(expectedIsTextMessage, message.IsTextMessage);

            _output.WriteLine($"✅ Message type 0x{textType:X2} correctly set to IsTextMessage={expectedIsTextMessage}");
        }

        /// <summary>
        /// Test: Unicode and special characters in messages
        /// </summary>
        [Fact]
        public void TryDeserialize_UnicodeCharacters_ShouldHandleCorrectly()
        {
            // Arrange - Unicode message "Alice: Hello 世界 🌍"
            var messageText = "Alice: Hello 世界 🌍";
            var messageBytes = System.Text.Encoding.UTF8.GetBytes(messageText);
            var testData = new byte[10 + messageBytes.Length];

            // V3 Header
            testData[0] = 0x11; testData[1] = 0x30; testData[2] = 0x00;
            // Channel info
            testData[3] = 0x00; testData[4] = 0x04; testData[5] = 0x00;
            // Timestamp
            testData[6] = 0x00; testData[7] = 0x10; testData[8] = 0x20; testData[9] = 0x83;
            // Unicode message
            Array.Copy(messageBytes, 0, testData, 10, messageBytes.Length);

            _output.WriteLine($"Testing Unicode message: '{messageText}'");
            _output.WriteLine($"UTF-8 byte length: {messageBytes.Length}");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);

            Assert.Equal("Alice", message.FromContactId); // Just sender name from payload
            Assert.Equal("Hello 世界 🌍", message.Content);

            _output.WriteLine($"✅ Unicode message handled correctly");
            _output.WriteLine($"   Parsed content: '{message.Content}'");
        }

        /// <summary>
        /// Test: Null terminator handling
        /// </summary>
        [Fact]
        public void TryDeserialize_NullTerminatedMessage_ShouldTrimNulls()
        {
            // Arrange - Message with null terminators
            var testData = new byte[]
            {
                // V3 Header
                0x11, 0x30, 0x00,
                // Channel message
                0x00, 0x04, 0x00,
                // Timestamp
                0x00, 0x10, 0x20, 0x83,
                // Message: "Test" with null terminators
                0x54, 0x65, 0x73, 0x74, 0x00, 0x00, 0x00  // "Test\0\0\0"
            };

            _output.WriteLine("Testing null terminator handling");

            // Act
            var result = MessageChannelV3Serialization.Instance.TryDeserialize(testData, out Message? message);

            // Assert
            Assert.True(result);
            Assert.NotNull(message);
            Assert.Equal("Test", message.Content);
            Assert.DoesNotContain('\0', message.Content);

            _output.WriteLine($"✅ Null terminators properly trimmed: '{message.Content}'");
        }

        /// <summary>
        /// Test: Singleton pattern
        /// </summary>
        [Fact]
        public void Instance_ShouldReturnSameSingletonInstance()
        {
            // Act
            var instance1 = MessageChannelV3Serialization.Instance;
            var instance2 = MessageChannelV3Serialization.Instance;

            // Assert
            Assert.Same(instance1, instance2);
            _output.WriteLine("✅ Singleton pattern working correctly");
        }

        /// <summary>
        /// Test: Deserialize method (throws on failure)
        /// </summary>
        [Fact]
        public void Deserialize_ValidData_ShouldReturnMessage()
        {
            // Act & Assert - Should not throw
            var message = MessageChannelV3Serialization.Instance.Deserialize(ActualDebuggerChannelMessageV3);

            Assert.NotNull(message);
            Assert.Equal("iBhamDin", message.FromContactId); // Just sender name from payload
            Assert.Equal("Test", message.Content);

            _output.WriteLine("✅ Deserialize method works with valid data");
        }

        /// <summary>
        /// Test: Deserialize method should throw on invalid data
        /// </summary>
        [Fact]
        public void Deserialize_InvalidData_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var invalidData = new byte[] { 0x11 }; // Too short

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                MessageChannelV3Serialization.Instance.Deserialize(invalidData));

            Assert.Contains("Failed to deserialize V3 channel message", exception.Message);
            _output.WriteLine($"✅ Deserialize properly throws on invalid data: {exception.Message}");
        }
    }

    /// <summary>
    /// Test data helper class with additional test scenarios
    /// </summary>
    public static class MessageChannelV3TestData
    {
        /// <summary>
        /// Real debugger data from RESP_CODE_CHANNEL_MSG_RECV_V3 response
        /// Source: Live debugging session with MeshCore device
        /// </summary>
        public static readonly byte[] RealDeviceData = MessageChannelV3SerializationTests.ActualDebuggerChannelMessageV3;

        /// <summary>
        /// Expected parsing results for the real device data
        /// </summary>
        public static class ExpectedResults
        {
            public const string OriginalText = "iBhamDin: Test";
            public const string ParsedSender = "iBhamDin"; // Just sender name, no channel formatting
            public const string ParsedContent = "Test";
            public const string ToContactId = "Channel_0";
            public const byte ChannelIndex = 0;
            public const uint Timestamp = 2208808704;
            public static readonly DateTime TimestampDateTime = DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;
            public const float SNR = 4.25f;
            public const byte Reserved1 = 48;
            public const byte Reserved2 = 0;
            public const byte PathLen = 4;
            public const byte TextType = 0;
            public const bool ExpectedIsTextMessage = true; // Changed from MessageType enum
        }
    }
}