// <copyright file="ContactMessageParams.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the parameters for a plain-text message sent to a contact via
    /// CMD_SEND_TXT_MSG with txt_type=0x00.
    /// </summary>
    /// <remarks>
    /// Wire format (variable length):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    txt_type (0x00 = plain text)
    /// 1       uint8    attempt  (retry counter, 0x00 for first attempt)
    /// 2-5     uint32   timestamp (Unix epoch seconds, little-endian)
    /// 6-11    bytes    pubkey_prefix (first 6 bytes of contact's 32-byte public key)
    /// 12+     bytes    message content (UTF-8 encoded, no null terminator)
    /// </code>
    /// Matches the Python reference <c>send_msg</c> in <c>messaging.py</c>.
    /// </remarks>
    public sealed class ContactMessageParams
    {
        /// <summary>
        /// The number of bytes from the contact's public key used in the wire payload.
        /// </summary>
        public const int PubKeyPrefixLength = 6;

        /// <summary>
        /// Gets or sets the target contact's 32-byte public key.
        /// Only the first <see cref="PubKeyPrefixLength"/> bytes are placed on the wire.
        /// </summary>
        [JsonPropertyName("target_public_key")]
        public ContactPublicKey TargetPublicKey { get; set; }

        /// <summary>
        /// Gets or sets the message content to send.
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the UTC timestamp when the message was created.
        /// Defaults to the current UTC time. Serialized as a Unix epoch uint32 on the wire.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the retry attempt counter (0 for first attempt, up to 3).
        /// </summary>
        [JsonPropertyName("attempt")]
        public uint Attempt { get; set; }

        /// <summary>
        /// Gets a value indicating whether the message has a valid target public key.
        /// </summary>
        [JsonIgnore]
        public bool HasValidTarget => TargetPublicKey != default(ContactPublicKey);

        /// <summary>
        /// Gets a value indicating whether the message content is empty.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrEmpty(Content);

        /// <summary>
        /// Gets the message content length in bytes when UTF-8 encoded.
        /// </summary>
        [JsonIgnore]
        public int ContentLength => string.IsNullOrEmpty(Content) ? 0 : Encoding.UTF8.GetByteCount(Content);

        /// <summary>
        /// Gets the estimated total payload size in bytes.
        /// Format: [txt_type(1)][attempt(1)][timestamp(4)][pubkey_prefix(6)][message]
        /// </summary>
        [JsonIgnore]
        public int EstimatedPayloadSize => 1 + 1 + 4 + PubKeyPrefixLength + ContentLength;

        /// <summary>
        /// Creates a new <see cref="ContactMessageParams"/> for the specified contact and message content.
        /// </summary>
        /// <param name="targetPublicKey">The 32-byte public key of the target contact.</param>
        /// <param name="content">The message text to send.</param>
        /// <returns>A new <see cref="ContactMessageParams"/> instance ready for serialization.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or empty.</exception>
        public static ContactMessageParams Create(ContactPublicKey targetPublicKey, string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }

            return new ContactMessageParams
            {
                TargetPublicKey = targetPublicKey,
                Content = content,
                Timestamp = DateTime.UtcNow,
                Attempt = 0
            };
        }

        /// <summary>
        /// Creates a new <see cref="ContactMessageParams"/> with a custom timestamp and attempt counter.
        /// </summary>
        /// <param name="targetPublicKey">The 32-byte public key of the target contact.</param>
        /// <param name="content">The message text to send.</param>
        /// <param name="timestamp">The UTC timestamp for the message. Converted to Unix epoch on the wire.</param>
        /// <param name="attempt">The retry attempt counter (0-3).</param>
        /// <returns>A new <see cref="ContactMessageParams"/> instance ready for serialization.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="content"/> is null or empty.</exception>
        public static ContactMessageParams Create(
            ContactPublicKey targetPublicKey,
            string content, 
            DateTime timestamp,
            uint attempt = 0)
        {
            if (string.IsNullOrEmpty(content))
            {
                throw new ArgumentException("Message content cannot be null or empty", nameof(content));
            }

            return new ContactMessageParams
            {
                TargetPublicKey = targetPublicKey,
                Content = content,
                Timestamp = timestamp,
                Attempt = attempt
            };
        }

        /// <summary>
        /// Returns a JSON representation of the contact message parameters.
        /// </summary>
        /// <returns>A JSON string describing the contact message parameters.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
