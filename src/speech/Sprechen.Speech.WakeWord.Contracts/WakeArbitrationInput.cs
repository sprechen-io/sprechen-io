namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt die Evidenz eines Aufnahmezyklus für die Aktivierungsentscheidung:
/// akustische Evidenz, lexikalische Auswertung, Verknüpfungsergebnis und Ersatzgrund.
/// Der Arbitrator prüft die zulässigen Kombinationen und Wertebereiche.
/// </summary>
public sealed record WakeArbitrationInput(
    string CorrelationId,
    WakeWordInventory Inventory)
{
    /// <summary>Optionale akustische Detektorevidenz des aktuellen Zyklus.</summary>
    public AcousticWakeWordEvidence? AcousticEvidence { get; init; }

    /// <summary>Optionale lexikalische Auswertung des Transkripts aus dem aktuellen Zyklus.</summary>
    public WakeWordActivationEvaluation? LexicalEvaluation { get; init; }

    /// <summary>Optionales Ergebnis der zyklusübergreifenden Verknüpfung für das aktuelle Transkript.</summary>
    public WakeStitchResult? StitchResult { get; init; }

    /// <summary>
    /// Grund des angeforderten lexikalischen Ersatzpfads für
    /// <see cref="WakeArbitrationOutcomeLexicalFallback"/>.
    /// <see cref="LexicalWakeFallbackReason.None"/> bedeutet, dass kein Ersatzpfad angefordert wurde.
    /// </summary>
    public LexicalWakeFallbackReason FallbackReason { get; init; } = LexicalWakeFallbackReason.None;
}
