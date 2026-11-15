using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob die gemeldete Konfidenz einer akustischen Aktivierung
/// im gültigen Bereich liegt, bevor sie für die Entscheidung verwendet wird.
/// </summary>
internal sealed class AcousticWakeWordEvidenceGuard
{
    public void Validate(WakeArbitrationInput input, WakeArbitrationInputShape shape)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (!shape.HasAcoustic)
        {
            return;
        }

        var evidence = input.AcousticEvidence!;
        if (!IsConfidenceScoreValid(evidence.ConfidenceScore))
        {
            throw new ArgumentException(
                $"AcousticWakeWordEvidence.ConfidenceScore must lie in [0,1]; got {evidence.ConfidenceScore}.",
                nameof(input));
        }
    }

    private static bool IsConfidenceScoreValid(double confidenceScore)
    {
        return !double.IsNaN(confidenceScore)
            && confidenceScore >= 0.0
            && confidenceScore <= 1.0;
    }
}
