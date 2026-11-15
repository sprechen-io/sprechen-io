using Sprechen.Mda.Contracts.V1.SpeechCapture;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft die Zeitgrenzen der Antwortfenster beim Sitzungseinstieg
/// und den zusätzlichen Ausgleich nach Ansagen.
/// </summary>
internal sealed class SpeechCaptureSessionEntryValidator
{
    /// <summary>
    /// Kleinste zulässige Dauer eines Antwortfensters in Millisekunden.
    /// </summary>
    private const int MinimumWindowMs = 2000;

    /// <summary>
    /// Größte zulässige Dauer eines Antwortfensters in Millisekunden.
    /// </summary>
    private const int MaximumWindowMs = 20000;

    public void Validate(List<PreflightIssue> errors, SpeechCaptureSessionEntryProfile sessionEntry)
    {
        ValidateRange(errors, sessionEntry.FirstContactInitialSilenceTimeoutMs, "firstContactInitialSilenceTimeoutMs");
        ValidateRange(errors, sessionEntry.RepeatInitialSilenceTimeoutMs, "repeatInitialSilenceTimeoutMs");
        ValidateRange(
            errors,
            sessionEntry.LanguageQuestionInitialSilenceTimeoutMs,
            "languageQuestionInitialSilenceTimeoutMs");

        if (sessionEntry.PlaybackSettleAllowanceMs is < 0 or > 2000)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "sessionEntry.playbackSettleAllowanceMs must be between 0 and 2000, got "
                    + $"{sessionEntry.PlaybackSettleAllowanceMs}.",
                Path: "$.sessionEntry.playbackSettleAllowanceMs"));
        }

        // Das Fenster für Wiederkehrende muss kürzer als das Erstkontaktfenster sein,
        // damit die zusätzliche Zeit für eine noch unbekannte Frage nicht jede Sitzung verlängert.
        if (sessionEntry.RepeatInitialSilenceTimeoutMs >= sessionEntry.FirstContactInitialSilenceTimeoutMs)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorSettingsInvalid,
                Message: "sessionEntry.repeatInitialSilenceTimeoutMs must be shorter than "
                    + $"firstContactInitialSilenceTimeoutMs, got {sessionEntry.RepeatInitialSilenceTimeoutMs} "
                    + $"and {sessionEntry.FirstContactInitialSilenceTimeoutMs}.",
                Path: "$.sessionEntry.repeatInitialSilenceTimeoutMs"));
        }
    }

    private static void ValidateRange(List<PreflightIssue> errors, int value, string field)
    {
        if (value >= MinimumWindowMs && value <= MaximumWindowMs)
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorSettingsInvalid,
            Message: $"sessionEntry.{field} must be between {MinimumWindowMs} and {MaximumWindowMs}, got {value}.",
            Path: $"$.sessionEntry.{field}"));
    }
}
