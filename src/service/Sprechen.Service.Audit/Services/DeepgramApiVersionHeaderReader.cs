namespace Sprechen.Service.Audit.Services;

/// <summary>
/// Liest X-DG-Model-Version ohne Unterscheidung der Groß- und Kleinschreibung.
/// Fehlende oder leere Werte ergeben null. Äußere Leerzeichen werden entfernt.
/// </summary>
public static class DeepgramApiVersionHeaderReader
{
    public const string HeaderName = "X-DG-Model-Version";

    public static string? Read(IReadOnlyDictionary<string, string>? responseHeaders)
    {
        if (responseHeaders is null)
        {
            return null;
        }

        foreach (var (key, value) in responseHeaders)
        {
            if (string.Equals(key, HeaderName, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
