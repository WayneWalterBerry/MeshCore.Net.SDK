// <copyright file="Contact.cs" company="Wayne Walter Berry">
// Copyright (c) Wayne Walter Berry. All rights reserved.
// </copyright>

namespace MeshCore.Net.SDK.Models
{
    /// <summary>
    /// Represents a 32-byte private key associated with a contact.
    /// </summary>
    /// <remarks>This struct is immutable and provides value-based equality. The public key is stored as a
    /// byte array and is expected to be exactly 32 bytes in length, suitable for cryptographic operations that require
    /// a fixed-size key.</remarks>
    public readonly record struct ContactPublicKey
    {
        /// <summary>
        /// Gets the underlying byte array value represented by this instance.
        /// </summary>
        public byte[] Value { get; }

        /// <summary>
        /// Initializes a new instance of the ContactPublicKey class using the specified 32-byte public key value.
        /// </summary>
        /// <param name="value">A byte array containing the public key. Must be exactly 32 bytes in length.</param>
        /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
        /// <exception cref="ArgumentException">Thrown if value is not exactly 32 bytes in length.</exception>
        public ContactPublicKey(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length != 32)
            {
                throw new ArgumentException("Private key must be exactly 32 bytes.", nameof(value));
            }

            Value = (byte[])value.Clone();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            try
            {
                return Convert.ToHexString(Value);
            }
            catch (FormatException)
            {
                // Handle corrupted or invalid byte data gracefully
                // Return a safe representation that won't break string formatting
                return Convert.ToHexString(new byte[32]); // Return all zeros as fallback
            }
            catch (ArgumentException)
            {
                // Handle null or invalid Value array
                return Convert.ToHexString(new byte[32]); // Return all zeros as fallback
            }
        }

        /// <summary>
        /// Determines whether the specified ContactPublicKey is equal to the current instance by comparing the byte array values.
        /// </summary>
        /// <param name="other">The ContactPublicKey to compare with the current instance.</param>
        /// <returns>true if the byte arrays are equal; otherwise, false.</returns>
        public bool Equals(ContactPublicKey other)
        {
            if (Value == null && other.Value == null)
            {
                return true;
            }

            if (Value == null || other.Value == null)
            {
                return false;
            }

            if (Value.Length != other.Value.Length)
            {
                return false;
            }

            return Value.SequenceEqual(other.Value);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            if (Value == null)
            {
                return 0;
            }

            // Use HashCode.Combine for the first few bytes for performance
            // Since the key is 32 bytes, we'll hash chunks to balance performance and distribution
            var hash = new HashCode();

            // Hash first 8 bytes (or less if array is smaller)
            int length = Math.Min(8, Value.Length);
            for (int i = 0; i < length; i++)
            {
                hash.Add(Value[i]);
            }

            // Hash middle bytes for better distribution
            if (Value.Length > 16)
            {
                hash.Add(Value[16]);
                hash.Add(Value[17]);
            }

            // Hash last bytes
            if (Value.Length > 24)
            {
                hash.Add(Value[Value.Length - 1]);
                hash.Add(Value[Value.Length - 2]);
            }

            return hash.ToHashCode();
        }
    }
}