namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Übermittelt beim Aufnahmeende die Sprache und ein gegebenenfalls vorhandenes abschließendes Transkript.
/// </summary>
public sealed record SpeechCaptureStopped(string LanguageCode, string? FinalTranscript);

