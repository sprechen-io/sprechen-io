namespace Sprechen.Speech.ModelRuntime.Services;

/// <summary>
/// Vereinheitlicht Sprachschlüssel vor Lexikonzugriffen. Fehlende oder formal ungültige Kennungen liefern keinen Wert, die Unterstützung prüft der Katalog.
/// </summary>
internal sealed class VoiceLexiconIsoCodeNormalizer
{
    internal static string? Normalize(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return null;
        }

        return IsoCode.TryNormalizeIso6391(isoCode);
    }
}
