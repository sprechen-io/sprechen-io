namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Extrahiert einen kleingeschriebenen Sprachcode aus zwei Buchstaben,
/// damit auch Sprachangaben mit Regionszusatz zugeordnet werden können.
/// </summary>
public static class Iso6391LanguageCode
{
    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var token = raw.Trim();
        var dashIndex = token.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            token = token[..dashIndex];
        }

        token = token.ToLowerInvariant();

        if (token.Length != 2 || !token.All(char.IsLetter))
        {
            return false;
        }

        normalized = token;
        return true;
    }
}
