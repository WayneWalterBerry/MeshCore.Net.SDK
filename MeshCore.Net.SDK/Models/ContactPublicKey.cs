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
            return Convert.ToHexString(Value);
        }
    }
}