namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Kennzeichnet das ausgeschöpfte Versuchslimit eines Erkennungsauftrags und bewahrt dessen letzten Fehler als innere Ausnahme.
/// Der stabile Fehlercode <see cref="CodeValue"/> erlaubt die Zuordnung unabhängig vom Meldungstext.
/// </summary>
public sealed class SpeechRecognitionBackoffExhaustedException : InvalidOperationException
{
    public const string CodeValue = "STT_BACKOFF_EXHAUSTED";

    public SpeechRecognitionBackoffExhaustedException(int maxRetries, Exception? lastFailure)
        : base(
            $"Speech recognition gave up after {maxRetries} consecutive failures.",
            lastFailure)
    {
        Code = CodeValue;
        MaxRetries = maxRetries;
    }

    public string Code { get; }

    public int MaxRetries { get; }
}
