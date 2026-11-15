namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Entscheidet, ob ein wegen unzureichender Konfidenz abgelehnter
/// Buchstabierwert zum Vorlesen und Korrigieren angeboten werden soll.
/// </summary>
/// <remarks>
/// Erforderlich sind Buchstabiermodus, eine gemeldete Konfidenz und deren
/// Einstufung als unzureichend. Diese Regel verwendet keinen eigenen Schwellenwert
/// und diagnostiziert nicht, ob Buchstaben tatsächlich falsch erkannt wurden.
/// </remarks>
internal sealed class SpellingConfidenceEscalationPolicy
{
    /// <summary>
    /// Wählt die zusätzliche Korrekturrückfrage nur im Buchstabiermodus bei gemeldeter,
    /// als unzureichend eingestufter Konfidenz. Andere Kombinationen werden hier nicht behandelt.
    /// </summary>
    public SpellingEscalationDecision Decide(
        InputMode expectedInputMode,
        SttConfidenceLevel confidenceLevel,
        int? sttConfidencePercent)
    {
        if (expectedInputMode != InputMode.Spelling)
        {
            return SpellingEscalationDecision.Defer;
        }

        // Ohne gemeldete Konfidenz keine zusätzliche Korrekturrückfrage ableiten.
        if (!sttConfidencePercent.HasValue)
        {
            return SpellingEscalationDecision.Defer;
        }

        // Nur die übergebene unzureichende Vertrauenseinstufung eskalieren.
        return confidenceLevel == SttConfidenceLevel.Insufficient
            ? SpellingEscalationDecision.Escalate
            : SpellingEscalationDecision.Defer;
    }
}
