namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Kennzeichnet einen abgewiesenen Erkennungsauftrag bei voller Warteschlange.
/// Aufrufer verwenden den stabilen Fehlercode <see cref="CodeValue"/> statt eines Vergleichs mit dem Meldungstext.
/// </summary>
public sealed class SpeechRecognitionQueueFullException : InvalidOperationException
{
    public const string CodeValue = "STT_QUEUE_FULL";

    public SpeechRecognitionQueueFullException(int queueDepth)
        : base($"Speech recognition request rejected: worker queue at capacity ({queueDepth}).")
    {
        Code = CodeValue;
        QueueDepth = queueDepth;
    }

    public string Code { get; }

    public int QueueDepth { get; }
}
