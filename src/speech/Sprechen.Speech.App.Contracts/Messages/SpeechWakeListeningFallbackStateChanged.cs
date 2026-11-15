using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Meldet Änderungen des lexikalischen Aktivierungsersatzes samt Grund für die Zustandsanzeige.
/// </summary>
public sealed record SpeechWakeListeningFallbackStateChanged(
    bool IsLexicalFallbackActive,
    string ReasonCode,
    LexicalWakeFallbackReason Reason);
