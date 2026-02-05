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
        /// Gets the singleton instance of the ChannelDeserializer
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

            string contactName = "Unknown Contact";
            string nodeId = "UNKNOWN";

            if (contactData.Length >= 32)
            {
                var publicKey = new byte[32];
                Array.Copy(contactData, 0, publicKey, 0, 32);
                nodeId = Convert.ToHexString(publicKey).ToLowerInvariant();
            }

            // Simple name extraction
            for (int startOffset = 32; startOffset < contactData.Length - 8; startOffset++)
            {
                if (contactData[startOffset] >= 32 && contactData[startOffset] <= 126)
                {
                    var nameBytes = new List<byte>();

                    for (int i = startOffset; i < Math.Min(contactData.Length, startOffset + 64); i++)
                    {
                        if (contactData[i] == 0) break;
                        else if (contactData[i] >= 32 && contactData[i] <= 126) nameBytes.Add(contactData[i]);
                        else if (contactData[i] >= 0x80) nameBytes.Add(contactData[i]);
                        else break;
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
                        catch { }
                    }
                }
            }

            result = new Contact
            {
                // Only set properties that are actually in the radio payload
                Id = nodeId,
                Name = contactName,
                NodeId = nodeId
                // Do NOT set: LastSeen, IsOnline, Status - these aren't in the basic contact payload
            };

            return true;
        }
    }
}
