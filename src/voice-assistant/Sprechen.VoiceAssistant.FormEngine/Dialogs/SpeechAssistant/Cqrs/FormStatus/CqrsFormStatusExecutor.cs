using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Bündelt Formularwerte, Struktur, Status und Validierung. Für Zusammenfassungen wird die Erlaubnis zum Vorlesen ohne angenommene ausdrückliche Einwilligung geprüft.
/// </summary>
internal sealed class CqrsFormStatusExecutor : ICqrsFormStatusExecutor
{
    private readonly GetValuesSnapshotQueryHandler _getValuesSnapshotHandler;
    private readonly GetDiscoverySnapshotQueryHandler _getDiscoverySnapshotHandler;
    private readonly GetDetailedFormStatusQueryHandler _getDetailedFormStatusHandler;
    private readonly ValidateAllQueryHandler _validateAllHandler;

    private readonly FormStatusProjectionBuilder _statusProjectionBuilder;
    private readonly FormSummaryProjectionBuilder _summaryProjectionBuilder;
    private readonly FormFocusedFieldSnapshotBuilder _focusedFieldSnapshotBuilder;
    private readonly FormFieldVisibilityPolicy _visibilityPolicy;
    private readonly FormPrivacyReadbackPolicy _privacyReadbackPolicy;
    private readonly FormMutableStateStore _mutableState;

    public CqrsFormStatusExecutor(
        FormMutableStateStore mutableState,
        FormStatusProjectionBuilder statusProjectionBuilder,
        FormSummaryProjectionBuilder summaryProjectionBuilder,
        FormDiscoveryProjectionBuilder discoveryProjectionBuilder,
        FormFocusedFieldSnapshotBuilder focusedFieldSnapshotBuilder,
        FormDialogValidator validator,
        FormFieldVisibilityPolicy visibilityPolicy,
        FormPrivacyReadbackPolicy privacyReadbackPolicy)
    {
        ArgumentNullException.ThrowIfNull(mutableState);
        ArgumentNullException.ThrowIfNull(statusProjectionBuilder);
        ArgumentNullException.ThrowIfNull(summaryProjectionBuilder);
        ArgumentNullException.ThrowIfNull(discoveryProjectionBuilder);
        ArgumentNullException.ThrowIfNull(focusedFieldSnapshotBuilder);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(visibilityPolicy);
        ArgumentNullException.ThrowIfNull(privacyReadbackPolicy);

        _mutableState = mutableState;
        _statusProjectionBuilder = statusProjectionBuilder;
        _summaryProjectionBuilder = summaryProjectionBuilder;
        _focusedFieldSnapshotBuilder = focusedFieldSnapshotBuilder;
        _visibilityPolicy = visibilityPolicy;
        _privacyReadbackPolicy = privacyReadbackPolicy;

        _getValuesSnapshotHandler = new GetValuesSnapshotQueryHandler(mutableState, visibilityPolicy);
        _getDiscoverySnapshotHandler = new GetDiscoverySnapshotQueryHandler(discoveryProjectionBuilder, focusedFieldSnapshotBuilder, visibilityPolicy);
        _getDetailedFormStatusHandler = new GetDetailedFormStatusQueryHandler(statusProjectionBuilder, validator, visibilityPolicy, mutableState);
        _validateAllHandler = new ValidateAllQueryHandler(validator, visibilityPolicy);
    }

    public Task<ImmutableDictionary<string, string>> GetValuesSnapshotAsync()
        => _getValuesSnapshotHandler.HandleAsync(new GetValuesSnapshotQuery());

    public Task<FormDiscoverySnapshot> GetDiscoverySnapshotAsync()
        => _getDiscoverySnapshotHandler.HandleAsync(new GetDiscoverySnapshotQuery());

    public Task<FormDetailedStatusSnapshot> GetDetailedStatusSnapshotAsync()
        => _getDetailedFormStatusHandler.HandleAsync(new GetDetailedFormStatusQuery());

    public Task<ImmutableArray<FormConstraintViolation>> ValidateAllAsync()
        => _validateAllHandler.HandleAsync(new ValidateAllQuery());

    public async Task<FormStatusSnapshot> GetStatusSnapshotAsync()
    {
        var field = _focusedFieldSnapshotBuilder.Build();
        var violations = await ValidateAllAsync().ConfigureAwait(false);
        return _statusProjectionBuilder.BuildStatusSnapshot(
            field,
            _mutableState.ValuesByVariableId,
            violations,
            _visibilityPolicy.IsFieldEffectivelyVisible);
    }

    public Task<ImmutableArray<FormSummaryItem>> BuildSummaryItemsAsync()
        => Task.FromResult(_summaryProjectionBuilder.BuildSummaryItems(
            _mutableState.ValuesByVariableId,
            variableId => _privacyReadbackPolicy.CanSpeakValueInConfirmation(variableId, explicitOptIn: false)));
}
