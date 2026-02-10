// <copyright file="LogRxDataFrameSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using MeshCore.Net.SDK.Models;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Provides serialization and deserialization for RF log data frames
    /// </summary>
    public sealed class LogRxDataFrameSerialization : IBinaryDeserializer<LogRxDataFrame>
    {
        private static readonly Lazy<LogRxDataFrameSerialization> _instance =
            new Lazy<LogRxDataFrameSerialization>(() => new LogRxDataFrameSerialization());

        private readonly ILogger<LogRxDataFrameSerialization> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogRxDataFrameSerialization"/> class
        /// </summary>
        private LogRxDataFrameSerialization()
        {
            _logger = NullLogger<LogRxDataFrameSerialization>.Instance;
        }

        /// <summary>
        /// Gets the singleton instance of the LogRxDataFrameSerialization class
        /// </summary>
        public static LogRxDataFrameSerialization Instance => _instance.Value;

        /// <summary>
        /// Attempts to deserialize RF log data frame from binary payload
        /// </summary>
        /// <param name="data">The raw binary data from the device</param>
        /// <param name="result">When this method returns, contains the deserialized LogRxDataFrame if successful; otherwise, null</param>
        /// <returns>True if deserialization succeeded; otherwise, false</returns>
        public bool TryDeserialize(byte[] data, out LogRxDataFrame? result)
        {
            result = null;

            // Minimum frame: RESP_CODE_LOG_RX_DATA (0x88) + SNR + RSSI = 3 bytes
            if (data == null || data.Length < 3)
            {
                _logger.LogWarning("RF log data payload too short: {Length} bytes (minimum 3 required)", data?.Length ?? 0);
                return false;
            }

            // Verify this is an RX_LOG_DATA frame (0x88)
            if (data[0] != 0x88)
            {
                _logger.LogWarning("Invalid RF log data marker: expected 0x88, got 0x{Marker:X2}", data[0]);
                return false;
            }

            try
            {
                // Parse frame structure:
                // Byte 0: 0x88 (RX_LOG_DATA marker)
                // Byte 1: SNR (Signal-to-Noise Ratio)
                // Byte 2: RSSI (Received Signal Strength Indicator)
                // Bytes 3+: Payload data
                var snr = (sbyte)data[1];
                var rssi = (sbyte)data[2];

                // Extract remaining payload (if any)
                var payloadLength = data.Length - 3;
                var payload = new byte[payloadLength];
                if (payloadLength > 0)
                {
                    Buffer.BlockCopy(data, 3, payload, 0, payloadLength);
                }

                result = new LogRxDataFrame
                {
                    SignalToNoiseRatio = snr,
                    ReceivedSignalStrength = rssi,
                    Payload = payload
                };

                _logger.LogDebug("Successfully parsed RF log frame: SNR={SNR}dB, RSSI={RSSI}dBm, PayloadLen={PayloadLen}",
                    snr, rssi, payloadLength);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing RF log data frame");
                return false;
            }
        }

        /// <summary>
        /// Deserializes RF log data frame from binary payload
        /// </summary>
        /// <param name="data">The raw binary data from the device</param>
        /// <returns>The deserialized LogRxDataFrame</returns>
        /// <exception cref="InvalidOperationException">Thrown when deserialization fails</exception>
        public LogRxDataFrame Deserialize(byte[] data)
        {
            if (!TryDeserialize(data, out var result) || result == null)
            {
                throw new InvalidOperationException("Failed to deserialize RF log data frame");
            }

            return result;
        }
    }
}