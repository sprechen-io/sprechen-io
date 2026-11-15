using System.Text;

namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Begrenzt modellierte statische Keyterms für die Übertragung an den Sprachdienst.
/// Ungültige und überzählige Erkennungshinweise werden ausgelassen.
/// Der Dienst prüft seine Eingangsgrenzen unabhängig von dieser Clientprojektion.
/// </summary>
public sealed class SpeechKeytermProjectionPolicy
{
    /// <summary>
    /// Höchstzahl zusätzlicher Begriffe, damit Platz für den Serverwortschatz bleibt.
    /// </summary>
    public const int MaximumCountWithServerBase = 12;

    /// <summary>
    /// Höchstzahl ohne Serverwortschatz, abgestimmt auf DeepgramKeytermRequestPolicy.
    /// In diesem Fall muss kein Anteil für zusätzliche Serverbegriffe reserviert werden.
    /// </summary>
    public const int MaximumCountWithoutServerBase = 100;

    private const int MaximumTermLength = 128;
    private const int MaximumTermUtf8Bytes = 256;

    // Begrenzt das Übertragungsvolumen zusätzlich zur Anzahl der Begriffe.
    private const int MaximumTotalUtf8BytesWithServerBase = 512;
    private const int MaximumTotalUtf8BytesWithoutServerBase = 4096;

    /// <summary>
    /// Liefert eine getrimmte, eindeutige Teilmenge in ursprünglicher Reihenfolge.
    /// Anzahl- und Bytebudget richten sich nach <paramref name="composition"/>:
    /// Ohne Serverbasis entfällt die Reserve für deren Wortschatz.
    /// </summary>
    public IReadOnlyList<string> Project(
        IEnumerable<string?> keyterms,
        KeytermComposition composition = KeytermComposition.WithServerBase)
    {
        ArgumentNullException.ThrowIfNull(keyterms);

        var maximumCount = composition == KeytermComposition.WithoutServerBase
            ? MaximumCountWithoutServerBase
            : MaximumCountWithServerBase;
        var maximumTotalUtf8Bytes = composition == KeytermComposition.WithoutServerBase
            ? MaximumTotalUtf8BytesWithoutServerBase
            : MaximumTotalUtf8BytesWithServerBase;

        var projected = new List<string>(maximumCount);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var totalUtf8Bytes = 0;

        foreach (var candidate in keyterms)
        {
            if (projected.Count == maximumCount || totalUtf8Bytes == maximumTotalUtf8Bytes)
            {
                break;
            }

            var term = candidate?.Trim();
            if (string.IsNullOrWhiteSpace(term)
                || term.Length > MaximumTermLength
                || term.Any(char.IsControl)
                || !seen.Add(term))
            {
                continue;
            }

            var termUtf8Bytes = Encoding.UTF8.GetByteCount(term);
            if (termUtf8Bytes > MaximumTermUtf8Bytes
                || totalUtf8Bytes + termUtf8Bytes > maximumTotalUtf8Bytes)
            {
                continue;
            }

            projected.Add(term);
            totalUtf8Bytes += termUtf8Bytes;
        }

        return projected;
    }
}
