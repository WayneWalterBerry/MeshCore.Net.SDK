// <copyright file="ContactMessageParamsSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System;
    using System.Text;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles serialization of <see cref="ContactMessageParams"/> into the binary payload
    /// expected by CMD_SEND_TXT_MSG (0x02) with txt_type=0x00 (plain text).
    /// </summary>
    /// <remarks>
    /// Wire format (variable length):
    /// <code>
    /// Offset  Type     Field
    /// 0       uint8    txt_type  (0x00 = plain text)
    /// 1       uint8    attempt   (retry counter, 0x00 for first attempt)
    /// 2-5     uint32   timestamp (Unix epoch seconds, little-endian)
    /// 6-11    bytes    pubkey_prefix (first 6 bytes of contact's 32-byte public key)
    /// 12+     bytes    message content (UTF-8 encoded, no null terminator)
    /// </code>
    /// Matches the Python reference <c>send_msg</c> in <c>messaging.py</c>:
    /// <c>b"\x02\x00" + attempt.to_bytes(1, "little") + timestamp.to_bytes(4, "little")
    /// + dst_bytes + msg.encode("utf-8")</c>
    /// </remarks>
    internal sealed class ContactMessageParamsSerialization : IBinarySerializer<ContactMessageParams>
    {
        /// <summary>
        /// The txt_type byte value for plain text messages.
        /// </summary>
        private const byte TXT_TYPE_PLAIN = 0x00;

        private static readonly Lazy<ContactMessageParamsSerialization> _instance = new(() => new ContactMessageParamsSerialization());

        /// <summary>
        /// Gets the singleton instance of the <see cref="ContactMessageParamsSerialization"/> class.
        /// </summary>
        public static ContactMessageParamsSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton.
        /// </summary>
        private ContactMessageParamsSerialization()
        {
        }

        /// <summary>
        /// Serializes a <see cref="ContactMessageParams"/> object into the binary payload
        /// for CMD_SEND_TXT_MSG with txt_type=0x00.
        /// </summary>
        /// <param name="obj">The contact message parameters to serialize.</param>
        /// <returns>A byte array containing the serialized message payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="obj"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the parameters have an invalid target or empty content.</exception>
        public byte[] Serialize(ContactMessageParams obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (!obj.HasValidTarget)
            {
                throw new InvalidOperationException("Cannot serialize message without a valid target public key.");
            }

            if (obj.IsEmpty)
            {
                throw new InvalidOperationException("Cannot serialize message with empty content.");
            }

            var messageBytes = Encoding.UTF8.GetBytes(obj.Content);

            var payload = new byte[obj.EstimatedPayloadSize];
            var offset = 0;

            // 1. txt_type = 0x00 (plain text)
            payload[offset++] = TXT_TYPE_PLAIN;

            // 2. attempt counter
            payload[offset++] = obj.Attempt;

            // 3. timestamp (4 bytes, little-endian uint32)
            BitConverter.GetBytes(obj.Timestamp).CopyTo(payload, offset);
            offset += 4;

            // 4. pubkey_prefix (first 6 bytes of the 32-byte public key)
            Buffer.BlockCopy(obj.TargetPublicKey.Value, 0, payload, offset, ContactMessageParams.PubKeyPrefixLength);
            offset += ContactMessageParams.PubKeyPrefixLength;

            // 5. message content (UTF-8, no null terminator)
            Buffer.BlockCopy(messageBytes, 0, payload, offset, messageBytes.Length);

            return payload;
        }
    }
}
