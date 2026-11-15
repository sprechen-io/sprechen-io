namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Erkennungswert, geparste Absicht und konkurrierende Hinweise einer Bestätigungsentscheidung
/// für die Diagnose fest. Fehlende Werte bleiben fehlend, damit ein nicht vorhandener Vertrauenswert
/// nicht als gemessene Null erscheint.
/// </summary>
/// <param name="SttConfidencePercent">Der übergebene Vertrauenswert, sofern vorhanden.</param>
/// <param name="Intent">Die geparste Antwortabsicht, sofern vorhanden.</param>
/// <param name="HasCompetingIntentEvidence">Zusätzliche konkurrierende Hinweise des Parsers.</param>
internal sealed record SelectionConfirmationDecisionInputs(
    int? SttConfidencePercent,
    SelectionDialogCommonIntent? Intent,
    bool HasCompetingIntentEvidence);
