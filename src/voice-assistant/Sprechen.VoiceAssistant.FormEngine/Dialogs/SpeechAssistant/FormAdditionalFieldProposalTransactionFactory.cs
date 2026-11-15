using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Baut je Übernahmeversuch eine private Kopie des Formularzustands mit eigenen Diensten auf.
/// Die Definition ist unveränderlich und wird geteilt. Kopiert wird nur der veränderliche Teil,
/// damit ein fehlgeschlagener Versuch den gemeinsamen Zustand nicht berühren kann.
/// </summary>
internal sealed class FormAdditionalFieldProposalTransactionFactory
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _canonicalState;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormRuleLoopGuardFeedback _loopGuardFeedback;
    private readonly int _maxRuleIterations;

    internal FormAdditionalFieldProposalTransactionFactory(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore canonicalState,
        IFormTranscriptParser transcriptParser,
        IFormRuleLoopGuardFeedback loopGuardFeedback,
        int maxRuleIterations)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _canonicalState = canonicalState ?? throw new ArgumentNullException(nameof(canonicalState));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _loopGuardFeedback = loopGuardFeedback ?? throw new ArgumentNullException(nameof(loopGuardFeedback));
        _maxRuleIterations = maxRuleIterations;
    }

    internal FormAdditionalFieldProposalTransaction Begin()
    {
        // Meldungen zur Regeliterationsgrenze bleiben bis zur Übernahme des Ergebnisses zurückgehalten.
        var deferredLoopGuardFeedback = new DeferredFormRuleLoopGuardFeedback(_loopGuardFeedback);
        var privateState = new FormMutableStateStore(
            _definitionState.VariablesById.Keys.ToHashSet(),
            _definitionState.FieldsById.Keys.ToHashSet(),
            _definitionState.FieldGroupsById.Keys.ToHashSet());
        privateState.RestoreSnapshot(TakeCanonicalSnapshot());

        var navigationState = new FormDialogNavigationState(_definitionState, privateState);
        var privateServices = FormDialogServiceFactory.Create(
            _definitionState,
            privateState,
            new FormDialogServiceDependencies(
                _transcriptParser,
                privateState,
                privateState,
                privateState,
                navigationState,
                navigationState),
            deferredLoopGuardFeedback,
            _maxRuleIterations);
        navigationState.Bind(privateServices);

        return new FormAdditionalFieldProposalTransaction(
            privateState,
            privateServices,
            new FormTranscriptApplicator(_definitionState, privateState, privateServices),
            deferredLoopGuardFeedback);
    }

    internal FormDialogStateSnapshot TakeCanonicalSnapshot()
        => FormDialogStateSnapshotStore.Take(
            _canonicalState.ValuesByVariableId,
            _canonicalState.FieldGroupVisibleById,
            _canonicalState.FieldVisibleById,
            _canonicalState.FieldEnabledById,
            _canonicalState.FieldReadOnlyById,
            _canonicalState.FieldInputModeOverrides,
            _canonicalState.FocusedFieldId?.Value);
}
