namespace MeshCore.Net.SDK.Protocol;

/// <summary>
/// MeshCore command definitions based on the Companion Radio Protocol
/// </summary>
public enum MeshCoreCommand : byte
{
    /// <summary>
    /// Start application mode with protocol version
    /// </summary>
    CMD_APP_START = 1,
    
    /// <summary>
    /// Send text message to contact
    /// </summary>
    CMD_SEND_TXT_MSG = 2,
    
    /// <summary>
    /// Send text message to channel
    /// </summary>
    CMD_SEND_CHANNEL_TXT_MSG = 3,
    
    /// <summary>
    /// Get contacts list
    /// </summary>
    CMD_CONTACT_LIST_GET = 4,
    
    /// <summary>
    /// Get current device time
    /// </summary>
    CMD_GET_DEVICE_TIME = 5,
    
    /// <summary>
    /// Set device time
    /// </summary>
    CMD_SET_DEVICE_TIME = 6,
    
    /// <summary>
    /// Send self advertisement to network
    /// </summary>
    CMD_SEND_SELF_ADVERT = 7,
    
    /// <summary>
    /// Set advertisement name for device
    /// </summary>
    CMD_SET_ADVERT_NAME = 8,
    
    /// <summary>
    /// Add or update contact information
    /// </summary>
    CMD_ADD_UPDATE_CONTACT = 9,
    
    /// <summary>
    /// Synchronize next message in queue
    /// </summary>
    CMD_SYNC_NEXT_MESSAGE = 10,
    
    /// <summary>
    /// Set radio parameters
    /// </summary>
    CMD_SET_RADIO_PARAMS = 11,
    
    /// <summary>
    /// Set radio transmit power
    /// </summary>
    CMD_SET_RADIO_TX_POWER = 12,
    
    /// <summary>
    /// Reset routing path information
    /// </summary>
    CMD_RESET_PATH = 13,
    
    /// <summary>
    /// Set advertisement latitude/longitude coordinates
    /// </summary>
    CMD_SET_ADVERT_LATLON = 14,
    
    /// <summary>
    /// Remove contact from device
    /// </summary>
    CMD_REMOVE_CONTACT = 15,
    
    /// <summary>
    /// Share contact information
    /// </summary>
    CMD_SHARE_CONTACT = 16,
    
    /// <summary>
    /// Export contact information
    /// </summary>
    CMD_EXPORT_CONTACT = 17,
    
    /// <summary>
    /// Import contact information
    /// </summary>
    CMD_IMPORT_CONTACT = 18,
    
    /// <summary>
    /// Reboot device
    /// </summary>
    CMD_REBOOT = 19,
    
    /// <summary>
    /// Get battery level and storage information
    /// </summary>
    CMD_GET_BATT_AND_STORAGE = 20,
    
    /// <summary>
    /// Set tuning parameters
    /// </summary>
    CMD_SET_TUNING_PARAMS = 21,
    
    /// <summary>
    /// Query device information
    /// </summary>
    CMD_DEVICE_QUERY = 22,
    
    /// <summary>
    /// Export private key
    /// </summary>
    CMD_EXPORT_PRIVATE_KEY = 23,
    
    /// <summary>
    /// Import private key
    /// </summary>
    CMD_IMPORT_PRIVATE_KEY = 24,
    
    /// <summary>
    /// Send raw data
    /// </summary>
    CMD_SEND_RAW_DATA = 25,
    
    /// <summary>
    /// Send login credentials
    /// </summary>
    CMD_SEND_LOGIN = 26,
    
    /// <summary>
    /// Send status request
    /// </summary>
    CMD_SEND_STATUS_REQ = 27,
    
    /// <summary>
    /// Check if device has network connection
    /// </summary>
    CMD_HAS_CONNECTION = 28,
    
    /// <summary>
    /// Logout from network
    /// </summary>
    CMD_LOGOUT = 29,
    
    /// <summary>
    /// Get contact by key
    /// </summary>
    CMD_GET_CONTACT_BY_KEY = 30,
    
    /// <summary>
    /// Get channel information
    /// </summary>
    CMD_GET_CHANNEL = 31,
    
    /// <summary>
    /// Set channel configuration
    /// </summary>
    CMD_SET_CHANNEL = 32,
    
    /// <summary>
    /// Sign start
    /// </summary>
    CMD_SIGN_START = 33,
    
    /// <summary>
    /// Sign data
    /// </summary>
    CMD_SIGN_DATA = 34,
    
    /// <summary>
    /// Sign finish
    /// </summary>
    CMD_SIGN_FINISH = 35,
    
    /// <summary>
    /// Send trace path request
    /// </summary>
    CMD_SEND_TRACE_PATH = 36,
    
    /// <summary>
    /// Set device PIN
    /// </summary>
    CMD_SET_DEVICE_PIN = 37,
    
    /// <summary>
    /// Set other device parameters
    /// </summary>
    CMD_SET_OTHER_PARAMS = 38,
    
    /// <summary>
    /// Send telemetry request
    /// </summary>
    CMD_SEND_TELEMETRY_REQ = 39,
    
    /// <summary>
    /// Get custom variables
    /// </summary>
    CMD_GET_CUSTOM_VARS = 40,
    
    /// <summary>
    /// Set custom variable
    /// </summary>
    CMD_SET_CUSTOM_VAR = 41,
    
    /// <summary>
    /// Get advertisement path
    /// </summary>
    CMD_GET_ADVERT_PATH = 42,
    
    /// <summary>
    /// Get tuning parameters
    /// </summary>
    CMD_GET_TUNING_PARAMS = 43,
    
    /// <summary>
    /// Send binary request
    /// </summary>
    CMD_SEND_BINARY_REQ = 50,
    
    /// <summary>
    /// Perform factory reset
    /// </summary>
    CMD_FACTORY_RESET = 51,
    
    /// <summary>
    /// Send path discovery request
    /// </summary>
    CMD_SEND_PATH_DISCOVERY_REQ = 52,
    
    /// <summary>
    /// Set flood scope for message broadcasting
    /// </summary>
    CMD_SET_FLOOD_SCOPE = 54,
    
    /// <summary>
    /// Send control data
    /// </summary>
    CMD_SEND_CONTROL_DATA = 55,
    
    /// <summary>
    /// Get device statistics
    /// </summary>
    CMD_GET_STATS = 56,
    
    /// <summary>
    /// Send anonymous request
    /// </summary>
    CMD_SEND_ANON_REQ = 57,
    
    /// <summary>
    /// Set auto-add configuration
    /// </summary>
    CMD_SET_AUTOADD_CONFIG = 58,
    
    /// <summary>
    /// Get auto-add configuration
    /// </summary>
    CMD_GET_AUTOADD_CONFIG = 59
}

/// <summary>
/// MeshCore response status codes
/// </summary>
public enum MeshCoreStatus : byte
{
    /// <summary>
    /// Operation completed successfully
    /// </summary>
    Success = 0x00,
    
    /// <summary>
    /// Command is not recognized or invalid
    /// </summary>
    InvalidCommand = 0x01,
    
    /// <summary>
    /// Parameter provided is invalid
    /// </summary>
    InvalidParameter = 0x02,
    
    /// <summary>
    /// Device encountered an error
    /// </summary>
    DeviceError = 0x03,
    
    /// <summary>
    /// Network communication error occurred
    /// </summary>
    NetworkError = 0x04,
    
    /// <summary>
    /// Operation timed out
    /// </summary>
    TimeoutError = 0x05,
    
    /// <summary>
    /// Unknown or unspecified error
    /// </summary>
    UnknownError = 0xFF
}

/// <summary>
/// MeshCore response codes
/// </summary>
public enum MeshCoreResponseCode : byte
{
    /// <summary>
    /// Operation completed successfully
    /// </summary>
    RESP_CODE_OK = 0,
    
    /// <summary>
    /// Error occurred during operation
    /// </summary>
    RESP_CODE_ERR = 1,
    
    /// <summary>
    /// Start of contacts list transmission
    /// </summary>
    RESP_CODE_CONTACTS_START = 2,
    
    /// <summary>
    /// Contact information data
    /// </summary>
    RESP_CODE_CONTACT = 3,
    
    /// <summary>
    /// End of contacts list transmission
    /// </summary>
    RESP_CODE_END_OF_CONTACTS = 4,
    
    /// <summary>
    /// Self device information
    /// </summary>
    RESP_CODE_SELF_INFO = 5,
    
    /// <summary>
    /// Message sent confirmation
    /// </summary>
    RESP_CODE_SENT = 6,
    
    /// <summary>
    /// Contact message received
    /// </summary>
    RESP_CODE_CONTACT_MSG_RECV = 7,
    
    /// <summary>
    /// Channel message received
    /// </summary>
    RESP_CODE_CHANNEL_MSG_RECV = 8,
    
    /// <summary>
    /// Current device time
    /// </summary>
    RESP_CODE_CURR_TIME = 9,
    
    /// <summary>
    /// No more messages available
    /// </summary>
    RESP_CODE_NO_MORE_MESSAGES = 10,
    
    /// <summary>
    /// Exported contact information
    /// </summary>
    RESP_CODE_EXPORT_CONTACT = 11,
    
    /// <summary>
    /// Battery and storage information
    /// </summary>
    RESP_CODE_BATT_AND_STORAGE = 12,
    
    /// <summary>
    /// Device information response
    /// </summary>
    RESP_CODE_DEVICE_INFO = 13,
    
    /// <summary>
    /// Private key response
    /// </summary>
    RESP_CODE_PRIVATE_KEY = 14,
    
    /// <summary>
    /// Feature or command is disabled
    /// </summary>
    RESP_CODE_DISABLED = 15,
    
    /// <summary>
    /// Contact message received (version 3 protocol)
    /// </summary>
    RESP_CODE_CONTACT_MSG_RECV_V3 = 16,
    
    /// <summary>
    /// Channel message received (version 3 protocol)
    /// </summary>
    RESP_CODE_CHANNEL_MSG_RECV_V3 = 17,
    
    /// <summary>
    /// Channel information response
    /// </summary>
    RESP_CODE_CHANNEL_INFO = 18,
    
    /// <summary>
    /// Sign start response
    /// </summary>
    RESP_CODE_SIGN_START = 19,
    
    /// <summary>
    /// Signature response
    /// </summary>
    RESP_CODE_SIGNATURE = 20,
    
    /// <summary>
    /// Custom variables data
    /// </summary>
    RESP_CODE_CUSTOM_VARS = 21,
    
    /// <summary>
    /// Advertisement path information
    /// </summary>
    RESP_CODE_ADVERT_PATH = 22,
    
    /// <summary>
    /// Tuning parameters response
    /// </summary>
    RESP_CODE_TUNING_PARAMS = 23,
    
    /// <summary>
    /// Statistics data
    /// </summary>
    RESP_CODE_STATS = 24,
    
    /// <summary>
    /// Auto-add configuration response
    /// </summary>
    RESP_CODE_AUTOADD_CONFIG = 25
}

/// <summary>
/// Protocol frame constants
/// </summary>
public static class ProtocolConstants
{
    /// <summary>
    /// Frame start byte for inbound frames (PC to radio)
    /// </summary>
    public const byte FRAME_START_INBOUND = 0x3C;   // '<' PC to radio
    
    /// <summary>
    /// Frame start byte for outbound frames (radio to PC)
    /// </summary>
    public const byte FRAME_START_OUTBOUND = 0x3E;  // '>' radio to PC
    
    /// <summary>
    /// Size of frame header in bytes (start byte + 2 length bytes)
    /// </summary>
    public const int FRAME_HEADER_SIZE = 3;          // start byte + 2 length bytes
    
    /// <summary>
    /// Maximum allowed frame size in bytes
    /// </summary>
    public const int MAX_FRAME_SIZE = 1024;          // Maximum frame size
    
    /// <summary>
    /// Default timeout for operations in milliseconds
    /// </summary>
    public const int DEFAULT_TIMEOUT_MS = 5000;     // Default operation timeout
}