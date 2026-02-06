// <copyright file="UnixTimestampJsonConverter.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Json
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// JSON converter for DateTime to Unix timestamp
    /// </summary>
    public class UnixTimestampJsonConverter : JsonConverter<DateTime>
    {
        /// <summary>
        /// Reads DateTime from Unix timestamp (not implemented for this use case)
        /// </summary>
        /// <param name="reader">The JSON reader</param>
        /// <param name="typeToConvert">The type to convert to</param>
        /// <param name="options">Serializer options</param>
        /// <returns>DateTime instance</returns>
        /// <exception cref="NotImplementedException">Reading is not supported</exception>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization not supported for Unix timestamps");
        }

        /// <summary>
        /// Writes DateTime as Unix timestamp to JSON
        /// </summary>
        /// <param name="writer">The JSON writer</param>
        /// <param name="value">The DateTime to serialize</param>
        /// <param name="options">Serializer options</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var unixTimestamp = new DateTimeOffset(value).ToUnixTimeSeconds();
            writer.WriteNumberValue(unixTimestamp);
        }
    }
}
