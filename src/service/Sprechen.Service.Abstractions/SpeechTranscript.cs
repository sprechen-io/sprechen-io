namespace Sprechen.Service.Abstractions;

/// <summary>
/// Erkannter Text mit optionaler Erkennungskonfidenz in Prozent von 0 bis 100.
/// </summary>
public sealed record SpeechTranscript(string Text, int? ConfidencePercent);
