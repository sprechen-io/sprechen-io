
namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verteilt das Aufnahmeende mit Sprache und optionalem Transkript an interessierte Beobachter.
/// </summary>
public interface ISpeechCaptureStoppedSignals
{
    ValueTask PublishStoppedAsync(string languageCode, string? finalTranscript);

    IDisposable SubscribeToStopped(Func<SpeechCaptureStopped, ValueTask> handler);
}
