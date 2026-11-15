using System.Collections.Immutable;
using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Localization;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verwaltet den Formularlauf und koordiniert Fokusinitialisierung, Anfangsregeln und Befehlszugänge.
/// </summary>
internal sealed class FormInterpreterRuntimeOwner :
    IFormDialogRuntime,
    IFormDialogPrivacyReadbackPolicy,
    IFormDialogAdditionalFieldProposalRuntime
{
    private const int MaxRuleIterations = 10;

    private readonly FormRuntimeComposition _composition;
    private readonly string _sessionId;
    private readonly FormDialogRuntimeOwnedPorts _ports;

    internal FormTranscriptApplicator TranscriptApplicationService => _composition.TranscriptApplicationService;

    internal FormValidationErrorFocusPolicy ValidationErrorFocusService => _composition.ValidationErrorFocusService;

    internal FormOpenRequiredFieldFocusPolicy OpenRequiredFieldFocusService => _composition.OpenRequiredFieldFocusService;

    internal FormDialogDefinitionState DefinitionState => _composition.DefinitionState;

    internal FormDialogDependencies Services => _composition.Services;

    internal FormMutableStateStore MutableState => _composition.MutableState;

    internal FormInterpreterRuntimeOwner(
        FormContract contract,
        string requestedIsoCode,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
        : this(
            contract,
            requestedIsoCode,
            initialValuesByVariableId,
            FormTranscriptParsers.CreateDefault(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            PolitenessLevel.Formal)
    {
    }

    internal FormInterpreterRuntimeOwner(
        FormContract contract,
        string requestedIsoCode,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        PolitenessLevel politenessLevel)
        : this(
            contract,
            requestedIsoCode,
            initialValuesByVariableId,
            FormTranscriptParsers.CreateDefault(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            politenessLevel)
    {
    }

    internal FormInterpreterRuntimeOwner(
        FormContract contract,
        string requestedIsoCode,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        IFormTranscriptParser transcriptParser)
        : this(contract, requestedIsoCode, initialValuesByVariableId, transcriptParser, PolitenessLevel.Formal)
    {
    }

    internal FormInterpreterRuntimeOwner(
        FormContract contract,
        string requestedIsoCode,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        IFormTranscriptParser transcriptParser,
        PolitenessLevel politenessLevel,
        string? sessionId = null,
        IFormRuleLoopGuardFeedback? loopGuardFeedback = null)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(transcriptParser);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);

        _sessionId = sessionId ?? Guid.NewGuid().ToString("N");

        var composition = FormRuntimeCompositionFactory.Create(
            contract,
            requestedIsoCode,
            politenessLevel,
            transcriptParser,
            initialValuesByVariableId,
            loopGuardFeedback ?? NullFormRuleLoopGuardFeedback.Instance,
            MaxRuleIterations);

        _composition = composition;
        _ports = new FormDialogRuntimeOwnedPorts(composition, this, this);

        EnsureFocusIsInitialized();
        // Der Regelablauf übernimmt Wiederherstellung und Fehlerrückmeldung bei fehlender Konvergenz.
        _ = TryApplyRules(RuleScopes.OnInit, out _);
        EnsureFocusIsValid();
    }

    public FormContract Contract => DefinitionState.Contract;

    public PolitenessLevel PolitenessLevel => DefinitionState.PolitenessLevel;

    internal FormCqrsProjectionAdapter CqrsProjection => Services.CqrsProjectionService;

    internal IEventStore EventStore => _composition.EventStore;

    public Task<ImmutableDictionary<string, string>> GetValuesSnapshotAsync()
        => _composition.StatusExecutor.GetValuesSnapshotAsync();

    public Task<ImmutableDictionary<string, FormSurfaceFieldValue>> GetSurfaceFieldValuesAsync()
        => _ports.GetSurfaceFieldValuesAsync();

    public IFormDialogSilentInput SilentInput => _ports;

    public Task<string?> GetFocusedFieldIdAsync()
        => Task.FromResult(_composition.MutableState.FocusedFieldId is { } fieldId ? fieldId.Value : null);

    public Task<FormFieldSnapshot> GetFocusedFieldAsync()
        => _composition.FocusExecutor.GetFocusedFieldAsync();

    public Task<FormDiscoverySnapshot> GetDiscoverySnapshotAsync()
        => _composition.StatusExecutor.GetDiscoverySnapshotAsync();

    public Task<FormStatusSnapshot> GetStatusSnapshotAsync()
        => _composition.StatusExecutor.GetStatusSnapshotAsync();

    public Task<FormDetailedStatusSnapshot> GetDetailedStatusSnapshotAsync()
        => _composition.StatusExecutor.GetDetailedStatusSnapshotAsync();

    public Task<ImmutableArray<FormSummaryItem>> BuildSummaryItemsAsync()
        => _composition.StatusExecutor.BuildSummaryItemsAsync();

    public async Task<bool> TryMoveFocusNextAsync()
        => (await _composition.NavigationExecutor.NavigateNextAsync(_sessionId).ConfigureAwait(false)).Success;

    public async Task<bool> TryMoveFocusBackAsync()
        => (await _composition.NavigationExecutor.NavigateBackAsync(_sessionId).ConfigureAwait(false)).Success;

    public Task<FormJumpToResult> TryJumpToAsync(string target)
        => _ports.TryJumpToAsync(target);

    public async Task<bool> TryNavigateToFieldGroupAsync(string fieldGroupId)
        => (await _composition.NavigationExecutor.NavigateToFieldGroupAsync(_sessionId, fieldGroupId).ConfigureAwait(false)).Success;

    public Task<FormInputResult> TryApplyTranscriptAsync(string transcript)
        => _composition.EditingExecutor.ApplyTranscriptAsync(transcript);

    public Task<FormInputResult> TryApplyComposedValueAsync(string composedValue)
        => _composition.EditingExecutor.ApplyComposedValueAsync(composedValue);

    internal ImmutableArray<FormAdditionalFieldValueCandidate> ExtractAdditionalFieldCandidates(string transcript)
        => _ports.ExtractAdditionalFieldCandidates(transcript);

    public Task<ImmutableArray<FormAdditionalFieldValueCandidate>> ExtractAdditionalFieldCandidatesAsync(string transcript)
        => Task.FromResult(ExtractAdditionalFieldCandidates(transcript));

    public Task<FormAdditionalFieldProposalPreparation> PrepareAdditionalFieldProposalAsync(
        FormAdditionalFieldValueCandidate candidate)
        => Task.FromResult(_ports.PrepareAdditionalFieldProposal(candidate));

    public Task<FormAdditionalFieldValueAdoptionResult> CommitAdditionalFieldProposalAsync(
        FormAdditionalFieldProposalCommitToken commitToken)
        => Task.FromResult(_ports.CommitAdditionalFieldProposal(commitToken));

    public Task AbandonAdditionalFieldProposalAsync(FormAdditionalFieldProposalCommitToken commitToken)
    {
        _ports.AbandonAdditionalFieldProposal(commitToken);
        return Task.CompletedTask;
    }

    public Task<ImmutableArray<FormConstraintViolation>> ValidateAllAsync()
        => _composition.StatusExecutor.ValidateAllAsync();

    public Task<FormConstraintViolation?> TryFocusNextErrorAsync()
        => _composition.NavigationExecutor.FocusNextErrorAsync();

    public Task<FocusNextOpenRequiredFieldResult> TryFocusNextOpenRequiredFieldAsync()
        => _composition.NavigationExecutor.FocusNextOpenRequiredFieldAsync();

    public Task<ImmutableArray<string>> TryApplySubmitAttemptRulesAsync()
        => _composition.RulesExecutor.ApplySubmitAttemptRulesAsync();

    public Task<ImmutableArray<string>> TryApplyEnterFieldGroupRulesAsync()
        => _composition.RulesExecutor.ApplyEnterFieldGroupRulesAsync();

    public Task<bool> TrySetFocusToFieldIdAsync(string fieldId)
        => _composition.FocusExecutor.SetFocusToFieldIdAsync(fieldId);

    public Task<bool> TrySetFocusedFieldInputModeAsync(string inputMode)
        => _composition.FocusExecutor.SetFocusedFieldInputModeAsync(inputMode);

    public Task<FormFieldInputModeAvailability> GetFocusedFieldInputModeAvailabilityAsync(string inputMode)
        => _composition.FocusExecutor.GetFocusedFieldInputModeAvailabilityAsync(inputMode);

    public Task RecordDeniedFieldTurnAnnouncementAsync()
        => _composition.FocusExecutor.RecordDeniedFieldTurnAnnouncementAsync();

    public Task<bool> HasDeniedFieldTurnAnnouncementAsync()
        => _composition.FocusExecutor.HasDeniedFieldTurnAnnouncementAsync();

    public Task<VoiceInputAllowedResult> IsVoiceInputAllowedAsync(string variableId)
        => _composition.PrivacyExecutor.IsVoiceInputAllowedAsync(variableId);

    public Task<VoiceInputAllowedResult> ShouldOfferSilentInputOptionAsync(string variableId)
        => _composition.PrivacyExecutor.ShouldOfferSilentInputOptionAsync(variableId);

    public Task<bool> RequiresReadbackOptInAsync(string variableId)
        => _composition.PrivacyExecutor.RequiresReadbackOptInAsync(variableId);

    public Task<bool> CanSpeakValueInConfirmationAsync(string variableId, bool explicitOptIn)
        => _composition.PrivacyExecutor.CanSpeakValueInConfirmationAsync(variableId, explicitOptIn);

    public Task<bool> IsSpellOutAllowedInConfirmationAsync(string variableId)
        => _composition.PrivacyExecutor.IsSpellOutAllowedInConfirmationAsync(variableId);

    public Task<string> BuildMaskedReadbackAsync(string variableId, string value, string isoCode)
        => _composition.PrivacyExecutor.BuildMaskedReadbackAsync(variableId, value, isoCode);

    private void EnsureFocusIsInitialized()
        => Services.Navigator.EnsureFocusIsValid();

    private void EnsureFocusIsValid()
        => Services.Navigator.EnsureFocusIsValid();

    private bool TryApplyRules(string scope, out ImmutableArray<string> promptIdsToSpeak)
        => Services.RuleOrchestrationService.TryApplyRules(scope, out promptIdsToSpeak);
}
