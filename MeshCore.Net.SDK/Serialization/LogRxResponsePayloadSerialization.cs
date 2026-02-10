// <copyright file="LogRxResponsePayloadSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Provides serialization and deserialization for MeshCore response payloads embedded within RF log data frames
    /// </summary>
    public sealed class LogRxResponsePayloadSerialization : IBinaryDeserializer<LogRxResponsePayload>
    {
        private static readonly Lazy<LogRxResponsePayloadSerialization> _instance =
            new Lazy<LogRxResponsePayloadSerialization>(() => new LogRxResponsePayloadSerialization());

        private readonly ILogger<LogRxResponsePayloadSerialization> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRxResponsePayloadSerialization"/> class
        /// </summary>
        private LogRxResponsePayloadSerialization()
        {
            _logger = NullLogger<LogRxResponsePayloadSerialization>.Instance;
        }

        /// <summary>
        /// Gets the singleton instance of the LogRxResponsePayloadSerialization class
        /// </summary>
        public static LogRxResponsePayloadSerialization Instance => _instance.Value;

        /// <summary>
        /// Attempts to deserialize a MeshCore response payload from binary data
        /// </summary>
        /// <param name="data">The raw binary data containing the response payload</param>
        /// <param name="result">When this method returns, contains the deserialized LogRxResponsePayload if successful; otherwise, null</param>
        /// <returns>True if deserialization succeeded; otherwise, false</returns>
        public bool TryDeserialize(byte[] data, out LogRxResponsePayload? result)
        {
            result = null;

            // Minimum payload: response code (1 byte)
            if (data == null || data.Length < 1)
            {
                _logger.LogWarning("Response payload data too short: {Length} bytes (minimum 1 required)", data?.Length ?? 0);
                return false;
            }

            try
            {
                // Parse response payload structure:
                // Byte 0: MeshCore Response Code
                // Bytes 1+: Response-specific data
                var responseCodeByte = data[0];

                // Extract remaining data (if any)
                var dataLength = data.Length - 1;
                var responseData = new byte[dataLength];
                if (dataLength > 0)
                {
                    Buffer.BlockCopy(data, 1, responseData, 0, dataLength);
                }

                result = new LogRxResponsePayload
                {
                    ResponseCodeByte = responseCodeByte,
                    Payload = responseData
                };

                _logger.LogDebug("Successfully parsed response payload: ResponseCode=0x{ResponseCode:X2}, DataLen={DataLen}",
                    responseCodeByte, dataLength);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing response payload data");
                return false;
            }
        }

        /// <summary>
        /// Deserializes a MeshCore response payload from binary data
        /// </summary>
        /// <param name="data">The raw binary data containing the response payload</param>
        /// <returns>The deserialized LogRxResponsePayload</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public LogRxResponsePayload Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize response payload data");
            }

            return result;
        }
    }
}