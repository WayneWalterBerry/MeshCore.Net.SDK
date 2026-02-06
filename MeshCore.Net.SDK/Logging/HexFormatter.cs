// <copyright file="HexFormatter.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Logging;

/// <summary>
/// Utility class for formatting binary data as hexadecimal strings
/// Provides consistent hex formatting across the SDK for debugging and logging
/// </summary>
public static class HexFormatter
{
    /// <summary>
    /// Converts a byte array to a hex string without separators (e.g., "3E020001")
    /// Matches Python CLI format: "Received data: 3E020001"
    /// </summary>
    /// <param name="data">The byte array to convert</param>
    /// <returns>A hex string representation</returns>
    public static string ToHexString(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        return BitConverter.ToString(data).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// Converts a byte array to a hex string with space separators (e.g., "3E 02 00 01")
    /// Useful for more readable output
    /// </summary>
    /// <param name="data">The byte array to convert</param>
    /// <returns>A hex string with space separators</returns>
    public static string ToHexStringSpaced(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        return BitConverter.ToString(data).Replace("-", " ").ToLowerInvariant();
    }

    /// <summary>
    /// Converts a portion of a byte array to a hex string without separators
    /// </summary>
    /// <param name="data">The byte array to convert</param>
    /// <param name="offset">Starting offset in the array</param>
    /// <param name="count">Number of bytes to convert</param>
    /// <returns>A hex string representation</returns>
    public static string ToHexString(byte[] data, int offset, int count)
    {
        if (data == null || data.Length == 0 || count == 0)
        {
            return string.Empty;
        }

        if (offset < 0 || count < 0 || offset + count > data.Length)
        {
            throw new ArgumentException("Invalid offset or count");
        }

        return BitConverter.ToString(data, offset, count).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>
    /// Converts a single byte to a two-character hex string (e.g., "3E")
    /// </summary>
    /// <param name="value">The byte to convert</param>
    /// <returns>A two-character hex string</returns>
    public static string ToHexString(byte value)
    {
        return value.ToString("x2");
    }

    /// <summary>
    /// Converts a 16-bit value to a four-character hex string (e.g., "A52F")
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="littleEndian">Whether to format as little-endian (default: true)</param>
    /// <returns>A four-character hex string</returns>
    public static string ToHexString(ushort value, bool littleEndian = true)
    {
        if (littleEndian)
        {
            var bytes = BitConverter.GetBytes(value);
            return ToHexString(bytes);
        }

        return value.ToString("x4");
    }

    /// <summary>
    /// Converts a 32-bit value to an eight-character hex string
    /// </summary>
    /// <param name="value">The value to convert</param>
    /// <param name="littleEndian">Whether to format as little-endian (default: true)</param>
    /// <returns>An eight-character hex string</returns>
    public static string ToHexString(uint value, bool littleEndian = true)
    {
        if (littleEndian)
        {
            var bytes = BitConverter.GetBytes(value);
            return ToHexString(bytes);
        }

        return value.ToString("x8");
    }

    /// <summary>
    /// Formats a command frame for logging (e.g., "3C02001603" for Device Query)
    /// </summary>
    /// <param name="command">The command byte</param>
    /// <param name="payload">Optional payload bytes</param>
    /// <returns>A formatted frame string matching protocol specification</returns>
    public static string FormatCommandFrame(byte command, byte[]? payload = null)
    {
        var payloadLength = payload?.Length ?? 0;
        var frame = new List<byte>
        {
            0x3C, // START_BYTE_OUTBOUND
            (byte)(payloadLength & 0xFF),
            (byte)((payloadLength >> 8) & 0xFF),
            command
        };

        if (payload != null && payload.Length > 0)
        {
            frame.AddRange(payload);
        }

        return ToHexString(frame.ToArray());
    }

    /// <summary>
    /// Formats a response frame for logging (e.g., "3E020001" for OK response)
    /// </summary>
    /// <param name="responseCode">The response code byte</param>
    /// <param name="payload">Optional payload bytes</param>
    /// <returns>A formatted frame string matching protocol specification</returns>
    public static string FormatResponseFrame(byte responseCode, byte[]? payload = null)
    {
        var payloadLength = (payload?.Length ?? 0) + 1; // +1 for response code
        var frame = new List<byte>
        {
            0x3E, // START_BYTE_INBOUND
            (byte)(payloadLength & 0xFF),
            (byte)((payloadLength >> 8) & 0xFF),
            responseCode
        };

        if (payload != null && payload.Length > 0)
        {
            frame.AddRange(payload);
        }

        return ToHexString(frame.ToArray());
    }

    /// <summary>
    /// Truncates a hex string for display, adding ellipsis if needed
    /// </summary>
    /// <param name="hexString">The hex string to truncate</param>
    /// <param name="maxLength">Maximum length before truncation</param>
    /// <returns>Truncated string with ellipsis if needed</returns>
    public static string TruncateWithEllipsis(string hexString, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(hexString) || hexString.Length <= maxLength)
        {
            return hexString;
        }

        return hexString.Substring(0, maxLength) + "...";
    }
}
