namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Liefert die begrenzte Lautheitsanhebung je TTS-Sprache anhand eines
/// Katalogs gemessener Aura-Stimmen. Ziel ist die Referenzlautheit R = −16 LUFS
/// der Hinweistöne. Die technische Obergrenze kann die Anhebung darunter halten.
///
/// Jeder Eintrag verbindet den Messwert mit der vermessenen Stimme, damit ein
/// Abgleich mit <c>DeepgramOptions.TtsModels[iso]</c> einen Stimmenwechsel erkennt.
/// Die Anhebung setzt den vorausschauenden Limiter im Wiedergabe-DSP voraus, der
/// PCM-Spitzen vor der erneuten Abtastung im Browser begrenzt. Aura erreicht bereits 0 dBFS.
/// Unbekannte oder leere Sprachcodes erhalten den Faktor 1,0. Laufzeitzustand wird nicht gehalten.
/// </summary>
internal sealed class TtsOutputLoudnessMakeup
{
    // Schlüssel ist der ISO-639-1-Code. Die hinterlegte Anhebung ergibt sich aus R minus
    // dem Median der integrierten Lautheit von vier repräsentativen Äußerungen.
    // Das Datum gilt jeweils für die vermessene Stimme, damit ein Stimmenwechsel
    // einen eigenen neuen Messstand erhält. Aurelia ist hier die leiseste Stimme:
    // Median −31,4 LUFS aus −32,3, −31,6, −31,1 und −31,1 LUFS. Ihre benötigte Anhebung
    // liegt deutlich über den anderen Einträgen und beansprucht den Limiter stärker.
    // Die Stimmenwahl nimmt das zugunsten ihres Klangcharakters in Kauf.
    private static readonly IReadOnlyDictionary<string, MakeupEntry> EntriesByIso =
        new Dictionary<string, MakeupEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = new MakeupEntry("aura-2-aurelia-de", 15.4), // gemessen am 2026-07-31
            ["en"] = new MakeupEntry("aura-2-iris-en", 2.2), // gemessen am 2026-07-01
            ["fr"] = new MakeupEntry("aura-2-agathe-fr", 8.0), // gemessen am 2026-07-01
            ["es"] = new MakeupEntry("aura-2-silvia-es", 7.1), // gemessen am 2026-07-01
            ["it"] = new MakeupEntry("aura-2-maia-it", 8.5), // gemessen am 2026-07-01
        };

    // Die gemessene Anhebung beschreibt den Bedarf bis R. Tatsächlich angewendet wird
    // höchstens diese technische Grenze. Mehr Verstärkung hebt auch das Eigenrauschen
    // und den Arbeitspunkt des Limiters an. Die Aufnahme kalibriert ihre Einsatzschwelle
    // direkt nach der Sprachausgabe. Die Grenze soll verhindern, dass kurze Antworten
    // danach als Stille gelten. Die übrigen Einträge benötigen 2,2 bis 8,5 dB.
    // Nur Deutsch wird hier auf 9 dB begrenzt und bleibt rechnerisch rund 6 dB
    // unter der Referenz. Die Grenze ist eine technische Obergrenze, keine Messung.
    private const double MaxAppliedMakeupDb = 9.0;

    public IReadOnlyDictionary<string, MakeupEntry> Entries => EntriesByIso;

    /// <summary>
    /// Liefert die durch die Obergrenze begrenzte Anhebung eines Eintrags in Dezibel.
    /// Der direkte Wert erlaubt ihre Prüfung ohne Rückrechnung aus dem linearen Faktor.
    /// </summary>
    public static double GetAppliedMakeupDb(MakeupEntry entry)
        => Math.Min(entry.MakeupDb, MaxAppliedMakeupDb);

    /// <summary>
    /// Liefert den linearen Verstärkungsfaktor aus der begrenzten Anhebung für den
    /// Sprachcode einer Äußerung, etwa "de" oder "de-DE". Unbekannte oder leere Codes
    /// erhalten den Faktor 1,0 und damit keine Anhebung.
    /// </summary>
    public double GetFactor(string? languageCode)
    {
        var iso = NormalizeIso(languageCode);
        if (iso is not null && EntriesByIso.TryGetValue(iso, out var entry))
        {
            return Math.Pow(10, GetAppliedMakeupDb(entry) / 20.0);
        }

        return 1.0;
    }

    private static string? NormalizeIso(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        var trimmed = languageCode.Trim();
        var primary = trimmed.Length >= 2 ? trimmed[..2] : trimmed;
        return primary.ToLowerInvariant();
    }
}
