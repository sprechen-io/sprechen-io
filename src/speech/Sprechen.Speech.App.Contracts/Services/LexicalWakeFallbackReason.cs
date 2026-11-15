namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Typisierte Gründe für den lexikalischen Ersatzpfad der Sprachaktivierung.
/// Die sechs Gründe entsprechen den Schlüsseln von WakeWordDetectorFallbackReasonCodes.
/// None bezeichnet einen inaktiven Ersatzpfad. Kataloge können die zugehörigen
/// Zeichenketten frei wählen, deshalb bleiben diese von den internen Enum-Werten getrennt.
/// </summary>
public enum LexicalWakeFallbackReason
{
    /// <summary>
    /// Kein Ersatzpfad aktiv. ToWireCode und ResolveCatalogReasonCode weisen diesen Wert
    /// mit ArgumentOutOfRangeException zurück.
    /// </summary>
    None = 0,

    /// <summary>
    /// Profilbedingter Ersatzpfad bei lexicalFallbackOnly oder mehrwortigen Aktivierungsformen.
    /// </summary>
    CatalogFallbackOnly,

    /// <summary>
    /// Die Zulässigkeitsprüfung lehnt die nutzerdefinierte Aktivierungsform ab.
    /// </summary>
    OverrideIneligible,

    /// <summary>
    /// Aktivierungsformen sind nur durch Spracherkennung erreichbar, nicht durch den akustischen Detektor.
    /// </summary>
    RecognitionOnlyActivation,

    DetectorUnavailable,

    /// <summary>
    /// Der akustische Detektor befindet sich noch in seiner modellierten Startphase.
    /// </summary>
    DetectorWarmup,

    /// <summary>
    /// Der akustische Detektor ist nach dem Start fehlgeschlagen.
    /// </summary>
    DetectorFailure,
}
