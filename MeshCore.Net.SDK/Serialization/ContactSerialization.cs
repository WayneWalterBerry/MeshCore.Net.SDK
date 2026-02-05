// <copyright file="ContactSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

using System.Text;
using MeshCore.Net.SDK.Models;
using MeshCore.Net.SDK.Protocol;

namespace MeshCore.Net.SDK.Serialization
{
    internal class ContactSerialization : IBinaryDeserializer<Contact>
    {
        private static readonly Lazy<ContactSerialization> _instance = new(() => new ContactSerialization());

        /// <summary>
        /// Gets the singleton instance of the ContactSerialization
        /// </summary>
        public static ContactSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private ContactSerialization()
        {
        }

        public Contact? Deserialize(byte[] data)
        {
            if (!this.TryDeserialize(data, out var result))
            {
                throw new InvalidOperationException("Failed to deserialize contact from binary data");
            }

            return result;
        }

        /// <summary>
        /// Attempts to deserialize a contact from the specified byte array.
        /// </summary>
        /// <remarks>If the input data does not contain a valid contact payload, the method returns false
        /// and sets <paramref name="result"/> to <see langword="null"/>. The contact name is extracted from the payload
        /// if available; otherwise, a default name is assigned.</remarks>
        /// <param name="data">The byte array containing the serialized contact data. The array must contain at least 32 bytes for a valid
        /// contact public key.</param>
        /// <param name="result">When this method returns, contains the deserialized <see cref="Contact"/> object if the operation succeeds;
        /// otherwise, <see langword="null"/>.</param>
        /// <returns>true if the contact was successfully deserialized; otherwise, false.</returns>
        /// <remarks>
        /// Payload layout (from MyMesh::writeContactRespFrame):
        /// [0]      = RESP_CODE_CONTACT (optional – may be stripped before deserialization)
        /// [1..32]  = contact.id.pub_key (32 bytes public key)
        /// [33]     = contact.type (ADV_TYPE_* enum: chat, repeater, room, sensor, etc.)
        /// [34]     = contact.flags
        /// [35]     = contact.out_path_len
        /// [36..(36+MAX_PATH_SIZE-1)] = contact.out_path (MAX_PATH_SIZE bytes)
        /// [..]     = contact.name (null-terminated, max 32 bytes)
        /// [..]     = contact.last_advert_timestamp (4 bytes, uint32)
        /// [..]     = contact.gps_lat (4 bytes, int32)
        /// [..]     = contact.gps_lon (4 bytes, int32)
        /// [..]     = contact.lastmod (4 bytes, uint32)
        /// </remarks>
        public bool TryDeserialize(byte[] data, out Contact? result)
        {
            result = default;

            var payloadStart = 0;

            if (data.Length > 0 && data[0] == (byte)MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                payloadStart = 1;
            }

            if (data.Length <= payloadStart)
            {
                return false;
            }

            var contactData = new byte[data.Length - payloadStart];
            Array.Copy(data, payloadStart, contactData, 0, contactData.Length);

            if (contactData.Length < 36)
            {
                // Need at least pubkey (32) + type (1) + flags (1) + out_path_len (1)
                return false;
            }

            // Public key -> NodeId
            var publicKey = new byte[32];
            Array.Copy(contactData, 0, publicKey, 0, 32);

            // Type byte
            var rawType = contactData[32];
            var nodeType = MapContactType(rawType);

            // Flags byte
            var flags = contactData[33];

            // out_path_len (logical length / sentinel)
            var outPathLen = contactData[34];

            // Firmware always writes a fixed MAX_PATH_SIZE out_path block regardless of outPathLen.
            // This must match firmware MAX_PATH_SIZE in MyMesh/ContactInfo.
            const int MAX_PATH_SIZE = 64;

            // Ensure buffer is large enough for the fixed-size out_path block.
            var requiredMinLength = 35 + MAX_PATH_SIZE;
            if (contactData.Length < requiredMinLength)
            {
                // Payload does not contain the full fixed-size out_path block.
                return false;
            }

            // Interpret outPathLen:
            // - 0xFF means "no valid outbound path"
            // - otherwise clamp to MAX_PATH_SIZE for safety.
            var effectiveOutPathLen = outPathLen == 0xFF
                ? 0
                : Math.Min(outPathLen, (byte)MAX_PATH_SIZE);

            // Extract only the meaningful part of the fixed-size out_path block for higher-level parsing.
            byte[] outPathBytes;
            if (effectiveOutPathLen > 0)
            {
                outPathBytes = new byte[effectiveOutPathLen];
                Array.Copy(contactData, 35, outPathBytes, 0, effectiveOutPathLen);
            }
            else
            {
                outPathBytes = Array.Empty<byte>();
            }

            string contactName = "Unknown Contact";

            // In firmware, name is stored after the fixed-size out_path block.
            var nameSearchStart = 35 + MAX_PATH_SIZE;

            // Guard: keep at least 8 bytes for trailing fields (timestamps, gps, etc.)
            if (nameSearchStart < 32)
            {
                nameSearchStart = 32;
            }

            for (var startOffset = nameSearchStart; startOffset < contactData.Length - 8; startOffset++)
            {
                if (contactData[startOffset] >= 32 && contactData[startOffset] <= 126)
                {
                    var nameBytes = new List<byte>();

                    for (var i = startOffset; i < Math.Min(contactData.Length, startOffset + 64); i++)
                    {
                        if (contactData[i] == 0)
                        {
                            break;
                        }

                        if (contactData[i] >= 32 && contactData[i] <= 126)
                        {
                            nameBytes.Add(contactData[i]);
                        }
                        else if (contactData[i] >= 0x80)
                        {
                            nameBytes.Add(contactData[i]);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (nameBytes.Count >= 3)
                    {
                        try
                        {
                            var candidateName = Encoding.UTF8.GetString(nameBytes.ToArray()).Trim();
                            if (!string.IsNullOrWhiteSpace(candidateName) && candidateName.Length >= 3)
                            {
                                var uniqueChars = candidateName.Distinct().Count();
                                if (uniqueChars >= 2)
                                {
                                    contactName = candidateName;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore decoding issues and continue scanning
                        }
                    }
                }
            }

            OutboundRoute? outboundRoute;
            OutboundRouteSerialization.Instance.TryDeserialize(outPathBytes, out outboundRoute);

            result = new Contact
            {
                Name = contactName,
                PublicKey = new ContactPublicKey(publicKey),
                NodeType = nodeType,
                ContactFlags = (ContactFlags)flags,
                OutboundRoute = outboundRoute
            };

            return true;
        }

        /// <summary>
        /// Maps the raw contact.type byte from firmware to the SDK NodeType enum.
        /// </summary>
        private static NodeType MapContactType(byte rawType)
        {
            // These values should match ADV_TYPE_* in MyMesh.cpp.
            // Adjust them if your NodeType enum uses different underlying values.
            return rawType switch
            {
                0x00 => NodeType.Unknown,   // if you have an Unknown value
                0x01 => NodeType.Chat,      // ADV_TYPE_CHAT
                0x02 => NodeType.Repeater,  // ADV_TYPE_REPEATER
                0x03 => NodeType.RoomServer,      // ADV_TYPE_ROOM
                0x04 => NodeType.Sensor,    // ADV_TYPE_SENSOR
                _ => NodeType.Unknown
            };
        }
    }
}
