namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verteilt Wiederholungsversuche mit <see cref="IWakeCaptureRetryPacer"/> zeitlich, ohne
/// dafür einen Thread der Browserlaufzeit zu blockieren. Positive Wartezeiten verwenden
/// <see cref="Task.Delay(TimeSpan, CancellationToken)"/>. Nicht positive enden ohne Verzögerung.
/// </summary>
internal sealed class TaskWakeCaptureRetryPacer : IWakeCaptureRetryPacer
{
    public async ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        if (delay <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }
}
