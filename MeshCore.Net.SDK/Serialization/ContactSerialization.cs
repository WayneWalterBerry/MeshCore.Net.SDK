// <copyright file="ContactSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;
    using MeshCore.Net.SDK.Protocol;

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
        /// <remarks>
        /// This implementation is intentionally tolerant:
        /// it accepts frames that are missing trailing optional fields (path, GPS, lastmod)
        /// and falls back to default values rather than rejecting the entire record.
        /// </remarks>
        /// <param name="data">The byte array containing the serialized contact data.</param>
        /// <param name="result">The resulting contact when deserialization succeeds; otherwise, null.</param>
        /// <returns>true if the contact was successfully deserialized; otherwise, false.</returns>
        /// <remarks>
        /// Payload layout (from MyMesh::writeContactRespFrame):
        /// [0]      = RESP_CODE_CONTACT (optional – may be stripped before deserialization)
        /// [1..32]  = contact.id.pub_key (32 bytes public key)
        /// [33]     = contact.type (ADV_TYPE_* enum: chat, repeater, room, sensor, etc.)
        /// [34]     = contact.flags
        /// [35]     = contact.out_path_len
        /// [36..(36+MAX_PATH_SIZE-1)] = contact.out_path (MAX_PATH_SIZE bytes, fixed-size block)
        /// [..]     = contact.name (null-terminated, max 32 bytes)
        /// [..]     = contact.last_advert_timestamp (4 bytes, uint32, optional)
        /// [..]     = contact.gps_lat (4 bytes, int32, optional)
        /// [..]     = contact.gps_lon (4 bytes, int32, optional)
        /// [..]     = contact.lastmod (4 bytes, uint32, optional)
        /// </remarks>
        public bool TryDeserialize(byte[] data, out Contact? result)
        {
            result = default;

            if (data is null || data.Length == 0)
            {
                return false;
            }

            var payloadStart = 0;

            // Allow caller to pass either raw contact payload or full frame
            if (data[0] == (byte)MeshCoreResponseCode.RESP_CODE_CONTACT)
            {
                payloadStart = 1;
                if (data.Length <= payloadStart)
                {
                    return false;
                }
            }

            var contactDataLength = data.Length - payloadStart;
            if (contactDataLength <= 0)
            {
                return false;
            }

            var contactData = new byte[contactDataLength];
            Array.Copy(data, payloadStart, contactData, 0, contactDataLength);

            // Minimum required layout: pubkey (32) + type (1) + flags (1) + out_path_len (1)
            if (contactData.Length < 32 + 1 + 1 + 1)
            {
                return false;
            }

            var offset = 0;

            // Public key (32 bytes)
            var publicKeyBytes = new byte[32];
            Array.Copy(contactData, offset, publicKeyBytes, 0, 32);
            offset += 32;

            // Type byte
            var rawType = contactData[offset++];
            var nodeType = MapContactType(rawType);

            // Flags byte
            var flags = contactData[offset++];

            // out_path_len (logical length / sentinel)
            var outPathLenRaw = contactData[offset++];

            // Firmware uses a fixed MAX_PATH_SIZE for the path block.
            const int MAX_PATH_SIZE = 64;

            // If there is enough data, consume the fixed-size out_path block.
            byte[] outPathBytes = Array.Empty<byte>();
            var remaining = contactData.Length - offset;
            if (remaining >= MAX_PATH_SIZE)
            {
                // Interpret outPathLen:
                //  - 0xFF means "no valid outbound path"
                //  - otherwise clamp to MAX_PATH_SIZE for safety.
                var effectiveOutPathLen = outPathLenRaw == 0xFF
                    ? 0
                    : Math.Min(outPathLenRaw, (byte)MAX_PATH_SIZE);

                if (effectiveOutPathLen > 0)
                {
                    outPathBytes = new byte[effectiveOutPathLen];
                    Array.Copy(contactData, offset, outPathBytes, 0, effectiveOutPathLen);
                }

                offset += MAX_PATH_SIZE;
            }
            else
            {
                // Older or truncated payloads may omit the fixed-sized path block entirely.
                // In that case, treat as "no outbound path".
                outPathBytes = Array.Empty<byte>();
                offset += remaining; // move to end for safety
            }

            // Name block is a fixed 32 bytes in current firmware, but treat it as optional.
            string contactName = "Unknown Contact";
            remaining = contactData.Length - offset;
            if (remaining > 0)
            {
                var nameBlockLength = Math.Min(32, remaining);
                var nameBytes = new byte[32];
                Array.Clear(nameBytes, 0, nameBytes.Length);
                Array.Copy(contactData, offset, nameBytes, 0, nameBlockLength);

                var terminatorIndex = Array.IndexOf(nameBytes, (byte)0);
                if (terminatorIndex < 0)
                {
                    terminatorIndex = nameBlockLength;
                }

                if (terminatorIndex > 0)
                {
                    try
                    {
                        var rawName = Encoding.UTF8.GetString(nameBytes, 0, terminatorIndex).Trim();
                        if (!string.IsNullOrWhiteSpace(rawName))
                        {
                            contactName = rawName;
                        }
                    }
                    catch
                    {
                        // If name decoding fails, keep default "Unknown Contact"
                    }
                }

                // Advance offset by the full fixed-size name block if present,
                // otherwise just consume what we actually had.
                offset += nameBlockLength;
                if (nameBlockLength == 32 && contactData.Length >= offset)
                {
                    // For full 32-byte blocks, we know there might be trailing fields.
                    offset = 32 + (offset - nameBlockLength);
                }
            }

            // Optional trailing fields.
            // We only read a field when there are enough bytes remaining; otherwise we fall back to defaults.
            const double CoordScale = 1_000_000.0;

            var lastAdvertTimestampSeconds = 0u;
            var gpsLatRaw = 0;
            var gpsLonRaw = 0;
            var lastModifiedTimestampSeconds = 0u;

            remaining = contactData.Length - offset;

            if (remaining >= 4)
            {
                lastAdvertTimestampSeconds = BitConverter.ToUInt32(contactData, offset);
                offset += 4;
                remaining -= 4;
            }

            if (remaining >= 4)
            {
                gpsLatRaw = BitConverter.ToInt32(contactData, offset);
                offset += 4;
                remaining -= 4;
            }

            if (remaining >= 4)
            {
                gpsLonRaw = BitConverter.ToInt32(contactData, offset);
                offset += 4;
                remaining -= 4;
            }

            if (remaining >= 4)
            {
                lastModifiedTimestampSeconds = BitConverter.ToUInt32(contactData, offset);
            }

            OutboundRoute? outboundRoute = null;
            if (outPathBytes.Length > 0)
            {
                OutboundRouteSerialization.Instance.TryDeserialize(outPathBytes, out outboundRoute);
            }

            var latitude = gpsLatRaw / CoordScale;
            var longitude = gpsLonRaw / CoordScale;

            // If timestamps are zero (missing or unset), keep DateTime.MinValue to indicate "unknown".
            DateTime lastAdvertUtc = DateTime.MinValue;
            DateTime lastModifiedUtc = DateTime.MinValue;

            if (lastAdvertTimestampSeconds != 0)
            {
                lastAdvertUtc = DateTimeOffset.FromUnixTimeSeconds(lastAdvertTimestampSeconds).UtcDateTime;
            }

            if (lastModifiedTimestampSeconds != 0)
            {
                lastModifiedUtc = DateTimeOffset.FromUnixTimeSeconds(lastModifiedTimestampSeconds).UtcDateTime;
            }

            result = new Contact
            {
                Name = contactName,
                PublicKey = new ContactPublicKey(publicKeyBytes),
                NodeType = nodeType,
                ContactFlags = (ContactFlags)flags,
                OutboundRoute = outboundRoute,
                Latitude = latitude,
                Longitude = longitude,
                LastAdvert = lastAdvertUtc,
                LastModified = lastModifiedUtc
            };

            return true;
        }

        /// <summary>
        /// Maps the raw <c>contact.type</c> byte from firmware to the SDK <see cref="NodeType"/> enum.
        /// </summary>
        /// <param name="rawType">The raw type value from the contact payload.</param>
        /// <returns>The corresponding <see cref="NodeType"/> value.</returns>
        private static NodeType MapContactType(byte rawType)
        {
            // These values should match ADV_TYPE_* in MyMesh.cpp.
            return rawType switch
            {
                0x00 => NodeType.Unknown,
                0x01 => NodeType.Chat,
                0x02 => NodeType.Repeater,
                0x03 => NodeType.RoomServer,
                0x04 => NodeType.Sensor,
                _ => NodeType.Unknown
            };
        }
    }
}