// <copyright file="ChannelSecret.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents a 16-byte MeshCore channel encryption secret.
    /// The MeshCore protocol uses a 16-byte (128-bit) AES key for channel encryption.
    /// The secret is transmitted on the wire as part of the channel record:
    /// <c>[index(1)][name(32)][secret(16)]</c> = 49 bytes.
    /// </summary>
    public sealed class ChannelSecret : IEquatable<ChannelSecret>
    {
        /// <summary>
        /// The required length of a channel secret in bytes.
        /// </summary>
        public const int SecretLength = 16;

        /// <summary>
        /// The required length of a channel secret when represented as a hex string.
        /// </summary>
        public const int HexLength = SecretLength * 2;

        /// <summary>
        /// The well-known default public channel key used by all MeshCore devices at channel index 0.
        /// Hex: <c>8b3387e9c5cdea6ac9e5edbaa115cd72</c>, Base64: <c>izOH6cXN6mrJ5e26oRXNcg==</c>.
        /// </summary>
        public static readonly ChannelSecret DefaultPublicKey = FromHex("8b3387e9c5cdea6ac9e5edbaa115cd72");

        /// <summary>
        /// An empty (all-zero) secret, representing an unencrypted channel.
        /// </summary>
        public static readonly ChannelSecret Empty = new(new byte[SecretLength]);

        private readonly byte[] _bytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelSecret"/> class from a 16-byte array.
        /// </summary>
        /// <param name="bytes">A 16-byte secret key. The array is copied defensively.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> is not exactly 16 bytes.</exception>
        private ChannelSecret(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);

            if (bytes.Length != SecretLength)
            {
                throw new ArgumentException(
                    $"Channel secret must be exactly {SecretLength} bytes, got {bytes.Length} bytes.",
                    nameof(bytes));
            }

            _bytes = new byte[SecretLength];
            Array.Copy(bytes, _bytes, SecretLength);
        }

        /// <summary>
        /// Gets the raw 16-byte secret as a read-only span.
        /// </summary>
        [JsonIgnore]
        public ReadOnlySpan<byte> Bytes => _bytes;

        /// <summary>
        /// Gets the secret as a lowercase hex string (32 characters).
        /// </summary>
        [JsonPropertyName("hex")]
        public string Hex => Convert.ToHexString(_bytes).ToLowerInvariant();

        /// <summary>
        /// Gets the secret as a Base64-encoded string.
        /// </summary>
        [JsonPropertyName("base64")]
        public string Base64 => Convert.ToBase64String(_bytes);

        /// <summary>
        /// Gets a value indicating whether this secret is all zeros (unencrypted / empty).
        /// </summary>
        [JsonPropertyName("is_empty")]
        public bool IsEmpty => _bytes.All(b => b == 0);

        /// <summary>
        /// Creates a <see cref="ChannelSecret"/> from a 16-byte array.
        /// </summary>
        /// <param name="bytes">A 16-byte secret key.</param>
        /// <returns>A new <see cref="ChannelSecret"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="bytes"/> is not exactly 16 bytes.</exception>
        public static ChannelSecret FromBytes(byte[] bytes)
        {
            return new ChannelSecret(bytes);
        }

        /// <summary>
        /// Creates a <see cref="ChannelSecret"/> from a 32-character hex string.
        /// </summary>
        /// <param name="hex">A 32-character hex string representing 16 bytes.</param>
        /// <returns>A new <see cref="ChannelSecret"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="hex"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hex"/> is not a valid 32-character hex string.</exception>
        public static ChannelSecret FromHex(string hex)
        {
            ArgumentNullException.ThrowIfNull(hex);

            if (hex.Length != HexLength)
            {
                throw new ArgumentException(
                    $"Hex string must be exactly {HexLength} characters, got {hex.Length}.",
                    nameof(hex));
            }

            byte[] bytes;
            try
            {
                bytes = Convert.FromHexString(hex);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException(
                    "Hex string contains invalid characters.", nameof(hex), ex);
            }

            return new ChannelSecret(bytes);
        }

        /// <summary>
        /// Creates a <see cref="ChannelSecret"/> from a Base64-encoded string.
        /// </summary>
        /// <param name="base64">A Base64 string that decodes to exactly 16 bytes.</param>
        /// <returns>A new <see cref="ChannelSecret"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="base64"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="base64"/> does not decode to exactly 16 bytes.</exception>
        public static ChannelSecret FromBase64(string base64)
        {
            ArgumentNullException.ThrowIfNull(base64);

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException(
                    "Base64 string is not valid.", nameof(base64), ex);
            }

            if (bytes.Length != SecretLength)
            {
                throw new ArgumentException(
                    $"Base64 string must decode to exactly {SecretLength} bytes, got {bytes.Length} bytes.",
                    nameof(base64));
            }

            return new ChannelSecret(bytes);
        }

        /// <summary>
        /// Derives a <see cref="ChannelSecret"/> from a hashtag channel name using
        /// <c>SHA256(name)[0:16]</c>, matching the Python CLI behavior:
        /// <c>channel_secret = sha256(channel_name.encode("utf-8")).digest()[0:16]</c>.
        /// </summary>
        /// <param name="channelName">The channel name (typically starting with '#').</param>
        /// <returns>A new <see cref="ChannelSecret"/> derived from the channel name.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="channelName"/> is null or empty.</exception>
        public static ChannelSecret FromChannelName(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                throw new ArgumentException(
                    "Channel name cannot be null or empty.", nameof(channelName));
            }

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(channelName));
            return new ChannelSecret(hash[..SecretLength]);
        }

        /// <summary>
        /// Generates a cryptographically random <see cref="ChannelSecret"/> suitable for private channels.
        /// </summary>
        /// <returns>A new <see cref="ChannelSecret"/> with cryptographically random bytes.</returns>
        public static ChannelSecret CreateRandom()
        {
            var bytes = new byte[SecretLength];
            RandomNumberGenerator.Fill(bytes);
            return new ChannelSecret(bytes);
        }

        /// <summary>
        /// Returns a copy of the raw 16-byte secret as a new array.
        /// </summary>
        /// <returns>A new byte array containing the secret.</returns>
        public byte[] ToByteArray()
        {
            var copy = new byte[SecretLength];
            Array.Copy(_bytes, copy, SecretLength);
            return copy;
        }

        /// <summary>
        /// Determines whether the specified <see cref="ChannelSecret"/> is equal to the current instance.
        /// </summary>
        /// <param name="other">The <see cref="ChannelSecret"/> to compare with.</param>
        /// <returns><c>true</c> if the secrets contain the same bytes; otherwise, <c>false</c>.</returns>
        public bool Equals(ChannelSecret? other)
        {
            if (other is null)
            {
                return false;
            }

            return _bytes.AsSpan().SequenceEqual(other._bytes);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as ChannelSecret);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Use first 4 bytes for hash code (sufficient for hash table distribution)
            return BitConverter.ToInt32(_bytes, 0);
        }

        /// <summary>
        /// Returns a JSON representation of the channel secret.
        /// </summary>
        /// <returns>A JSON string describing the channel secret.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }

        /// <summary>
        /// Determines whether two <see cref="ChannelSecret"/> instances are equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns><c>true</c> if both instances contain the same bytes; otherwise, <c>false</c>.</returns>
        public static bool operator ==(ChannelSecret? left, ChannelSecret? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="ChannelSecret"/> instances are not equal.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns><c>true</c> if the instances contain different bytes; otherwise, <c>false</c>.</returns>
        public static bool operator !=(ChannelSecret? left, ChannelSecret? right)
        {
            return !(left == right);
        }
    }
}
