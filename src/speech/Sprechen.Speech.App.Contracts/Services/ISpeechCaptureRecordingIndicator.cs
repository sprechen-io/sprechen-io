
namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt den Aufnahmestatus bereit und kann ihn um hörbare Zuhör- und Verarbeitungssignale ergänzen.
/// </summary>
public interface ISpeechCaptureRecordingIndicator
{
    bool IsRecording { get; }

    ValueTask<IAsyncDisposable> BeginRecordingAsync();

    ValueTask PlayInputAcceptedCueAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    /// <summary>
    /// Beendet einen laufenden Verarbeitungston. Vor einer erneuten Aufnahme ohne
    /// neuen Bereitschaftston muss der Aufrufer ihn ausdrücklich stoppen, damit
    /// die Aufnahme nicht auf das Ende der Schleifenwiedergabe wartet.
    /// Für Indikatoren ohne Audiowiedergabe ist die Standardimplementierung leer.
    /// </summary>
    ValueTask StopProcessingCueAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    ValueTask EnsureStoppedAsync();

    IDisposable SubscribeToRecordingStateChanged(Func<SpeechAssistantRecordingStateChanged, ValueTask> handler);
}
