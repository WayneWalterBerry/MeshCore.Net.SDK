// <copyright file="ContactPublicKeyJsonConverter.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Json
{
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// JSON converter for ContactPublicKey to serialize as hex string
    /// </summary>
    public class ContactPublicKeyJsonConverter : JsonConverter<ContactPublicKey>
    {
        /// <summary>
        /// Reads ContactPublicKey from JSON (not implemented for this use case)
        /// </summary>
        /// <param name="reader">The JSON reader</param>
        /// <param name="typeToConvert">The type to convert to</param>
        /// <param name="options">Serializer options</param>
        /// <returns>ContactPublicKey instance</returns>
        /// <exception cref="NotImplementedException">Reading is not supported</exception>
        public override ContactPublicKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException("Deserialization not supported for ContactPublicKey");
        }

        /// <summary>
        /// Writes ContactPublicKey as hex string to JSON
        /// </summary>
        /// <param name="writer">The JSON writer</param>
        /// <param name="value">The ContactPublicKey to serialize</param>
        /// <param name="options">Serializer options</param>
        public override void Write(Utf8JsonWriter writer, ContactPublicKey value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
