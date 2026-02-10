// <copyright file="ByteArrayHexConverter.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Json
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// JSON converter for byte arrays to serialize as lowercase hexadecimal strings
    /// </summary>
    public class ByteArrayHexConverter : JsonConverter<byte[]>
    {
        /// <summary>
        /// Reads a byte array from a hexadecimal JSON string
        /// </summary>
        /// <param name="reader">The JSON reader</param>
        /// <param name="typeToConvert">The type to convert to</param>
        /// <param name="options">Serializer options</param>
        /// <returns>Byte array parsed from hex string</returns>
        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var hexString = reader.GetString();
            if (string.IsNullOrEmpty(hexString))
            {
                return Array.Empty<byte>();
            }

            // Remove any whitespace or separators
            hexString = hexString.Replace(" ", string.Empty).Replace("-", string.Empty);

            // Ensure even number of characters
            if (hexString.Length % 2 != 0)
            {
                throw new JsonException($"Invalid hex string length: {hexString.Length}");
            }

            var bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return bytes;
        }

        /// <summary>
        /// Writes a byte array as a lowercase hexadecimal string to JSON
        /// </summary>
        /// <param name="writer">The JSON writer</param>
        /// <param name="value">The byte array to serialize</param>
        /// <param name="options">Serializer options</param>
        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            if (value == null || value.Length == 0)
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            // Convert to lowercase hex string without separators
            var hexString = Convert.ToHexString(value).ToLowerInvariant();
            writer.WriteStringValue(hexString);
        }
    }
}