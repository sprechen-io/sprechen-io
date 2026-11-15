namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verwirft widersprüchliche oder leere Eingabeformen der Aktivierungsentscheidung, damit genau die
/// erlaubten Kombinationen an die Routenauswahl gelangen.
/// </summary>
internal sealed class WakeArbitrationInputConflictValidator
{
    public void Validate(WakeArbitrationInputShape shape)
    {
        if (shape.HasFallback && shape.HasDetectionInputs)
        {
            throw new ArgumentException(
                "WakeArbitrationInput.FallbackReason is mutually exclusive with detection inputs (AcousticEvidence, LexicalEvaluation, StitchResult).",
                "input");
        }

        if (shape.HasAcoustic && shape.HasLexical)
        {
            throw new ArgumentException(
                "WakeArbitrationInput must not carry both AcousticEvidence and LexicalEvaluation in the same arbitration call.",
                "input");
        }

        if (shape.HasAcoustic && shape.HasStitch)
        {
            throw new ArgumentException(
                "AcousticEvidence inputs must not be combined with a StitchResult.",
                "input");
        }

        if (!shape.HasFallback && !shape.HasAcoustic && !shape.HasLexical)
        {
            throw new ArgumentException(
                "WakeArbitrationInput must carry at least one of FallbackReason (!= None), AcousticEvidence, or LexicalEvaluation.",
                "input");
        }
    }
}
