namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält sprach- und anredeabhängige Hinweise für nicht erkannte Aktivierung:
/// einen ersten direkten Text und einen Folgetext mit Alternativen.
/// Fehlerschwelle und wachsende Mindestabstände steuern die Freigabe.
/// Ohne diesen optionalen Wert fordert der Erkennungsablauf keine solche Hilfe an.
/// </summary>
public sealed class WakeRecoveryGuidance
{
    public WakeRecoveryGuidance(
        string primaryGuidanceText,
        string alternativesGuidanceText,
        string isoCode,
        int missThreshold,
        TimeSpan cooldown,
        TimeSpan maxCooldown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(primaryGuidanceText);
        ArgumentException.ThrowIfNullOrWhiteSpace(alternativesGuidanceText);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(missThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(cooldown, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCooldown, cooldown);

        PrimaryGuidanceText = primaryGuidanceText;
        AlternativesGuidanceText = alternativesGuidanceText;
        IsoCode = isoCode;
        MissThreshold = missThreshold;
        Cooldown = cooldown;
        MaxCooldown = maxCooldown;
        Backoff = new EscalatingCooldown(cooldown, maxCooldown);
    }

    /// <summary>
    /// Direkter erster Hinweis, lokalisiert nach <see cref="IsoCode"/> und Höflichkeitsform.
    /// </summary>
    public string PrimaryGuidanceText { get; }

    /// <summary>
    /// Fester Hinweis mit Aktivierungsalternativen für die zweite und jede weitere
    /// freigegebene Hilfe. Lokalisiert nach <see cref="IsoCode"/> und Höflichkeitsform.
    /// </summary>
    public string AlternativesGuidanceText { get; }

    public string IsoCode { get; }

    /// <summary>
    /// Erforderliche Folge nicht erkannter Aktivierungen vor dem ersten Erfolg.
    /// </summary>
    public int MissThreshold { get; }

    /// <summary>
    /// Basisintervall für den wachsenden Abstand zwischen Hilfen.
    /// </summary>
    public TimeSpan Cooldown { get; }

    /// <summary>
    /// Obergrenze des wachsenden Ansageabstands. Sie begrenzt nicht die
    /// Anzahl der Hilfen. Deren Freigabe wird zusätzlich geprüft.
    /// </summary>
    public TimeSpan MaxCooldown { get; }

    /// <summary>
    /// Berechnet den wachsenden Abstand aus Basisintervall und Obergrenze.
    /// </summary>
    internal EscalatingCooldown Backoff { get; }
}
