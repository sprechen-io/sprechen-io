namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verbindet einen typisierten Sprachfehlercode mit Beschreibung und optionaler Ursache für die Fehlerbehandlung.
/// </summary>
public sealed class SpeechErrorException : Exception
{
    public SpeechErrorException(SpeechErrorCode errorCode, string message)
        : this(errorCode, message, innerException: null)
    {
    }

    public SpeechErrorException(SpeechErrorCode errorCode, string message, Exception? innerException)
        : base(string.IsNullOrWhiteSpace(message) ? "Speech operation failed." : message, innerException)
    {
        ErrorCode = errorCode;
    }

    public SpeechErrorCode ErrorCode { get; }
}
