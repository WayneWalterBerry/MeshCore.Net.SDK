// <copyright file="MeshCoreFrame.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Protocol
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a MeshCore protocol frame
    /// </summary>
    public class MeshCoreFrame
    {
        /// <summary>
        /// Gets or sets the frame start byte indicating frame direction
        /// </summary>
        [JsonPropertyName("start_byte")]
        public byte StartByte { get; set; }

        /// <summary>
        /// Gets or sets the length of the payload in bytes
        /// </summary>
        [JsonPropertyName("length")]
        public ushort Length { get; set; }

        /// <summary>
        /// Gets or sets the frame payload data
        /// </summary>
        [JsonIgnore]
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets whether this is an inbound frame (PC to radio)
        /// </summary>
        [JsonPropertyName("is_inbound")]
        public bool IsInbound => StartByte == ProtocolConstants.FRAME_START_INBOUND;

        /// <summary>
        /// Gets whether this is an outbound frame (radio to PC)
        /// </summary>
        [JsonPropertyName("is_outbound")]
        public bool IsOutbound => StartByte == ProtocolConstants.FRAME_START_OUTBOUND;

        /// <summary>
        /// Gets the frame direction as a string for JSON serialization
        /// </summary>
        [JsonPropertyName("direction")]
        public string Direction => IsInbound ? "Inbound" : "Outbound";

        /// <summary>
        /// Gets the command from the payload for JSON serialization
        /// </summary>
        [JsonPropertyName("command")]
        public string? Command => GetCommand()?.ToString();

        /// <summary>
        /// Gets the response code from the payload for JSON serialization
        /// </summary>
        [JsonPropertyName("response_code")]
        public string? ResponseCode => GetResponseCode()?.ToString();

        /// <summary>
        /// Gets the status from the payload for JSON serialization
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status => GetStatus()?.ToString();

        /// <summary>
        /// Gets the payload as hex string for JSON serialization
        /// </summary>
        [JsonPropertyName("payload_hex")]
        public string PayloadHex => Payload.Length > 0
            ? Convert.ToHexString(Payload).ToLowerInvariant()
            : string.Empty;

        /// <summary>
        /// Gets the payload length for JSON serialization
        /// </summary>
        [JsonPropertyName("payload_length")]
        public int PayloadLength => Payload.Length;

        /// <summary>
        /// Creates a new inbound frame (PC to radio)
        /// </summary>
        /// <param name="payload">The payload data for the frame</param>
        /// <returns>A new inbound MeshCore frame</returns>
        public static MeshCoreFrame CreateInbound(byte[] payload)
        {
            return new MeshCoreFrame
            {
                StartByte = ProtocolConstants.FRAME_START_INBOUND,
                Length = (ushort)payload.Length,
                Payload = payload
            };
        }

        /// <summary>
        /// Creates a new outbound frame (radio to PC)
        /// </summary>
        /// <param name="payload">The payload data for the frame</param>
        /// <returns>A new outbound MeshCore frame</returns>
        public static MeshCoreFrame CreateOutbound(byte[] payload)
        {
            return new MeshCoreFrame
            {
                StartByte = ProtocolConstants.FRAME_START_OUTBOUND,
                Length = (ushort)payload.Length,
                Payload = payload
            };
        }

        /// <summary>
        /// Converts the frame to a byte array for transmission
        /// </summary>
        /// <returns>The frame as a byte array ready for transmission</returns>
        public byte[] ToByteArray()
        {
            var result = new List<byte>
            {
                StartByte,
                (byte)(Length & 0xFF),        // Little-endian low byte
                (byte)((Length >> 8) & 0xFF)  // Little-endian high byte
            };
            result.AddRange(Payload);
            return result.ToArray();
        }

        /// <summary>
        /// Parses a byte array into a frame
        /// </summary>
        /// <param name="data">The byte array to parse</param>
        /// <returns>A parsed MeshCore frame, or null if parsing failed</returns>
        public static MeshCoreFrame? Parse(byte[] data)
        {
            if (data.Length < ProtocolConstants.FRAME_HEADER_SIZE)
                return null;

            var startByte = data[0];
            if (startByte != ProtocolConstants.FRAME_START_INBOUND &&
                startByte != ProtocolConstants.FRAME_START_OUTBOUND)
                return null;

            var length = (ushort)(data[1] | (data[2] << 8)); // Little-endian

            if (data.Length < ProtocolConstants.FRAME_HEADER_SIZE + length)
                return null;

            var payload = new byte[length];
            Array.Copy(data, ProtocolConstants.FRAME_HEADER_SIZE, payload, 0, length);

            return new MeshCoreFrame
            {
                StartByte = startByte,
                Length = length,
                Payload = payload
            };
        }

        /// <summary>
        /// Gets the command from the payload (first byte)
        /// </summary>
        /// <returns>The command if valid, otherwise null</returns>
        public MeshCoreCommand? GetCommand()
        {
            if (Payload.Length == 0)
                return null;

            if (Enum.IsDefined(typeof(MeshCoreCommand), Payload[0]))
                return (MeshCoreCommand)Payload[0];

            return null;
        }

        /// <summary>
        /// Gets the response code from the payload (first byte for outbound frames)
        /// </summary>
        /// <returns>The response code if valid, otherwise null</returns>
        public MeshCoreResponseCode? GetResponseCode()
        {
            if (Payload.Length == 0)
                return null;

            if (Enum.IsDefined(typeof(MeshCoreResponseCode), Payload[0]))
            {
                return (MeshCoreResponseCode)Payload[0];
            }

            return null;
        }

        /// <summary>
        /// Gets the status from the payload (second byte for error responses)
        /// </summary>
        /// <returns>The status code if valid, otherwise null</returns>
        public MeshCoreStatus? GetStatus()
        {
            // For outbound frames, check the response code
            if (IsOutbound && Payload.Length >= 1)
            {
                var responseCode = GetResponseCode();
                if (responseCode == MeshCoreResponseCode.RESP_CODE_ERR && Payload.Length >= 2)
                {
                    if (Enum.IsDefined(typeof(MeshCoreStatus), Payload[1]))
                        return (MeshCoreStatus)Payload[1];
                }
                else if (responseCode == MeshCoreResponseCode.RESP_CODE_OK)
                {
                    return MeshCoreStatus.Success;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the data payload (excluding command and status bytes)
        /// </summary>
        /// <returns>The data portion of the payload</returns>
        public byte[] GetDataPayload()
        {
            if (Payload.Length <= 2)
                return Array.Empty<byte>();

            var data = new byte[Payload.Length - 2];
            Array.Copy(Payload, 2, data, 0, data.Length);
            return data;
        }

        /// <summary>
        /// Returns a JSON representation of the frame matching the MeshCore protocol format.
        /// </summary>
        /// <returns>A JSON string describing the frame.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
        }
    }
}