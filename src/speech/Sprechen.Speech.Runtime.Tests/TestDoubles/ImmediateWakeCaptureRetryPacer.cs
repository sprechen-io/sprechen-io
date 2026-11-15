namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Überspringt die Wartezeit zwischen Aufnahmeversuchen für Tests,
/// die das Verhalten der Verzögerung nicht prüfen.
/// </summary>
internal sealed class ImmediateWakeCaptureRetryPacer : IWakeCaptureRetryPacer
{
    public ValueTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;
}
