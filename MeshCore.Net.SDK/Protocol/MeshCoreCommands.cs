namespace MeshCore.Net.SDK.Protocol;

/// <summary>
/// MeshCore command definitions based on the Companion Radio Protocol
/// </summary>
public enum MeshCoreCommand : byte
{
    // Device Commands
    CMD_DEVICE_QUERY = 22,
    CMD_GET_DEVICE_INFO = 23,
    CMD_SET_DEVICE_TIME = 24,
    CMD_GET_DEVICE_TIME = 25,
    CMD_RESET_DEVICE = 26,
    
    // Contact Commands
    CMD_GET_CONTACTS = 30,
    CMD_ADD_CONTACT = 31,
    CMD_DELETE_CONTACT = 32,
    CMD_UPDATE_CONTACT = 33,
    
    // Message Commands
    CMD_SEND_MESSAGE = 40,
    CMD_GET_MESSAGES = 41,
    CMD_DELETE_MESSAGE = 42,
    CMD_MARK_MESSAGE_READ = 43,
    
    // Network Commands
    CMD_GET_NETWORK_STATUS = 50,
    CMD_SCAN_NETWORKS = 51,
    CMD_CONNECT_NETWORK = 52,
    CMD_DISCONNECT_NETWORK = 53,
    
    // Configuration Commands
    CMD_GET_CONFIG = 60,
    CMD_SET_CONFIG = 61,
    CMD_RESET_CONFIG = 62
}

/// <summary>
/// MeshCore response status codes
/// </summary>
public enum MeshCoreStatus : byte
{
    Success = 0x00,
    InvalidCommand = 0x01,
    InvalidParameter = 0x02,
    DeviceError = 0x03,
    NetworkError = 0x04,
    TimeoutError = 0x05,
    UnknownError = 0xFF
}

/// <summary>
/// Protocol frame constants
/// </summary>
public static class ProtocolConstants
{
    public const byte FRAME_START_INBOUND = 0x3C;   // '<' PC to radio
    public const byte FRAME_START_OUTBOUND = 0x3E;  // '>' radio to PC
    public const int FRAME_HEADER_SIZE = 3;          // start byte + 2 length bytes
    public const int MAX_FRAME_SIZE = 1024;          // Maximum frame size
    public const int DEFAULT_TIMEOUT_MS = 5000;     // Default operation timeout
}