namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Beschreibt den lexikalischen Aktivierungsersatz samt Begründung für Zustandsabfragen und Rückmeldungen.
/// </summary>
public sealed record SpeechWakeListeningFallbackState(
    bool IsLexicalFallbackActive,
    string ReasonCode,
    LexicalWakeFallbackReason Reason)
{
    public static SpeechWakeListeningFallbackState Inactive { get; } =
        new(false, string.Empty, LexicalWakeFallbackReason.None);

    public bool HasReasonCode => !string.IsNullOrWhiteSpace(ReasonCode);
}
