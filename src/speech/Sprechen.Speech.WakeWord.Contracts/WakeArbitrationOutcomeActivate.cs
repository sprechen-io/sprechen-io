namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Aktivierung durch akustische Evidenz oder direkte lexikalische Auswertung.
/// Zur angegebenen Quelle gehört die jeweilige Evidenz. Aktivierungen aus
/// zusammengefügten Zyklen werden als <see cref="WakeArbitrationOutcomeStitch"/> übergeben.
/// </summary>
public sealed record WakeArbitrationOutcomeActivate : WakeArbitrationOutcome
{
    public WakeArbitrationOutcomeActivate(
        WakeWordActivationSource source,
        AcousticWakeWordEvidence? acousticEvidence,
        WakeWordActivationEvaluation? lexicalEvaluation)
    {
        Source = source;
        AcousticEvidence = acousticEvidence;
        LexicalEvaluation = lexicalEvaluation;
    }

    public WakeWordActivationSource Source { get; }

    public AcousticWakeWordEvidence? AcousticEvidence { get; }

    public WakeWordActivationEvaluation? LexicalEvaluation { get; }
}
