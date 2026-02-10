// <copyright file="MeshCoreResponseCode.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Protocol;

/// <summary>
/// MeshCore response codes returned by the device in response frames
/// These codes appear as the first byte in outbound frames (device to PC)
/// </summary>
public enum MeshCoreResponseCode : byte
{
    /// <summary>
    /// Operation completed successfully
    /// Generic success acknowledgment with no additional data
    /// </summary>
    RESP_CODE_OK = 0x00,

    /// <summary>
    /// Error occurred during operation
    /// An error code byte typically follows to explain the specific error condition
    /// </summary>
    RESP_CODE_ERR = 0x01,

    /// <summary>
    /// Start of contacts list transmission
    /// Marks the beginning of a contact list response sequence
    /// </summary>
    RESP_CODE_CONTACTS_START = 0x02,

    /// <summary>
    /// Contact information data entry
    /// Contains one contact's details as part of a list or new contact notification
    /// </summary>
    RESP_CODE_CONTACT = 0x03,

    /// <summary>
    /// End of contacts list transmission
    /// Marks the completion of a contact list response sequence
    /// </summary>
    RESP_CODE_END_OF_CONTACTS = 0x04,

    /// <summary>
    /// Self device information (node settings and radio parameters)
    /// Response to CMD_APP_START containing the device's own configuration and status
    /// </summary>
    RESP_CODE_SELF_INFO = 0x05,

    /// <summary>
    /// Message sent confirmation
    /// Indicates a message was successfully queued for mesh transmission
    /// Often includes an ACK tracking code and suggested timeout
    /// </summary>
    RESP_CODE_SENT = 0x06,

    /// <summary>
    /// Contact message received (protocol v1-2 format)
    /// Contains an incoming direct message from another node
    /// Legacy format for older protocol versions
    /// </summary>
    RESP_CODE_CONTACT_MSG_RECV = 0x07,

    /// <summary>
    /// Channel message received (protocol v1-2 format)
    /// Contains an incoming channel/group message
    /// Legacy format for older protocol versions
    /// </summary>
    RESP_CODE_CHANNEL_MSG_RECV = 0x08,

    /// <summary>
    /// Current device time response
    /// Contains a 32-bit Unix timestamp (UTC seconds)
    /// Response to CMD_GET_DEVICE_TIME
    /// </summary>
    RESP_CODE_CURR_TIME = 0x09,

    /// <summary>
    /// No more messages available in queue
    /// Response to CMD_SYNC_NEXT_MESSAGE when the message queue is empty
    /// </summary>
    RESP_CODE_NO_MORE_MESSAGES = 0x0A,

    /// <summary>
    /// Exported contact information
    /// Contains a contact's advertisement data (business card)
    /// Response to CMD_EXPORT_CONTACT
    /// </summary>
    RESP_CODE_EXPORT_CONTACT = 0x0B,

    /// <summary>
    /// Battery and storage information
    /// Contains battery voltage (mV) and storage statistics
    /// Response to CMD_GET_BATT_AND_STORAGE
    /// </summary>
    RESP_CODE_BATT_AND_STORAGE = 0x0C,

    /// <summary>
    /// Device information and capabilities
    /// Contains firmware version, build date, model name, max contacts/channels, BLE PIN
    /// Response to CMD_DEVICE_QUERY - used for protocol version negotiation
    /// </summary>
    RESP_CODE_DEVICE_INFO = 0x0D,

    /// <summary>
    /// Private key response
    /// Contains the device's exported private key (security sensitive)
    /// Response to CMD_EXPORT_PRIVATE_KEY
    /// </summary>
    RESP_CODE_PRIVATE_KEY = 0x0E,

    /// <summary>
    /// Feature or command is disabled
    /// Indicates the requested feature is not enabled in this firmware build
    /// </summary>
    RESP_CODE_DISABLED = 0x0F,

    /// <summary>
    /// Contact message received (protocol v3+ format)
    /// Contains an incoming direct message with enhanced format
    /// Current format for protocol version 3 and above
    /// </summary>
    RESP_CODE_CONTACT_MSG_RECV_V3 = 0x10,

    /// <summary>
    /// Channel message received (protocol v3+ format)
    /// Contains an incoming channel/group message with enhanced format
    /// Current format for protocol version 3 and above
    /// </summary>
    RESP_CODE_CHANNEL_MSG_RECV_V3 = 0x11,

    /// <summary>
    /// Channel information response
    /// Contains channel configuration (name, encryption parameters, etc.)
    /// Response to CMD_GET_CHANNEL
    /// </summary>
    RESP_CODE_CHANNEL_INFO = 0x12,

    /// <summary>
    /// Sign start response
    /// Acknowledgment that a signature operation has begun
    /// Response to CMD_SIGN_START
    /// </summary>
    RESP_CODE_SIGN_START = 0x13,

    /// <summary>
    /// Signature response
    /// Contains the cryptographic signature result
    /// Response to CMD_SIGN_FINISH
    /// </summary>
    RESP_CODE_SIGNATURE = 0x14,

    /// <summary>
    /// Custom variables data
    /// Contains user-defined configuration variables
    /// Response to CMD_GET_CUSTOM_VARS
    /// </summary>
    RESP_CODE_CUSTOM_VARS = 0x15,

    /// <summary>
    /// Advertisement path information
    /// Contains the last known advertisement path for a contact
    /// Response to CMD_GET_ADVERT_PATH
    /// </summary>
    RESP_CODE_ADVERT_PATH = 0x16,

    /// <summary>
    /// Tuning parameters response
    /// Contains advanced radio tuning settings
    /// Response to CMD_GET_TUNING_PARAMS
    /// </summary>
    RESP_CODE_TUNING_PARAMS = 0x17,

    /// <summary>
    /// Statistics data response
    /// Contains device performance and operation statistics
    /// Response to CMD_GET_STATS
    /// </summary>
    RESP_CODE_STATS = 0x18,

    /// <summary>
    /// Auto-add configuration response
    /// Contains contact auto-add configuration flags
    /// Response to CMD_GET_AUTOADD_CONFIG
    /// </summary>
    RESP_CODE_AUTOADD_CONFIG = 0x19,

    // ===== ASYNCHRONOUS PUSH NOTIFICATIONS (0x80-0x8F) =====
    // These are sent by the device spontaneously, not as responses to commands

    /// <summary>
    /// Advertisement push notification
    /// Asynchronous notification when a node advertisement is received
    /// Contains the advertising node's information
    /// </summary>
    PUSH_CODE_ADVERT = 0x80,

    /// <summary>
    /// Login success notification
    /// Asynchronous notification that login to a room server succeeded
    /// Response to CMD_SEND_LOGIN when authentication is successful
    /// </summary>
    PUSH_CODE_LOGIN_SUCCESS = 0x85,

    /// <summary>
    /// Login failure notification
    /// Asynchronous notification that login to a room server failed
    /// Response to CMD_SEND_LOGIN when authentication is rejected
    /// </summary>
    PUSH_CODE_LOGIN_FAIL = 0x86,

    /// <summary>
    /// Status response notification
    /// Asynchronous response containing node status information
    /// Reply to CMD_SEND_STATUS_REQ when target node responds
    /// </summary>
    PUSH_CODE_STATUS_RESPONSE = 0x87,

    /// <summary>
    /// Raw data packet received
    /// Asynchronous notification when a custom binary packet is received
    /// Contains user-defined payload from CMD_SEND_RAW_DATA
    /// </summary>
    PUSH_CODE_RAW_DATA = 0x84,

    /// <summary>
    /// Messages waiting notification
    /// Asynchronous notification that new messages have arrived
    /// Prompts the app to call CMD_SYNC_NEXT_MESSAGE to retrieve them
    /// </summary>
    PUSH_CODE_MSG_WAITING = 0x83,

    /// <summary>
    /// Send confirmation notification
    /// Asynchronous notification that a sent message was acknowledged by recipient
    /// Contains the ACK code and round-trip time for tracking
    /// </summary>
    PUSH_CODE_SEND_CONFIRMED = 0x82,

    /// <summary>
    /// New contact advertisement received
    /// Asynchronous notification when a new contact's advertisement is received
    /// Device may add the contact automatically depending on settings
    /// </summary>
    PUSH_CODE_NEW_ADVERT = 0x81,

    /// <summary>
    /// Log RX data push notification
    /// Asynchronous notification containing received RF packet log data
    /// Includes SNR, RSSI, and raw payload from over-the-air reception
    /// Used for monitoring mesh activity and debugging
    /// </summary>
    RESP_CODE_LOG_RX_DATA = 0x88,

    /// <summary>
    /// Trace path data response
    /// Asynchronous response containing path trace results
    /// Reply to CMD_SEND_TRACE_PATH with hop-by-hop SNR measurements
    /// Contains the complete path and signal quality data
    /// </summary>
    PUSH_CODE_TRACE_DATA = 0x89,

    /// <summary>
    /// Telemetry response notification
    /// Asynchronous response containing sensor telemetry data
    /// Reply to CMD_SEND_TELEMETRY_REQ when target node responds
    /// </summary>
    PUSH_CODE_TELEMETRY_RESPONSE = 0x8B,

    /// <summary>
    /// Binary response notification
    /// Asynchronous response to a binary request
    /// Reply to CMD_SEND_BINARY_REQ with custom binary data
    /// </summary>
    PUSH_CODE_BINARY_RESPONSE = 0x8C,

    /// <summary>
    /// Path discovery response (asynchronous push notification)
    /// Contains discovered route information from mesh path discovery
    /// This is NOT an immediate command response but arrives asynchronously
    /// when path discovery completes (typically after CMD_SEND_PATH_DISCOVERY_REQ)
    /// Payload format: [outbound_path_length][outbound_path_bytes][inbound_path_length][inbound_path_bytes]
    /// Wire protocol value: 0x8D (141 decimal)
    /// </summary>
    RESP_CODE_PATH_RESPONSE = 0x8D,

    /// <summary>
    /// Control data received notification
    /// Asynchronous notification when a control packet is received
    /// Reply to CMD_SEND_CONTROL_DATA or spontaneous control messages
    /// </summary>
    PUSH_CODE_CONTROL_DATA = 0x8E,
}