using System.Text;

namespace Sprechen.Service.Abstractions;

/// <summary>
/// Prüft Anzahl und Größe der Erkennungsbegriffe und entfernt äußere Leerzeichen.
/// Aufrufer dürfen nur modellierte Begriffe liefern, keine Transkripte oder Feldwerte.
/// Die inhaltliche Herkunft kann diese Prüfung nicht feststellen.
/// Mit zusätzlichem Servervokabular bleibt das Anfragebudget kleiner, um Platz für dessen Begriffe zu lassen.
/// Die Bytebudgets begrenzen zusätzlich das Protokollvolumen der Anfragebegriffe.
/// </summary>
public sealed class SpeechKeytermPolicy
{
    /// <summary>
    /// Höchstzahl der Anfragebegriffe bei zusätzlichem serverseitigem Vokabular.
    /// </summary>
    public const int MaximumCount = 12;

    /// <summary>
    /// Höchstzahl der Anfragebegriffe ohne zusätzliches serverseitiges Vokabular.
    /// </summary>
    public const int MaximumCountWithoutServerBase = 100;

    /// <summary>
    /// Maximale UTF-16-Länge eines normalisierten Begriffs.
    /// </summary>
    public const int MaximumTermLength = 128;

    /// <summary>
    /// Maximale UTF-8-Größe eines normalisierten Begriffs in Bytes.
    /// </summary>
    public const int MaximumTermUtf8Bytes = 256;

    /// <summary>
    /// Maximale UTF-8-Gesamtgröße der normalisierten Anfragebegriffe in Bytes.
    /// </summary>
    public const int MaximumTotalUtf8Bytes = 512;

    /// <summary>
    /// Maximale UTF-8-Gesamtgröße ohne zusätzliches serverseitiges Vokabular in Bytes.
    /// </summary>
    public const int MaximumTotalUtf8BytesWithoutServerBase = 4096;

    /// <summary>
    /// Normalisiert die Begriffe und übernimmt den angegebenen Kombinationsmodus unverändert.
    /// </summary>
    public SpeechKeyterms? Normalize(SpeechKeyterms? keyterms)
    {
        if (keyterms is null)
        {
            return null;
        }

        return new SpeechKeyterms(
            Normalize(keyterms.Terms, MaximumCountFor(keyterms.Composition), MaximumTotalUtf8BytesFor(keyterms.Composition)) ?? [],
            keyterms.Composition);
    }

    /// <summary>
    /// Liefert die zulässige Anzahl an Anfragebegriffen für den Kombinationsmodus.
    /// </summary>
    public static int MaximumCountFor(KeytermComposition composition)
        => composition == KeytermComposition.WithoutServerBase
            ? MaximumCountWithoutServerBase
            : MaximumCount;

    /// <summary>
    /// Liefert die zulässige UTF-8-Gesamtgröße der Anfragebegriffe für den Kombinationsmodus.
    /// </summary>
    public static int MaximumTotalUtf8BytesFor(KeytermComposition composition)
        => composition == KeytermComposition.WithoutServerBase
            ? MaximumTotalUtf8BytesWithoutServerBase
            : MaximumTotalUtf8Bytes;

    /// <summary>
    /// Entfernt äußere Leerzeichen und prüft die Liste gegen die Standardgrenzen.
    /// Reihenfolge und doppelte Begriffe bleiben erhalten. Ungültige Einträge werden abgewiesen.
    /// </summary>
    public IReadOnlyList<string>? Normalize(IReadOnlyList<string>? keyterms)
        => Normalize(keyterms, MaximumCount, MaximumTotalUtf8Bytes);

    private IReadOnlyList<string>? Normalize(
        IReadOnlyList<string>? keyterms,
        int maximumCount,
        int maximumTotalUtf8Bytes)
    {
        if (keyterms is null)
        {
            return null;
        }

        if (keyterms.Count > maximumCount)
        {
            throw new ArgumentException(
                $"Keyterms may contain at most {maximumCount} entries.",
                nameof(keyterms));
        }

        if (keyterms.Count == 0)
        {
            return [];
        }

        var normalized = new string[keyterms.Count];
        var totalUtf8Bytes = 0;
        for (var index = 0; index < keyterms.Count; index++)
        {
            var term = keyterms[index]?.Trim();
            if (string.IsNullOrWhiteSpace(term))
            {
                throw new ArgumentException(
                    $"Context keyterm at index {index} must not be blank.",
                    nameof(keyterms));
            }

            if (term.Length > MaximumTermLength)
            {
                throw new ArgumentException(
                    $"Context keyterm at index {index} exceeds {MaximumTermLength} characters.",
                    nameof(keyterms));
            }

            if (term.Any(char.IsControl))
            {
                throw new ArgumentException(
                    $"Context keyterm at index {index} must not contain control characters.",
                    nameof(keyterms));
            }

            var termUtf8Bytes = Encoding.UTF8.GetByteCount(term);
            if (termUtf8Bytes > MaximumTermUtf8Bytes)
            {
                throw new ArgumentException(
                    $"Context keyterm at index {index} exceeds {MaximumTermUtf8Bytes} UTF-8 bytes.",
                    nameof(keyterms));
            }

            totalUtf8Bytes = checked(totalUtf8Bytes + termUtf8Bytes);
            if (totalUtf8Bytes > maximumTotalUtf8Bytes)
            {
                throw new ArgumentException(
                    $"Keyterms exceed {maximumTotalUtf8Bytes} cumulative UTF-8 bytes.",
                    nameof(keyterms));
            }

            normalized[index] = term;
        }

        return normalized;
    }
}
