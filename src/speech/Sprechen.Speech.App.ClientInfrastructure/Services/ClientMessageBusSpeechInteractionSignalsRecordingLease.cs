namespace Sprechen.Speech.App.ClientInfrastructure.Services;

/// <summary>
/// Gibt beim ersten asynchronen Freigeben genau einen gezählten
/// Aufnahmeanspruch an das zuständige Signalobjekt zurück.
/// </summary>
internal sealed class ClientMessageBusSpeechInteractionSignalsRecordingLease(
    ClientMessageBusSpeechRecordingSignals owner) : IAsyncDisposable
{
    private readonly ClientMessageBusSpeechRecordingSignals _owner =
        owner ?? throw new ArgumentNullException(nameof(owner));

    private int _disposed;

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return ValueTask.CompletedTask;
        }

        return _owner.ReleaseRecordingAsync();
    }
}
