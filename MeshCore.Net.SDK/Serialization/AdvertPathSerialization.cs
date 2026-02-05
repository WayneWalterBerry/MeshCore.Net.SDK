using MeshCore.Net.SDK.Models;

namespace MeshCore.Net.SDK.Serialization;

/// <summary>
/// Binary serializer for <see cref="AdvertPathInfo"/> instances using the MeshCore advert path wire format.
/// </summary>
public sealed class AdvertPathSerialization : IBinarySerializer<AdvertPathInfo>
{
    /// <summary>
    /// Gets the singleton instance of the <see cref="AdvertPathSerialization"/> class.
    /// </summary>
    public static AdvertPathSerialization Instance { get; } = new AdvertPathSerialization();

    private AdvertPathSerialization()
    {
    }

    /// <inheritdoc />
    public AdvertPathInfo Deserialize(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length < 5)
        {
            throw new ArgumentException("Advert path payload must be at least 5 bytes long.", nameof(data));
        }

        var timestamp = BitConverter.ToUInt32(data, 0);
        var pathLen = data[4];

        if (data.Length < 5 + pathLen)
        {
            throw new ArgumentException("Advert path payload is shorter than indicated path length.", nameof(data));
        }

        var path = new byte[pathLen];
        Buffer.BlockCopy(data, 5, path, 0, pathLen);

        return new AdvertPathInfo
        {
            ReceivedTimestamp = timestamp,
            Path = path
        };
    }

    /// <inheritdoc />
    public byte[] Serialize(AdvertPathInfo value)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        var path = value.Path ?? Array.Empty<byte>();
        if (path.Length > byte.MaxValue)
        {
            throw new ArgumentException("Advert path length cannot exceed 255 bytes.", nameof(value));
        }

        var buffer = new byte[5 + path.Length];
        var timestampBytes = BitConverter.GetBytes(value.ReceivedTimestamp);
        Buffer.BlockCopy(timestampBytes, 0, buffer, 0, 4);
        buffer[4] = (byte)path.Length;
        if (path.Length > 0)
        {
            Buffer.BlockCopy(path, 0, buffer, 5, path.Length);
        }

        return buffer;
    }
}