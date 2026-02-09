// <copyright file="CommandSerialization.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Serialization
{
    using System.Text;
    using MeshCore.Net.SDK.Models;

    /// <summary>
    /// Handles serialization of Command objects into MeshCore CMD_SEND_TXT_MSG payload format.
    /// Commands are sent with txt_type=0x01 for remote CLI operations on repeater/room server nodes.
    /// 
    /// Correct payload format per MeshCore protocol:
    /// [txt_type(1)][attempt(1)][timestamp(4)][contact_key_prefix(6)][command_text][null(1)]
    /// </summary>
    internal class CommandSerialization : IBinarySerializer<Command>
    {
        private static readonly Lazy<CommandSerialization> _instance = new(() => new CommandSerialization());

        /// <summary>
        /// Gets the singleton instance of the CommandSerialization
        /// </summary>
        public static CommandSerialization Instance => _instance.Value;

        /// <summary>
        /// Prevents external instantiation of the singleton
        /// </summary>
        private CommandSerialization()
        {
        }

        /// <summary>
        /// Serializes a Command object into the MeshCore CMD_SEND_TXT_MSG payload format.
        /// </summary>
        /// <param name="command">The command object to serialize</param>
        /// <returns>Byte array containing the serialized command payload</returns>
        /// <exception cref="ArgumentNullException">Thrown when command is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when command has invalid target or empty command text</exception>
        /// <remarks>
        /// Payload structure per MeshCore protocol documentation:
        /// [txt_type(1)] - 0x01 for CLI commands, 0x00 for regular messages
        /// [attempt(1)] - Retry counter (0-3), use 0x00 for first attempt
        /// [timestamp(4)] - Unix timestamp, little-endian uint32
        /// [contact_key_prefix(6)] - First 6 bytes of target's 32-byte public key
        /// [command_text] - UTF-8 encoded command string
        /// [null(1)] - 0x00 terminator for command string
        /// 
        /// This fixes the previous incorrect format which had:
        /// - Wrong field order (key first instead of txt_type first)
        /// - Full 32-byte key instead of 6-byte prefix
        /// - Missing attempt byte
        /// </remarks>
        public byte[] Serialize(Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (!command.HasValidTarget)
            {
                throw new InvalidOperationException("Cannot serialize command without a valid target public key.");
            }

            if (command.IsEmpty)
            {
                throw new InvalidOperationException("Cannot serialize command with empty command text.");
            }

            // Encode command text to UTF-8
            var commandBytes = Encoding.UTF8.GetBytes(command.CommandText);

            // Calculate payload size: txt_type(1) + attempt(1) + timestamp(4) + key_prefix(6) + text + null(1)
            var payloadSize = 1 + 1 + 4 + 6 + commandBytes.Length + 1;
            var payload = new List<byte>(payloadSize);

            // 1. Add txt_type = 0x01 (command type for CLI commands)
            payload.Add(0x01);

            // 2. Add attempt byte = 0x00 (first attempt, no retries yet)
            payload.Add(0x00);

            // 3. Add timestamp (4 bytes, little-endian uint32)
            var timestampBytes = BitConverter.GetBytes(command.Timestamp);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(timestampBytes);
            }
            payload.AddRange(timestampBytes);

            // 4. Add contact key prefix (first 6 bytes of the 32-byte public key)
            //    MeshCore uses 6-byte prefixes to identify contacts in messaging
            var contactKeyPrefix = new byte[6];
            Buffer.BlockCopy(command.TargetPublicKey.Value, 0, contactKeyPrefix, 0, 6);
            payload.AddRange(contactKeyPrefix);

            // 5. Add command text (UTF-8 encoded)
            payload.AddRange(commandBytes);

            // 6. Add null terminator for command string
            payload.Add(0x00);

            return payload.ToArray();
        }
    }
}