namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verknüpft die Veröffentlichung des lexikalischen Ersatzpfads mit Grund und möglichem Fehler, damit fehlgeschlagene Übergaben erkennbar bleiben.
/// </summary>
public sealed record WakeWordFallbackEvent(
    string CorrelationId,
    WakeWordFallbackPhase Phase,
    string? ReasonCode,
    Exception? Exception);
