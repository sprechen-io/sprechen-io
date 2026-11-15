using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft und übernimmt stille Eingabe im angegebenen Feldkontext, führt Regeln aus und behandelt Validierungsverstöße über die vorgesehene Rücknahme des Zielwerts.
/// </summary>
internal sealed class FormSilentInputApplicator : IFormDialogSilentInput
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormDialogDependencies _services;
    private readonly IFormDialogSurfaceState _surfaceState;

    public FormSilentInputApplicator(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormDialogDependencies services,
        IFormDialogSurfaceState surfaceState)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _surfaceState = surfaceState ?? throw new ArgumentNullException(nameof(surfaceState));
    }

    public Task<FormSilentInputEligibilityProjection?> GetFocusedSilentInputEligibilityAsync()
        => Task.FromResult(ProjectFocusedEligibility());

    public async Task<FormSilentInputResult> ApplySilentInputAsync(
        FormSilentInputRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (!MatchesRequestedContext(request) || !MatchesFocusedEligibility(request, out var variableId))
        {
            return FormSilentInputResult.StateChanged(
                await ObserveAsync(request).ConfigureAwait(false),
                ProjectFocusedEligibility());
        }

        var previous = _mutableState.GetVariableValue(variableId);
        if (request.IntentKind == FormSilentInputIntentKind.Clear)
        {
            _mutableState.ClearVariableValue(variableId);
        }
        else
        {
            _mutableState.SetVariableValue(variableId, request.Value!.Trim());
        }

        _ = _services.RuleOrchestrationService.TryApplyRules(RuleScopes.OnValueChanged, out _);
        _services.Navigator.EnsureFocusIsValid();

        var violations = _services.ValidationService.ValidateField(request.FieldId);
        if (violations.Length > 0)
        {
            Restore(variableId, previous);
            return FormSilentInputResult.Rejected(MapRepair(violations, request.ControlKind));
        }

        var values = _mutableState.ValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);
        var session = new SpeechAssistantApplicationSessionState(
            ImmutableDictionary.Create<string, SpeechAssistantFormSessionState>(StringComparer.OrdinalIgnoreCase)
                .Add(request.FormId, new SpeechAssistantFormSessionState(values, IsSubmitted: false)));

        var nextValue = _mutableState.GetVariableValue(variableId);
        var canonicalSame = string.Equals(previous, nextValue, StringComparison.Ordinal);

        return FormSilentInputResult.Applied(
            session,
            canonicalSame,
            await ObserveAsync(request).ConfigureAwait(false),
            ProjectFocusedEligibility());
    }

    private bool MatchesRequestedContext(FormSilentInputRequest request)
    {
        if (!string.Equals(_definitionState.RequestedIsoCode, request.LanguageCode, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(_definitionState.Contract.ApplicationId, request.ApplicationId, StringComparison.Ordinal))
        {
            return false;
        }

        return string.Equals(_definitionState.Contract.FormId.Value, request.FormId, StringComparison.Ordinal);
    }

    private bool MatchesFocusedEligibility(FormSilentInputRequest request, out string variableId)
    {
        variableId = string.Empty;
        if (_mutableState.FocusedFieldId is not { } focusedFieldId
            || !string.Equals(focusedFieldId.Value, request.FieldId, StringComparison.Ordinal))
        {
            return false;
        }

        var (field, variable) = _definitionState.ResolveFieldVariable(focusedFieldId);
        var eligibility = new FormSilentInputEligibilityProjectionBuilder(
                _definitionState,
                _services.InputModeAvailabilityPolicy)
            .TryProject(field, variable);
        if (eligibility is null || eligibility.ControlKind != request.ControlKind)
        {
            return false;
        }

        variableId = variable.VariableId.Value;
        return true;
    }

    internal FormSilentInputEligibilityProjection? ProjectFocusedEligibility()
    {
        if (_mutableState.FocusedFieldId is not { } focusedFieldId)
        {
            return null;
        }

        var (field, variable) = _definitionState.ResolveFieldVariable(focusedFieldId);
        return new FormSilentInputEligibilityProjectionBuilder(
                _definitionState,
                _services.InputModeAvailabilityPolicy)
            .TryProject(field, variable);
    }

    private void Restore(string variableId, string? previous)
    {
        if (previous is null)
        {
            _mutableState.ClearVariableValue(variableId);
        }
        else
        {
            _mutableState.SetVariableValue(variableId, previous);
        }

        _ = _services.RuleOrchestrationService.TryApplyRules(RuleScopes.OnValueChanged, out _);
        _services.Navigator.EnsureFocusIsValid();
    }

    private FormSilentInputRepairKind MapRepair(
        ImmutableArray<FormConstraintViolation> violations,
        SpeechAssistantSilentInputControlKind controlKind)
    {
        var violation = violations[0];
        var constraint = _definitionState.Constraints.FirstOrDefault(
            candidate => string.Equals(candidate.ConstraintId, violation.ConstraintId, StringComparison.Ordinal));

        return constraint?.Kind switch
        {
            ConstraintKind.Required => FormSilentInputRepairKind.Required,
            ConstraintKind.MinLength or ConstraintKind.MaxLength => FormSilentInputRepairKind.Length,
            ConstraintKind.Pattern when controlKind == SpeechAssistantSilentInputControlKind.Email
                => FormSilentInputRepairKind.EmailFormat,
            ConstraintKind.Pattern when controlKind == SpeechAssistantSilentInputControlKind.Telephone
                => FormSilentInputRepairKind.TelephoneFormat,
            _ => FormSilentInputRepairKind.Format
        };
    }

    private async Task<FormSurfaceSnapshot> ObserveAsync(FormSilentInputRequest request)
    {
        var fieldValuesById = await _surfaceState.GetSurfaceFieldValuesAsync().ConfigureAwait(false);
        var focusedFieldId = await _surfaceState.GetFocusedFieldIdAsync().ConfigureAwait(false);
        var detailedStatus = await _surfaceState.GetDetailedStatusSnapshotAsync().ConfigureAwait(false);
        var formDisplayName = _definitionState.LocalizationResolver.ResolveText(
            _definitionState.Contract.DisplayName,
            _definitionState.Contract.FormId.Value);

        var fieldStatusesById = detailedStatus.Fields.ToImmutableDictionary(
            field => field.FieldId,
            field => new FormSurfaceFieldStatus(
                field.FieldId,
                field.FieldGroupId,
                field.Relevant,
                field.Required,
                field.Completed,
                field.BlockingErrorCodes),
            StringComparer.Ordinal);

        var fieldGroupStatusesById = detailedStatus.FieldGroups.ToImmutableDictionary(
            fieldGroup => fieldGroup.FieldGroupId,
            fieldGroup => new FormSurfaceFieldGroupStatus(
                fieldGroup.FieldGroupId,
                fieldGroup.Relevant,
                fieldGroup.Completed,
                fieldGroup.OpenGroupErrorCodes),
            StringComparer.Ordinal);

        return new FormSurfaceSnapshot(
            FormSurfaceStage.Active,
            request.LanguageCode,
            request.ApplicationId,
            applicationDisplayName: string.Empty,
            request.FormId,
            formDisplayName,
            new FormSurfaceDefinition(),
            fieldValuesById,
            focusedFieldId,
            fieldStatusesById,
            fieldGroupStatusesById);
    }
}
