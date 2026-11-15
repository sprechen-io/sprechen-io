using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kapselt den Abgabeversuch im Sprachdialog und liefert dessen Ergebnis an die übergeordnete Dialogsteuerung zurück.
/// </summary>
public interface IFormVoiceCommandSubmissionService
{
    Task<FormSubmissionResult> TrySubmitAsync(
        IFormDialogRuntime interpreter,
    FormContract contract,
    EvaluationSession session,
    string isoCode,
    VoiceDialogSettings voiceSettings,
    IFormVoiceCommandDialogStateTracker stateTracker,
    CancellationToken cancellationToken);
}


