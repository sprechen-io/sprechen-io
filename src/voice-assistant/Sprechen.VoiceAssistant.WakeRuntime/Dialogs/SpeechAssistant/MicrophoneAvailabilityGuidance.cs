namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält sprach- und anredeabhängige Texte für fehlendes Eingabegerät,
/// verweigerte Berechtigung, unterbrochene Erfassung und Entwarnung.
/// Die modellierten Schwellen und Zeitabstände steuern die Ansagefreigabe.
/// </summary>
public sealed class MicrophoneAvailabilityGuidance
{
    public MicrophoneAvailabilityGuidance(
        string deviceNotFoundText,
        string permissionDeniedText,
        string unavailableText,
        string recoveryText,
        string isoCode,
        int unavailableThreshold,
        TimeSpan cooldown,
        TimeSpan maxCooldown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceNotFoundText);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionDeniedText);
        ArgumentException.ThrowIfNullOrWhiteSpace(unavailableText);
        ArgumentException.ThrowIfNullOrWhiteSpace(recoveryText);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentOutOfRangeException.ThrowIfLessThan(unavailableThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(cooldown, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCooldown, cooldown);

        DeviceNotFoundText = deviceNotFoundText;
        PermissionDeniedText = permissionDeniedText;
        UnavailableText = unavailableText;
        RecoveryText = recoveryText;
        IsoCode = isoCode;
        UnavailableThreshold = unavailableThreshold;
        Cooldown = cooldown;
        MaxCooldown = maxCooldown;
        Backoff = new EscalatingCooldown(cooldown, maxCooldown);
    }

    public string DeviceNotFoundText { get; }

    public string PermissionDeniedText { get; }

    /// <summary>
    /// Ansagetext für andere technische Ursachen einer nicht verfügbaren Audioerfassung.
    /// </summary>
    public string UnavailableText { get; }

    /// <summary>
    /// Text zur Entwarnung nach regulärer Erfassung im Anschluss an einen angekündigten Ausfall.
    /// </summary>
    public string RecoveryText { get; }

    public string IsoCode { get; }

    /// <summary>
    /// Anzahl aufeinanderfolgender Erfassungsfehler, ab der eine Ausfallansage freigegeben werden kann.
    /// </summary>
    public int UnavailableThreshold { get; }

    /// <summary>
    /// Anfänglicher Mindestabstand zwischen Ansagen.
    /// </summary>
    public TimeSpan Cooldown { get; }

    /// <summary>
    /// Obergrenze des wachsenden Ansageabstands, keine Höchstzahl von Ansagen.
    /// </summary>
    public TimeSpan MaxCooldown { get; }

    /// <summary>
    /// Berechnet den wachsenden Abstand aus Basisintervall und Obergrenze.
    /// </summary>
    internal EscalatingCooldown Backoff { get; }

    /// <summary>
    /// Liefert die Mindestpause vor erneuter Mikrofonanforderung. Bei verweigerter
    /// Berechtigung gilt das Basisintervall unabhängig vom gewachsenen Ansageabstand.
    /// Andere Ursachen erhalten hier keine zusätzliche Pause.
    /// </summary>
    public TimeSpan RetryFloor(WakeCaptureUnavailableReason reason)
        => reason == WakeCaptureUnavailableReason.PermissionDenied ? Cooldown : TimeSpan.Zero;

    /// <summary>
    /// Wählt den Handlungshinweis zur klassifizierten Ausfallursache.
    /// </summary>
    public string SelectText(WakeCaptureUnavailableReason reason)
        => reason switch
        {
            WakeCaptureUnavailableReason.PermissionDenied => PermissionDeniedText,
            WakeCaptureUnavailableReason.Unavailable or WakeCaptureUnavailableReason.CaptureInterrupted => UnavailableText,
            _ => DeviceNotFoundText,
        };
}
