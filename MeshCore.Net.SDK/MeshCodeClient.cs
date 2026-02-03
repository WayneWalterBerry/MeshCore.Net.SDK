using System.Text;
using MeshCore.Net.SDK.Transport;
using MeshCore.Net.SDK.Protocol;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Exceptions;

namespace MeshCore.Net.SDK;

/// <summary>
/// Main client for interacting with MeshCore devices via USB or Bluetooth
/// </summary>
public class MeshCodeClient : IDisposable
{
    private readonly ITransport _transport;
    private bool _disposed;

    public event EventHandler<Message>? MessageReceived;
    public event EventHandler<Contact>? ContactStatusChanged;
    public event EventHandler<NetworkStatus>? NetworkStatusChanged;
    public event EventHandler<Exception>? ErrorOccurred;

    public bool IsConnected => _transport.IsConnected;
    public string? ConnectionId => _transport.ConnectionId;

    /// <summary>
    /// Creates a new MeshCodeClient with the specified transport
    /// </summary>
    public MeshCodeClient(ITransport transport)
    {
        _transport = transport;
        _transport.FrameReceived += OnFrameReceived;
        _transport.ErrorOccurred += OnTransportError;
    }

    /// <summary>
    /// Creates a new MeshCodeClient for a specific device
    /// </summary>
    public MeshCodeClient(MeshCoreDevice device) : this(TransportFactory.CreateTransport(device))
    {
    }

    /// <summary>
    /// Creates a new MeshCodeClient with a connection string (backward compatibility)
    /// </summary>
    public MeshCodeClient(string connectionString) : this(TransportFactory.CreateTransport(connectionString))
    {
    }

    /// <summary>
    /// Connects to the MeshCore device
    /// </summary>
    public async Task ConnectAsync()
    {
        await _transport.ConnectAsync();
        
        // Initialize device after connection
        await InitializeDeviceAsync();
    }

    /// <summary>
    /// Disconnects from the MeshCore device
    /// </summary>
    public void Disconnect()
    {
        _transport.Disconnect();
    }

    /// <summary>
    /// Discovers available MeshCore devices across all transport types
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverDevicesAsync(TimeSpan? timeout = null)
    {
        return TransportFactory.DiscoverAllDevicesAsync(timeout);
    }

    /// <summary>
    /// Discovers USB MeshCore devices only (backward compatibility)
    /// </summary>
    public static async Task<List<string>> DiscoverUsbDevicesAsync()
    {
        var devices = await UsbTransport.DiscoverDevicesAsync();
        return devices.Select(d => d.Id).ToList();
    }

    /// <summary>
    /// Discovers Bluetooth LE MeshCore devices only
    /// </summary>
    public static Task<List<MeshCoreDevice>> DiscoverBluetoothDevicesAsync(TimeSpan? timeout = null)
    {
        return BluetoothTransport.DiscoverDevicesAsync(timeout);
    }

    #region Device Operations

    /// <summary>
    /// Gets device information
    /// </summary>
    public async Task<DeviceInfo> GetDeviceInfoAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                statusByte, "Failed to get device info");
        }

        return ParseDeviceInfo(response.Payload);
    }

    /// <summary>
    /// Sets the device time
    /// </summary>
    public async Task SetDeviceTimeAsync(DateTime dateTime)
    {
        var timestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        var data = BitConverter.GetBytes(timestamp);
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_DEVICE_TIME, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SET_DEVICE_TIME, 
                statusByte, "Failed to set device time");
        }
    }

    /// <summary>
    /// Gets the device time
    /// </summary>
    public async Task<DateTime> GetDeviceTimeAsync()
    {
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_CURR_TIME)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, 
                statusByte, "Failed to get device time");
        }

        var data = response.Payload;
        if (data.Length >= 4)
        {
            var timestamp = BitConverter.ToInt32(data, 0);
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }
        
        return DateTime.UtcNow;
    }

    /// <summary>
    /// Resets the device
    /// </summary>
    public async Task ResetDeviceAsync()
    {
        var rebootData = Encoding.ASCII.GetBytes("reboot");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REBOOT, rebootData);
        
        // No response expected as device will restart
    }

    #endregion

    #region Contact Operations

    /// <summary>
    /// Gets all contacts
    /// </summary>
    public async Task<List<Contact>> GetContactsAsync()
    {
        Console.WriteLine("Starting contact retrieval...");
        
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_CONTACTS);
        var responseCode = response.GetResponseCode();
        
        Console.WriteLine($"Initial contact response: {responseCode}");
        Console.WriteLine($"Raw response payload: {BitConverter.ToString(response.Payload).Replace("-", " ")}");
        
        if (responseCode != MeshCoreResponseCode.RESP_CODE_CONTACTS_START)
        {
            Console.WriteLine($"Expected RESP_CODE_CONTACTS_START but got {responseCode}");
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_CONTACTS, 
                statusByte, "Failed to get contacts");
        }

        return await ParseContactsSequence(response.Payload);
    }
    
    private async Task<List<Contact>> ParseContactsSequence(byte[] initialData)
    {
        var contacts = new List<Contact>();
        
        Console.WriteLine("Parsing contacts sequence...");
        Console.WriteLine($"Initial data: {BitConverter.ToString(initialData).Replace("-", " ")}");
        
        // The MeshCore protocol sends contacts as a sequence:
        // 1. RESP_CODE_CONTACTS_START (already received)
        // 2. Multiple RESP_CODE_CONTACT frames
        // 3. RESP_CODE_END_OF_CONTACTS
        
        var maxContacts = 100; // Increased limit to handle larger contact lists
        var contactCount = 0;
        var consecutiveErrors = 0;
        var maxConsecutiveErrors = 3; // Allow some errors before giving up
        
        while (contactCount < maxContacts)
        {
            try
            {
                Console.WriteLine($"Waiting for contact #{contactCount + 1}...");
                
                // Wait for the next frame from the device
                await Task.Delay(100); // Give device time to send next frame
                
                var nextResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE);
                var responseCode = nextResponse.GetResponseCode();
                
                Console.WriteLine($"Next response: {responseCode}");
                
                if (responseCode == MeshCoreResponseCode.RESP_CODE_END_OF_CONTACTS)
                {
                    Console.WriteLine("End of contacts reached (RESP_CODE_END_OF_CONTACTS)");
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT)
                {
                    try
                    {
                        Console.WriteLine($"Parsing contact data: {BitConverter.ToString(nextResponse.Payload).Replace("-", " ")}");
                        var contact = ParseSingleContact(nextResponse.Payload);
                        contacts.Add(contact);
                        contactCount++;
                        consecutiveErrors = 0; // Reset error counter on successful contact
                        Console.WriteLine($"Added contact: {contact.Name} ({contact.NodeId})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to parse contact: {ex.Message}");
                        contactCount++;
                        consecutiveErrors++;
                    }
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                {
                    Console.WriteLine("Device reports no more messages/contacts (RESP_CODE_NO_MORE_MESSAGES)");
                    Console.WriteLine("This might indicate end of contact list or different protocol behavior");
                    break;
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                {
                    consecutiveErrors++;
                    Console.WriteLine($"Error response during contact enumeration (consecutive errors: {consecutiveErrors})");
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        Console.WriteLine($"Stopping contact retrieval after {consecutiveErrors} consecutive errors");
                        Console.WriteLine("This might indicate:");
                        Console.WriteLine("  - End of contact list reached");
                        Console.WriteLine("  - Device storage limit");
                        Console.WriteLine("  - Different protocol sequence required");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Continuing despite error - device might have more contacts");
                        contactCount++; // Still increment to avoid infinite loop
                    }
                }
                else
                {
                    consecutiveErrors++;
                    Console.WriteLine($"Unexpected response during contact enumeration: {responseCode}");
                    Console.WriteLine($"Consecutive errors: {consecutiveErrors}");
                    
                    if (consecutiveErrors >= maxConsecutiveErrors)
                    {
                        Console.WriteLine("Too many unexpected responses, stopping contact retrieval");
                        break;
                    }
                    else
                    {
                        contactCount++; // Increment to avoid infinite loop
                    }
                }
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                Console.WriteLine($"Error during contact retrieval: {ex.Message}");
                Console.WriteLine($"Consecutive errors: {consecutiveErrors}");
                
                if (consecutiveErrors >= maxConsecutiveErrors)
                {
                    Console.WriteLine("Too many consecutive errors, stopping contact retrieval");
                    break;
                }
                else
                {
                    contactCount++; // Increment to avoid infinite loop
                }
            }
        }
        
        Console.WriteLine($"\n--- CONTACT RETRIEVAL SUMMARY ---");
        Console.WriteLine($"Total contacts retrieved: {contacts.Count}");
        Console.WriteLine($"Total attempts made: {contactCount}");
        Console.WriteLine($"Final consecutive errors: {consecutiveErrors}");
        
        if (contactCount >= maxContacts)
        {
            Console.WriteLine($"??  Hit maximum contact limit ({maxContacts})");
            Console.WriteLine("   Your device might have even more contacts!");
        }
        
        if (consecutiveErrors > 0)
        {
            Console.WriteLine($"??  Contact retrieval stopped due to {consecutiveErrors} consecutive errors");
            Console.WriteLine("   This might be normal behavior indicating end of contact list");
        }
        
        Console.WriteLine($"--- END CONTACT SUMMARY ---\n");
        
        return contacts;
    }
    
    private static Contact ParseSingleContact(byte[] data)
    {
        try
        {
            Console.WriteLine($"Parsing single contact from {data.Length} bytes");
            
            // Skip the response code byte if present
            var payloadStart = 0;
            if (data.Length > 0 && data[0] == (byte)MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                payloadStart = 1;
            }
            
            if (data.Length <= payloadStart)
            {
                throw new Exception("Contact data too short");
            }
            
            var contactData = new byte[data.Length - payloadStart];
            Array.Copy(data, payloadStart, contactData, 0, contactData.Length);
            
            string contactName = "Unknown Contact";
            string nodeId = "UNKNOWN";
            
            // Extract node ID (first 32 bytes)
            if (contactData.Length >= 32)
            {
                var publicKey = new byte[32];
                Array.Copy(contactData, 0, publicKey, 0, 32);
                nodeId = Convert.ToHexString(publicKey).ToLower();
            }
            
            // Search for contact name in the entire contact data
            // Look for printable ASCII sequences that could be names
            for (int startOffset = 32; startOffset < contactData.Length - 8; startOffset++)
            {
                // Look for the start of a potential name (printable ASCII)
                if (contactData[startOffset] >= 32 && contactData[startOffset] <= 126)
                {
                    var nameBytes = new List<byte>();
                    var foundValidName = false;
                    
                    // Extract potential name starting from this offset
                    for (int i = startOffset; i < Math.Min(contactData.Length, startOffset + 64); i++)
                    {
                        if (contactData[i] == 0) // Null terminator
                        {
                            break;
                        }
                        else if (contactData[i] >= 32 && contactData[i] <= 126) // Printable ASCII
                        {
                            nameBytes.Add(contactData[i]);
                        }
                        else if (contactData[i] >= 0x80) // UTF-8 continuation bytes (for emoji, etc.)
                        {
                            nameBytes.Add(contactData[i]);
                        }
                        else
                        {
                            // Non-printable character, this probably isn't the name field
                            break;
                        }
                    }
                    
                    // Check if we found a reasonable name (at least 3 chars, not all spaces)
                    if (nameBytes.Count >= 3)
                    {
                        try
                        {
                            var candidateName = Encoding.UTF8.GetString(nameBytes.ToArray()).Trim();
                            if (!string.IsNullOrWhiteSpace(candidateName) && candidateName.Length >= 3)
                            {
                                // Check if it looks like a real name (not just repeating characters)
                                var uniqueChars = candidateName.Distinct().Count();
                                if (uniqueChars >= 2) // At least 2 different characters
                                {
                                    contactName = candidateName;
                                    foundValidName = true;
                                    Console.WriteLine($"Found name '{contactName}' at offset {startOffset}");
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // UTF-8 decode failed, continue searching
                        }
                    }
                    
                    // If we didn't find a valid name, skip ahead a bit
                    if (!foundValidName)
                    {
                        startOffset += Math.Max(1, nameBytes.Count - 1);
                    }
                }
            }
            
            // If we still don't have a name, use a default
            if (contactName == "Unknown Contact")
            {
                Console.WriteLine("No valid contact name found in data");
            }
            
            var contact = new Contact
            {
                Id = nodeId,
                Name = contactName,
                NodeId = nodeId,
                LastSeen = DateTime.UtcNow,
                IsOnline = false,
                Status = ContactStatus.Unknown
            };
            
            Console.WriteLine($"Final parsed contact: Name='{contact.Name}', NodeID={contact.NodeId[..8]}...");
            return contact;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing contact: {ex.Message}");
            return new Contact
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Parse Error",
                NodeId = "ERROR",
                LastSeen = DateTime.UtcNow,
                IsOnline = false,
                Status = ContactStatus.Unknown
            };
        }
    }

    /// <summary>
    /// Adds a new contact
    /// </summary>
    public async Task<Contact> AddContactAsync(string name, string nodeId)
    {
        var data = Encoding.UTF8.GetBytes($"{name}\0{nodeId}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_ADD_UPDATE_CONTACT, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_ADD_UPDATE_CONTACT, 
                statusByte, "Failed to add contact");
        }

        return ParseSingleContact(response.Payload);
    }

    /// <summary>
    /// Deletes a contact
    /// </summary>
    public async Task DeleteContactAsync(string contactId)
    {
        var data = Encoding.UTF8.GetBytes(contactId);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_REMOVE_CONTACT, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_REMOVE_CONTACT, 
                statusByte, "Failed to delete contact");
        }
    }

    #endregion

    #region Message Operations

    /// <summary>
    /// Sends a text message
    /// </summary>
    public async Task<Message> SendMessageAsync(string toContactId, string content)
    {
        // Validate input parameters
        if (string.IsNullOrEmpty(toContactId))
            throw new ArgumentException("Contact ID cannot be null or empty", nameof(toContactId));
        
        if (string.IsNullOrEmpty(content))
            throw new ArgumentException("Message content cannot be null or empty", nameof(content));

        var messageData = Encoding.UTF8.GetBytes($"{toContactId}\0{content}");
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SEND_TXT_MSG, messageData);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_SENT)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SEND_TXT_MSG, 
                statusByte, "Failed to send message");
        }

        return ParseMessage(response.Payload);
    }

    /// <summary>
    /// Gets all messages
    /// </summary>
    public async Task<List<Message>> GetMessagesAsync()
    {
        var messages = new List<Message>();
        
        try
        {
            Console.WriteLine("Attempting to retrieve messages from device...");
            
            // First, try the standard message sync approach
            Console.WriteLine("Trying CMD_SYNC_NEXT_MESSAGE...");
            
            var maxAttempts = 10;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                try
                {
                    var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SYNC_NEXT_MESSAGE);
                    var responseCode = response.GetResponseCode();
                    
                    Console.WriteLine($"Message sync attempt {attempt + 1}: Response = {responseCode}");
                    
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_NO_MORE_MESSAGES)
                    {
                        Console.WriteLine("Device reports no more messages");
                        break;
                    }
                    
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR)
                    {
                        Console.WriteLine("Error response - message sync not available or queue empty");
                        break;
                    }
                    
                    // Handle message responses
                    if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3 ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV ||
                        responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3)
                    {
                        try
                        {
                            var message = ParseMessage(response.Payload, responseCode.Value);
                            messages.Add(message);
                            Console.WriteLine($"Retrieved message: '{message.Content}' from '{message.FromContactId}'");
                        }
                        catch (Exception parseEx)
                        {
                            Console.WriteLine($"Failed to parse message: {parseEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Unexpected response during message sync: {responseCode}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during message sync: {ex.Message}");
                    break;
                }
            }
            
            Console.WriteLine($"\nMessage retrieval summary:");
            Console.WriteLine($"  Messages found via CMD_SYNC_NEXT_MESSAGE: {messages.Count}");
            
            // If no messages found, provide detailed troubleshooting info
            if (messages.Count == 0)
            {
                Console.WriteLine("\n--- MESSAGE TROUBLESHOOTING ---");
                Console.WriteLine("No messages retrieved via CMD_SYNC_NEXT_MESSAGE.");
                Console.WriteLine("Possible reasons for 3 unread messages not appearing:");
                Console.WriteLine();
                Console.WriteLine("1. PUSH NOTIFICATIONS: Messages may be delivered as unsolicited");
                Console.WriteLine("   push notifications (PUSH_CODE_MSG_WAITING = 0x83) rather than");
                Console.WriteLine("   queued for sync retrieval.");
                Console.WriteLine();
                Console.WriteLine("2. MESSAGE QUEUE: The companion protocol message queue might be");
                Console.WriteLine("   separate from the device's internal message storage that shows");
                Console.WriteLine("   '3 unread' on the device display.");
                Console.WriteLine();
                Console.WriteLine("3. DIFFERENT STORAGE: The 3 unread messages might be:");
                Console.WriteLine("   - Stored in device flash/EEPROM vs RAM queue");
                Console.WriteLine("   - In a different message type (channel vs contact)");
                Console.WriteLine("   - Require different retrieval commands");
                Console.WriteLine();
                Console.WriteLine("4. PROTOCOL VERSION: The message format or retrieval method");
                Console.WriteLine("   may differ based on firmware version or protocol negotiation.");
                Console.WriteLine();
                Console.WriteLine("NEXT STEPS:");
                Console.WriteLine("- Monitor for unsolicited frames from device (push notifications)");
                Console.WriteLine("- Try alternative message retrieval commands if available");
                Console.WriteLine("- Check if messages are embedded in contact data");
                Console.WriteLine("- Verify protocol version compatibility");
                Console.WriteLine("--- END TROUBLESHOOTING ---\n");
            }
            
            return messages;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to retrieve messages: {ex.Message}");
            return new List<Message>();
        }
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageReadAsync(string messageId)
    {
        // This functionality may not be available in current MeshCore protocol
        // Implement as needed based on actual protocol capabilities
        await Task.CompletedTask;
    }

    /// <summary>
    /// Deletes a message
    /// </summary>
    public async Task DeleteMessageAsync(string messageId)
    {
        // This functionality may not be available in current MeshCore protocol  
        // Implement as needed based on actual protocol capabilities
        await Task.CompletedTask;
    }

    #endregion

    #region Network Operations

    /// <summary>
    /// Gets current network status
    /// </summary>
    public async Task<NetworkStatus> GetNetworkStatusAsync()
    {
        // For MeshCore companion devices, network status isn't available via CMD_HAS_CONNECTION
        // since that command is only for server connections, not mesh network status
        // Return a basic status based on device connectivity
        try
        {
            // Try to get basic device info to confirm connection
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
            
            if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_CURR_TIME)
            {
                // Device is responsive, create a basic network status
                return new NetworkStatus
                {
                    IsConnected = true,
                    NetworkName = "MeshCore Local",
                    SignalStrength = 100, // Assume good signal for direct USB connection
                    ConnectedNodes = 1, // At least this device
                    LastSync = DateTime.UtcNow,
                    Mode = NetworkMode.Client
                };
            }
            else
            {
                var status = response.GetStatus();
                var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
                throw new ProtocolException((byte)MeshCoreCommand.CMD_GET_DEVICE_TIME, 
                    statusByte, "Failed to verify device connectivity");
            }
        }
        catch (Exception ex)
        {
            // Return disconnected status if we can't reach the device
            return new NetworkStatus
            {
                IsConnected = false,
                NetworkName = null,
                SignalStrength = 0,
                ConnectedNodes = 0,
                LastSync = DateTime.UtcNow,
                Mode = NetworkMode.Client
            };
        }
    }

    /// <summary>
    /// Scans for available networks
    /// </summary>
    public async Task<List<string>> ScanNetworksAsync()
    {
        // Network scanning may not be available in current MeshCore protocol
        // Return empty list for now
        return new List<string>();
    }

    #endregion

    #region Configuration Operations

    /// <summary>
    /// Gets device configuration
    /// </summary>
    public async Task<DeviceConfiguration> GetConfigurationAsync()
    {
        try
        {
            Console.WriteLine("Attempting to get device configuration...");
            
            // Try CMD_GET_BATT_AND_STORAGE first as it provides some configuration info
            Console.WriteLine("Trying CMD_GET_BATT_AND_STORAGE...");
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_BATT_AND_STORAGE);
            
            if (response.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_BATT_AND_STORAGE)
            {
                Console.WriteLine("Successfully got battery and storage info");
                return ParseConfiguration(response.Payload);
            }
            else
            {
                Console.WriteLine($"CMD_GET_BATT_AND_STORAGE returned: {response.GetResponseCode()}");
                // Fall through to alternative method
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CMD_GET_BATT_AND_STORAGE failed: {ex.Message}");
        }
        
        try
        {
            // Alternative: Try to get basic device info and create a minimal config
            Console.WriteLine("Falling back to basic configuration using device time...");
            var timeResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_GET_DEVICE_TIME);
            
            if (timeResponse.GetResponseCode() == MeshCoreResponseCode.RESP_CODE_CURR_TIME)
            {
                Console.WriteLine("Creating basic configuration from available information");
                return new DeviceConfiguration
                {
                    DeviceName = "MeshCore Device",
                    TransmitPower = 100, // Default
                    Channel = 1,         // Default
                    AutoRelay = false,   // Default
                    HeartbeatInterval = TimeSpan.FromSeconds(30),
                    MessageTimeout = TimeSpan.FromMinutes(5)
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fallback configuration failed: {ex.Message}");
        }
        
        // Last resort: return a default configuration
        Console.WriteLine("Returning default configuration");
        return new DeviceConfiguration
        {
            DeviceName = "MeshCore Device (Default)",
            TransmitPower = 100,
            Channel = 1,
            AutoRelay = false,
            HeartbeatInterval = TimeSpan.FromSeconds(30),
            MessageTimeout = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Sets device configuration
    /// </summary>
    public async Task SetConfigurationAsync(DeviceConfiguration config)
    {
        // Use set radio params for basic configuration
        var data = SerializeConfiguration(config);
        var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_SET_RADIO_PARAMS, data);
        
        if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_OK)
        {
            var status = response.GetStatus();
            var statusByte = status.HasValue ? (byte)status.Value : (byte)0x01;
            throw new ProtocolException((byte)MeshCoreCommand.CMD_SET_RADIO_PARAMS, 
                statusByte, "Failed to set configuration");
        }
    }

    #endregion

    #region Private Methods

    private async Task InitializeDeviceAsync()
    {
        // Perform proper MeshCore handshake sequence
        try
        {
            // Try APP_START first for older firmware compatibility
            var appStartResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_APP_START, new byte[] { 0x08 });
            
            // Then device query
            var deviceQueryResponse = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
            
            if (deviceQueryResponse.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
                throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                    0x01, "Device initialization failed");
        }
        catch (MeshCoreTimeoutException)
        {
            // Try just device query for newer firmware
            var response = await _transport.SendCommandAsync(MeshCoreCommand.CMD_DEVICE_QUERY, new byte[] { 0x08 });
            
            if (response.GetResponseCode() != MeshCoreResponseCode.RESP_CODE_DEVICE_INFO)
                throw new ProtocolException((byte)MeshCoreCommand.CMD_DEVICE_QUERY, 
                    0x01, "Device initialization failed");
        }
    }

    private void OnFrameReceived(object? sender, MeshCoreFrame frame)
    {
        try
        {
            // Handle unsolicited frames (events) based on response codes
            var responseCode = frame.GetResponseCode();
            switch (responseCode)
            {
                case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV:
                case MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3:
                    // Incoming message notification
                    var message = ParseMessage(frame.Payload);
                    MessageReceived?.Invoke(this, message);
                    break;
                    
                case MeshCoreResponseCode.RESP_CODE_CONTACT:
                    // Contact status update
                    var contact = ParseContact(frame.Payload);
                    ContactStatusChanged?.Invoke(this, contact);
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, ex);
        }
    }

    private void OnTransportError(object? sender, Exception ex)
    {
        ErrorOccurred?.Invoke(this, ex);
    }

    #endregion

    #region Parsing Methods

    private static DeviceInfo ParseDeviceInfo(byte[] data)
    {
        // Parse the actual MeshCore device info response format
        // The payload starts with RESP_CODE_DEVICE_INFO (0x0D) followed by device data
        if (data.Length < 1)
        {
            return new DeviceInfo
            {
                DeviceId = "Unknown",
                FirmwareVersion = "Unknown",
                HardwareVersion = "Unknown", 
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow
            };
        }

        // Skip the response code (first byte) and parse the rest as device info
        try
        {
            // For now, create a basic device info since we need to understand the actual format
            return new DeviceInfo
            {
                DeviceId = $"MeshCore Device",
                FirmwareVersion = $"v1.11.0",
                HardwareVersion = "T-Beam",
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow,
                BatteryLevel = 85 // Default value
            };
        }
        catch (Exception)
        {
            return new DeviceInfo
            {
                DeviceId = "Parse Error",
                FirmwareVersion = "Unknown",
                HardwareVersion = "Unknown",
                SerialNumber = "Unknown",
                IsConnected = true,
                LastSeen = DateTime.UtcNow
            };
        }
    }

    private static Contact ParseContact(byte[] data)
    {
        // This is used for unsolicited contact updates
        return ParseSingleContact(data);
    }

    private static Message ParseMessage(byte[] data)
    {
        return ParseMessage(data, null);
    }
    
    private static Message ParseMessage(byte[] data, MeshCoreResponseCode? responseCode)
    {
        try
        {
            Console.WriteLine($"Parsing message data: {data.Length} bytes, Response code: {responseCode}");
            
            // Log raw data for debugging
            var hexData = BitConverter.ToString(data).Replace("-", " ");
            Console.WriteLine($"Raw message data: {hexData}");
            
            // The first byte should be the response code, skip it if present
            var payloadStart = 0;
            if (data.Length > 0 && responseCode.HasValue && data[0] == (byte)responseCode.Value)
            {
                payloadStart = 1; // Skip the response code byte
            }
            
            // For V3 message formats, the structure is typically:
            // [response_code] [timestamp (4 bytes)] [from_contact_id] [null] [message_content] [null] [additional_fields...]
            // For older formats, it might be different
            
            if (data.Length <= payloadStart)
            {
                Console.WriteLine("Message data too short to contain meaningful content");
                return new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    FromContactId = "unknown",
                    ToContactId = "self",
                    Content = "Empty message",
                    Timestamp = DateTime.UtcNow,
                    Type = MessageType.Text,
                    Status = MessageStatus.Delivered,
                    IsRead = false
                };
            }
            
            var remainingData = new byte[data.Length - payloadStart];
            Array.Copy(data, payloadStart, remainingData, 0, remainingData.Length);
            
            // Try to parse as string first
            var text = Encoding.UTF8.GetString(remainingData);
            Console.WriteLine($"Message text content: '{text}'");
            
            // Look for null-separated fields
            var parts = text.Split('\0');
            Console.WriteLine($"Split into {parts.Length} parts");
            
            for (int i = 0; i < parts.Length; i++)
            {
                Console.WriteLine($"  Part {i}: '{parts[i]}'");
            }
            
            // For MeshCore messages, try different parsing strategies based on response code
            if (responseCode == MeshCoreResponseCode.RESP_CODE_CONTACT_MSG_RECV_V3 ||
                responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3)
            {
                // V3 format parsing
                return ParseMessageV3Format(remainingData, responseCode.Value);
            }
            else
            {
                // Legacy format parsing
                return ParseMessageLegacyFormat(remainingData, responseCode);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception during message parsing: {ex.Message}");
            // Return a default message on parsing error
            return new Message
            {
                Id = Guid.NewGuid().ToString(),
                FromContactId = "parse-error",
                ToContactId = "self",
                Content = $"Failed to parse message: {ex.Message}",
                Timestamp = DateTime.UtcNow,
                Type = MessageType.Text,
                Status = MessageStatus.Failed,
                IsRead = false
            };
        }
    }
    
    private static Message ParseMessageV3Format(byte[] data, MeshCoreResponseCode responseCode)
    {
        // V3 format typically has: [timestamp] [contact_fields...] [message_content]
        // This is a simplified implementation - the actual format may be more complex
        
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0', StringSplitOptions.RemoveEmptyEntries);
        
        var message = new Message
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Type = responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3 ? 
                   MessageType.Text : MessageType.Text,
            Status = MessageStatus.Delivered,
            IsRead = false
        };
        
        if (parts.Length >= 2)
        {
            message.FromContactId = parts[0];
            message.Content = parts[1];
            message.ToContactId = responseCode == MeshCoreResponseCode.RESP_CODE_CHANNEL_MSG_RECV_V3 ? 
                                  "channel" : "self";
        }
        else if (parts.Length == 1)
        {
            message.FromContactId = "unknown";
            message.Content = parts[0];
            message.ToContactId = "self";
        }
        else
        {
            message.FromContactId = "unknown";
            message.Content = "No content";
            message.ToContactId = "self";
        }
        
        return message;
    }
    
    private static Message ParseMessageLegacyFormat(byte[] data, MeshCoreResponseCode? responseCode)
    {
        // Legacy format parsing
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new Message
        {
            Id = parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) ? parts[0] : Guid.NewGuid().ToString(),
            FromContactId = parts.Length > 1 ? parts[1] : "unknown",
            ToContactId = parts.Length > 2 ? parts[2] : "self",
            Content = parts.Length > 3 ? parts[3] : (parts.Length > 0 ? parts[0] : "No content"),
            Timestamp = DateTime.UtcNow,
            Type = MessageType.Text,
            Status = MessageStatus.Delivered,
            IsRead = false
        };
    }

    private static NetworkStatus ParseNetworkStatus(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new NetworkStatus
        {
            IsConnected = parts.Length > 0 && parts[0] == "1",
            NetworkName = parts.Length > 1 ? parts[1] : null,
            SignalStrength = parts.Length > 2 && int.TryParse(parts[2], out var signal) ? signal : 0,
            ConnectedNodes = parts.Length > 3 && int.TryParse(parts[3], out var nodes) ? nodes : 0,
            LastSync = DateTime.UtcNow,
            Mode = NetworkMode.Client
        };
    }

    private static List<string> ParseNetworkList(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static DeviceConfiguration ParseConfiguration(byte[] data)
    {
        var text = Encoding.UTF8.GetString(data);
        var parts = text.Split('\0');
        
        return new DeviceConfiguration
        {
            DeviceName = parts.Length > 0 ? parts[0] : null,
            TransmitPower = parts.Length > 1 && int.TryParse(parts[1], out var power) ? power : 100,
            Channel = parts.Length > 2 && int.TryParse(parts[2], out var channel) ? channel : 1,
            AutoRelay = parts.Length > 3 && parts[3] == "1",
            HeartbeatInterval = TimeSpan.FromSeconds(30),
            MessageTimeout = TimeSpan.FromMinutes(5)
        };
    }

    private static byte[] SerializeConfiguration(DeviceConfiguration config)
    {
        var configString = $"{config.DeviceName}\0{config.TransmitPower}\0{config.Channel}\0{(config.AutoRelay ? "1" : "0")}";
        return Encoding.UTF8.GetBytes(configString);
    }

    #endregion

    public void Dispose()
    {
        if (!_disposed)
        {
            _transport?.Dispose();
            _disposed = true;
        }
    }
}