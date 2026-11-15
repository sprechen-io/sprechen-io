using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bietet gezielte Sprünge, stille Eingabe, Feldwertansichten und Vorschläge für zusätzliche Feldwerte an.
/// </summary>
internal sealed class FormDialogRuntimeOwnedPorts : IFormDialogSilentInput
{
    private readonly FormJumpToCoordinator _jumpTo;
    private readonly FormSilentInputApplicator _silentInput;
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormDialogDependencies _services;
    private readonly FormAdditionalFieldProposalDependencies _additionalFieldProposal;

    internal FormDialogRuntimeOwnedPorts(
        FormRuntimeComposition composition,
        IFormDialogRuntime runtime,
        IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(composition);
        _definitionState = composition.DefinitionState;
        _mutableState = composition.MutableState;
        _services = composition.Services;
        _jumpTo = new FormJumpToCoordinator(composition, runtime, privacyReadbackPolicy);
        _silentInput = new FormSilentInputApplicator(_definitionState, _mutableState, _services, runtime);
        _additionalFieldProposal = composition.AdditionalFieldProposal;
    }

    internal Task<FormJumpToResult> TryJumpToAsync(string target)
        => _jumpTo.TryJumpToAsync(target);

    internal Task<ImmutableDictionary<string, FormSurfaceFieldValue>> GetSurfaceFieldValuesAsync()
        => Task.FromResult(
            new FormSurfaceFieldValueProjectionBuilder(_definitionState, _mutableState, _services.VisibilityService).Build());

    public Task<FormSilentInputEligibilityProjection?> GetFocusedSilentInputEligibilityAsync()
        => _silentInput.GetFocusedSilentInputEligibilityAsync();

    public Task<FormSilentInputResult> ApplySilentInputAsync(
        FormSilentInputRequest request,
        CancellationToken cancellationToken)
        => _silentInput.ApplySilentInputAsync(request, cancellationToken);

    internal ImmutableArray<FormAdditionalFieldValueCandidate> ExtractAdditionalFieldCandidates(string transcript)
        => _additionalFieldProposal.Extractor.Extract(transcript, _mutableState.FocusedFieldId);

    internal FormAdditionalFieldProposalPreparation PrepareAdditionalFieldProposal(
        FormAdditionalFieldValueCandidate candidate)
        => _additionalFieldProposal.Policy.Prepare(candidate);

    internal FormAdditionalFieldValueAdoptionResult CommitAdditionalFieldProposal(
        FormAdditionalFieldProposalCommitToken commitToken)
        => _additionalFieldProposal.Policy.Commit(commitToken);

    internal void AbandonAdditionalFieldProposal(FormAdditionalFieldProposalCommitToken commitToken)
        => _additionalFieldProposal.Policy.Abandon(commitToken);
}
