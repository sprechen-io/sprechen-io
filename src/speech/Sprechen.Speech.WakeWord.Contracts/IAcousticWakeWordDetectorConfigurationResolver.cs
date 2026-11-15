namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Erstellt die anfängliche Detektorkonfiguration und leitet daraus bei Bedarf
/// einen lexikalischen Ersatzpfad mit dem modellierten Grundcode ab.
/// </summary>
public interface IAcousticWakeWordDetectorConfigurationResolver
{
    ValueTask<AcousticWakeWordDetectorConfiguration> ResolveAsync(
        string isoCode,
        WakeWordInventory wakeWordInventory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Leitet aus der Ausgangskonfiguration eine Variante mit aktiviertem lexikalischem Ersatzpfad ab.
    /// Der Grundcode wird synchron aus ihrem Detektorprofil gelesen.
    /// </summary>
    /// <param name="reason">Ersatzgrund. None und unbekannte Enum-Werte werden abgewiesen.</param>
    /// <param name="baseConfiguration">Ausgangskonfiguration. Sprache, Inventar und Profil bleiben erhalten.</param>
    /// <exception cref="ArgumentNullException">Die Ausgangskonfiguration ist null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Der Ersatzgrund ist None oder unbekannt.</exception>
    AcousticWakeWordDetectorConfiguration ResolveForLexicalFallback(
        LexicalWakeFallbackReason reason,
        AcousticWakeWordDetectorConfiguration baseConfiguration);
}
