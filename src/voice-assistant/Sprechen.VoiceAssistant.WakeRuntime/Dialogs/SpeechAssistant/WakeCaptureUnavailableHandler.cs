using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt klassifizierte Mikrofonfehler beim Start und während der Aufnahme.
/// Nach Freigabe durch <see cref="MicrophoneAvailabilityGate"/> fordert er
/// einen ursachenspezifischen Hinweis bei der Sprachausgabe an.
/// </summary>
internal sealed class WakeCaptureUnavailableHandler
{
    private readonly MicrophoneAvailabilityGate _gate;

    public WakeCaptureUnavailableHandler(MicrophoneAvailabilityGate gate)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
    }

    /// <summary>
    /// Vermerkt den Erfassungsfehler und fordert bei erfüllter Fehlerschwelle
    /// und eingehaltenem Mindestabstand einen Hinweis an. Bestätigt den Zustand
    /// erst nach erfolgreicher Rückkehr der Wiedergabe. Andere Wiedergabefehler
    /// liefern false. Ein angeforderter Abbruch wird weitergegeben.
    /// </summary>
    public async ValueTask<bool> HandleAsync(
        WakeRuntimeContext context,
        WakeCaptureUnavailableReason reason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _gate.RecordUnavailable();

        var guidance = context.MicrophoneAvailabilityGuidance;
        if (!_gate.ShouldSpeak(guidance))
        {
            return false;
        }

        var text = guidance.SelectText(reason);

        try
        {
            // Auf die klassifizierte technische Ursache hinweisen, weil die
            // Erfassung das Mikrofon nicht nutzen konnte.
            await context.TextToSpeechPlayer
                .SpeakAsync(text, guidance.IsoCode, speechRate: 1.0d, SpokenAnnouncementKind.RecoverableFailure, cancellationToken)
                .ConfigureAwait(false);
            _gate.RecordAnnouncementSpoken();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Andere Wiedergabefehler verhindern hier die Bestätigung, unterbrechen aber
            // nicht den Wiederholungsweg. Abbruch durch den Aufrufer wurde zuvor weitergegeben.
            return false;
        }
    }

    public async ValueTask<bool> HandleRecoveryAsync(
        WakeRuntimeContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_gate.RecordHealthyCapture())
        {
            return false;
        }

        var guidance = context.MicrophoneAvailabilityGuidance;

        try
        {
            await context.TextToSpeechPlayer
                .SpeakAsync(
                    guidance.RecoveryText,
                    guidance.IsoCode,
                    speechRate: 1.0d,
                    SpokenAnnouncementKind.RecoverableFailure,
                    cancellationToken)
                .ConfigureAwait(false);
            _gate.RecordRecoveryAnnouncementSpoken();
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
