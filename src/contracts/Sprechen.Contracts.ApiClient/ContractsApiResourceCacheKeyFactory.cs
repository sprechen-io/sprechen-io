namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Kodiert jedes Segment des Cache-Schlüssels, damit enthaltene Schrägstriche nicht mit den Segmentgrenzen verwechselt werden.
/// </summary>
internal sealed class ContractsApiResourceCacheKeyFactory
{
    public string Create(params string[] segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return string.Join("/", segments.Select(EncodeSegment));
    }

    private static string EncodeSegment(string segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment);

        return Uri.EscapeDataString(segment.Trim());
    }
}
