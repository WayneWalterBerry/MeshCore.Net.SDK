namespace MeshCore.Net.SDK.Models;

/// <summary>
/// Represents the most recently observed advert path for a contact.
/// </summary>
public sealed class AdvertPathInfo
{
    /// <summary>
    /// Gets or sets the Unix timestamp (seconds since epoch) when the advert was received.
    /// </summary>
    public uint ReceivedTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the raw hop path as a sequence of hop identifiers from this node to the contact.
    /// </summary>
    public byte[] Path { get; set; } = Array.Empty<byte>();

    /// <inheritdoc/>
    public override string ToString()
    {
        var length = this.Path?.Length ?? 0;
        if (length == 0)
        {
            return $"AdvertPath: length=0, received={this.ReceivedTimestamp}, path=";
        }

        const int maxHopsToShow = 16;
        var hopsToShow = length < maxHopsToShow ? length : maxHopsToShow;

        // Build "AA,BB,CC,..." style list for the first N hops
        var parts = new string[hopsToShow];
        for (var i = 0; i < hopsToShow; i++)
        {
            parts[i] = this.Path![i].ToString("X2");
        }

        var preview = string.Join(",", parts);
        var suffix = length > maxHopsToShow ? ",…" : string.Empty;

        return $"AdvertPath: length={length}, received={this.ReceivedTimestamp}, path={preview}{suffix}";
    }

    private static char GetHexChar(int value)
    {
        return (char)(value < 10 ? ('0' + value) : ('A' + (value - 10)));
    }
}