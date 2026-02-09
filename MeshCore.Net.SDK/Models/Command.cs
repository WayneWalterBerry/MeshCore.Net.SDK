// <copyright file="Command.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a command message to be sent to a remote repeater or room server node.
    /// Commands are sent using CMD_SEND_TXT_MSG with txt_type=0x01 for remote CLI operations.
    /// </summary>
    public sealed class Command
    {
        /// <summary>
        /// Gets or sets the target contact's 32-byte public key.
        /// This identifies the remote node that will execute the command.
        /// </summary>
        [JsonPropertyName("target_public_key")]
        public ContactPublicKey TargetPublicKey { get; set; }

        /// <summary>
        /// Gets or sets the command text to execute on the remote node.
        /// Examples: "neighbors", "status", "get telemetry", etc.
        /// </summary>
        [JsonPropertyName("command_text")]
        public string CommandText { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the command was created (Unix epoch seconds).
        /// Defaults to current UTC time when created.
        /// </summary>
        [JsonPropertyName("timestamp")]
        public uint Timestamp { get; set; } = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// Gets a value indicating whether the command has a valid target public key.
        /// </summary>
        [JsonIgnore]
        public bool HasValidTarget => TargetPublicKey != default(ContactPublicKey);

        /// <summary>
        /// Gets a value indicating whether the command text is empty.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(CommandText);

        /// <summary>
        /// Gets the command text length in bytes when UTF-8 encoded.
        /// </summary>
        [JsonIgnore]
        public int CommandLength => string.IsNullOrEmpty(CommandText) ? 0 : Encoding.UTF8.GetByteCount(CommandText);

        /// <summary>
        /// Gets the estimated total payload size in bytes for this command.
        /// Format per MeshCore protocol: [txt_type(1)][attempt(1)][timestamp(4)][contact_key_prefix(6)][command_text][null(1)]
        /// </summary>
        [JsonIgnore]
        public int EstimatedPayloadSize => 1 + 1 + 4 + 6 + CommandLength + 1;

        /// <summary>
        /// Returns a JSON representation of the command.
        /// </summary>
        /// <returns>A JSON string describing the command.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Returns a human-readable description of the command.
        /// </summary>
        /// <returns>A formatted string describing the command.</returns>
        public string GetDescription()
        {
            var targetPrefix = HasValidTarget
                ? TargetPublicKey.ToString().Substring(0, Math.Min(8, TargetPublicKey.ToString().Length))
                : "None";

            var timestampDate = DateTimeOffset.FromUnixTimeSeconds(Timestamp).DateTime;

            return $"Command: '{CommandText}' -> [{targetPrefix}] at {timestampDate:yyyy-MM-dd HH:mm:ss}";
        }

        /// <summary>
        /// Creates a new command for the specified target contact.
        /// </summary>
        /// <param name="targetPublicKey">The 32-byte public key of the target node.</param>
        /// <param name="commandText">The command text to execute on the remote node.</param>
        /// <returns>A new Command instance ready for serialization.</returns>
        public static Command Create(ContactPublicKey targetPublicKey, string commandText)
        {
            return new Command
            {
                TargetPublicKey = targetPublicKey,
                CommandText = commandText,
                Timestamp = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
        }

        /// <summary>
        /// Creates a new command with a custom timestamp for the specified target contact.
        /// </summary>
        /// <param name="targetPublicKey">The 32-byte public key of the target node.</param>
        /// <param name="commandText">The command text to execute on the remote node.</param>
        /// <param name="timestamp">The Unix epoch timestamp for the command.</param>
        /// <returns>A new Command instance ready for serialization.</returns>
        public static Command CreateWithTimestamp(ContactPublicKey targetPublicKey, string commandText, uint timestamp)
        {
            return new Command
            {
                TargetPublicKey = targetPublicKey,
                CommandText = commandText,
                Timestamp = timestamp
            };
        }
    }
}