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