namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Wartet vor einem erneuten Aufnahmeversuch zur Aktivierung des Assistenten
/// die angeforderte Zeit ab und berücksichtigt einen Abbruch.
/// </summary>
internal interface IWakeCaptureRetryPacer
{
    ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
