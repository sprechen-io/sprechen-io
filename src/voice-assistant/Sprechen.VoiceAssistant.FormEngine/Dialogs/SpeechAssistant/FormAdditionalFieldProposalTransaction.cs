using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Wertübernahme, Regeln und Validierung auf einer getrennten Kopie des Formularzustands. Der Aufrufer übernimmt erst das gültige Ergebnis. Verworfene Versuche ändern weder Werte noch Fokus oder Revision des gemeinsamen Zustands.
/// </summary>
internal sealed class FormAdditionalFieldProposalTransaction
{
    private readonly FormMutableStateStore _privateState;
    private readonly FormDialogDependencies _privateServices;
    private readonly FormTranscriptApplicator _privateApplicator;
    private readonly DeferredFormRuleLoopGuardFeedback _loopGuardFeedback;

    internal FormAdditionalFieldProposalTransaction(
        FormMutableStateStore privateState,
        FormDialogDependencies privateServices,
        FormTranscriptApplicator privateApplicator,
        DeferredFormRuleLoopGuardFeedback loopGuardFeedback)
    {
        _privateState = privateState ?? throw new ArgumentNullException(nameof(privateState));
        _privateServices = privateServices ?? throw new ArgumentNullException(nameof(privateServices));
        _privateApplicator = privateApplicator ?? throw new ArgumentNullException(nameof(privateApplicator));
        _loopGuardFeedback = loopGuardFeedback ?? throw new ArgumentNullException(nameof(loopGuardFeedback));
    }

    /// <summary>
    /// Wertet das Transkript für das Zielfeld aus und prüft anschließend den Fokus. Die spätere Zustandsübernahme benötigt dadurch keinen getrennten Fokuswechsel.
    /// </summary>
    internal FormInputResult ApplyToField(FieldId fieldId, string transcript)
    {
        var result = _privateApplicator.TryApplyTranscriptToField(fieldId, transcript);
        _privateServices.Navigator.EnsureFocusIsValid();
        return result;
    }

    /// <summary>
    /// Leitet zurückgehaltene Meldungen zur Regeliterationsgrenze weiter. Der Aufrufer darf dies erst nach Übernahme des Ergebnisses ausführen.
    /// </summary>
    internal void ReplayLoopGuardBreaches() => _loopGuardFeedback.ReplayPendingBreaches();

    internal FormDialogStateSnapshot TakeSnapshot()
        => FormDialogStateSnapshotStore.Take(
            _privateState.ValuesByVariableId,
            _privateState.FieldGroupVisibleById,
            _privateState.FieldVisibleById,
            _privateState.FieldEnabledById,
            _privateState.FieldReadOnlyById,
            _privateState.FieldInputModeOverrides,
            _privateState.FocusedFieldId?.Value);
}
