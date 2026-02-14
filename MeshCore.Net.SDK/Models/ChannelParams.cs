// <copyright file="ChannelParams.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the parameters used to define a communication channel, including its index, name, and optional
    /// encryption settings.
    /// </summary>
    public class ChannelParams
    {
        /// <summary>
        /// The maximum valid channel index (0-based). MeshCore devices support up to 40 channels (indices 0 through 39).
        /// </summary>
        public const uint MaxIndex = 39;

        /// <summary>
        /// Gets or sets the unique channel index
        /// </summary>
        [JsonPropertyName("index")]
        public uint Index { get; set; }

        /// <summary>
        /// Gets or sets the channel name (up to 31 characters as per MeshCore spec)
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the channel encryption secret (16-byte key).
        /// Null when the channel is a hashtag channel whose key is derived from its name.
        /// </summary>
        [JsonIgnore]
        public ChannelSecret? EncryptionKey { get; private set; }

        /// <summary>
        /// Creates a new <see cref="ChannelParams"/> instance with the specified name and encryption key.
        /// </summary>
        /// <param name="index">The channel index (0 through <see cref="MaxIndex"/>).</param>
        /// <param name="name">The channel name (up to 31 characters).</param>
        /// <param name="encryptionKey">The 16-byte channel encryption secret.</param>
        /// <returns>A new <see cref="ChannelParams"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="encryptionKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index exceeds <see cref="MaxIndex"/>.</exception>
        public static ChannelParams Create(uint index, string name, ChannelSecret encryptionKey)
        {
            if (index > MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Channel index must be between 0 and {MaxIndex}");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            }

            if (name.Length > 31)
            {
                throw new ArgumentException("Channel name cannot exceed 31 characters", nameof(name));
            }

            ArgumentNullException.ThrowIfNull(encryptionKey);

            return new ChannelParams
            {
                Index = index,
                Name = name,
                EncryptionKey = encryptionKey
            };
        }

        /// <summary>
        /// Creates a new public (unencrypted) <see cref="ChannelParams"/> instance with the specified name.
        /// </summary>
        /// <param name="index">The channel index (0 through <see cref="MaxIndex"/>).</param>
        /// <param name="name">The channel name (up to 31 characters, must start with '#').</param>
        /// <returns>A new <see cref="ChannelParams"/> instance with no encryption key.</returns>
        /// <exception cref="ArgumentException">Thrown when the name is null, empty, exceeds 31 characters, or does not start with '#'.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the index exceeds <see cref="MaxIndex"/>.</exception>
        public static ChannelParams Create(uint index, string name)
        {
            if (index > MaxIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Channel index must be between 0 and {MaxIndex}");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Channel name cannot be null or empty", nameof(name));
            }

            if (!name.StartsWith('#'))
            {
                throw new ArgumentException("Public channel name must start with '#'", nameof(name));
            }

            if (name.Length > 31)
            {
                throw new ArgumentException("Channel name cannot exceed 31 characters", nameof(name));
            }

            return new ChannelParams
            {
                Index = index,
                Name = name,
                EncryptionKey = ChannelSecret.FromChannelName(name)
            };
        }

        /// <summary>
        /// Returns a JSON representation of the channel parameters.
        /// </summary>
        /// <returns>A JSON string describing the channel parameters.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }
}
