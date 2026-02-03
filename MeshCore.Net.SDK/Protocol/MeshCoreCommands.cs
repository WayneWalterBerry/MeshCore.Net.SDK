namespace MeshCore.Net.SDK.Protocol;

/// <summary>
/// MeshCore command definitions based on the Companion Radio Protocol
/// </summary>
public enum MeshCoreCommand : byte
{
    // Core Device Commands
    CMD_APP_START = 0x01,
    CMD_GET_DEVICE_TIME = 0x05,
    CMD_SET_DEVICE_TIME = 0x06,
    CMD_SEND_SELF_ADVERT = 0x07,
    CMD_SET_ADVERT_NAME = 0x08,
    CMD_ADD_UPDATE_CONTACT = 0x09,
    CMD_SET_RADIO_PARAMS = 0x0B,
    CMD_SET_RADIO_TX_POWER = 0x0C,
    CMD_SYNC_NEXT_MESSAGE = 0x10,
    CMD_RESET_PATH = 0x13,
    CMD_SET_ADVERT_LATLON = 0x14,
    CMD_REMOVE_CONTACT = 0x15,
    CMD_DEVICE_QUERY = 0x16,
    CMD_EXPORT_CONTACT = 0x17,
    CMD_IMPORT_CONTACT = 0x18,
    CMD_REBOOT = 0x19,
    
    // Messaging Commands
    CMD_SEND_TXT_MSG = 0x02,
    CMD_SEND_CHANNEL_TXT_MSG = 0x03,
    CMD_GET_CONTACTS = 0x04,
    CMD_SHARE_CONTACT = 0x16,
    
    // Network Commands
    CMD_SEND_LOGIN = 0x1A,
    CMD_SEND_STATUS_REQ = 0x1B,
    CMD_HAS_CONNECTION = 0x1C,
    CMD_LOGOUT = 0x1D,
    CMD_GET_BATT_AND_STORAGE = 0x20,
    CMD_SET_TUNING_PARAMS = 0x21,
    CMD_SEND_PATH_DISCOVERY_REQ = 0x24,
    CMD_SEND_RAW_DATA = 0x25,
    CMD_SEND_TELEMETRY_REQ = 0x27,
    CMD_GET_CHANNEL = 0x32,
    CMD_SET_CHANNEL = 0x33,
    CMD_SEND_TRACE_PATH = 0x36,
    CMD_SET_OTHER_PARAMS = 0x38,
    CMD_SEND_BINARY_REQ = 0x50,
    CMD_FACTORY_RESET = 0x51,
    CMD_SET_FLOOD_SCOPE = 0x54,
    CMD_SEND_CONTROL_DATA = 0x55,
    CMD_GET_STATS = 0x56
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
/// MeshCore response codes
/// </summary>
public enum MeshCoreResponseCode : byte
{
    RESP_CODE_OK = 0x00,
    RESP_CODE_ERR = 0x01,
    RESP_CODE_CONTACTS_START = 0x02,
    RESP_CODE_CONTACT = 0x03,
    RESP_CODE_END_OF_CONTACTS = 0x04,
    RESP_CODE_SELF_INFO = 0x05,
    RESP_CODE_SENT = 0x06,
    RESP_CODE_CONTACT_MSG_RECV = 0x07,
    RESP_CODE_CHANNEL_MSG_RECV = 0x08,
    RESP_CODE_CURR_TIME = 0x09,
    RESP_CODE_NO_MORE_MESSAGES = 0x0A,
    RESP_CODE_EXPORT_CONTACT = 0x0B,
    RESP_CODE_BATT_AND_STORAGE = 0x0C,
    RESP_CODE_DEVICE_INFO = 0x0D,
    RESP_CODE_DISABLED = 0x0F,
    RESP_CODE_CONTACT_MSG_RECV_V3 = 0x10,
    RESP_CODE_CHANNEL_MSG_RECV_V3 = 0x11,
    RESP_CODE_CUSTOM_VARS = 0x15,
    RESP_CODE_ADVERT_PATH = 0x16,
    RESP_CODE_STATS = 0x18
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