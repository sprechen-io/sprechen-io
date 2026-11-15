using System.Collections.Immutable;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt erforderliche, noch nicht abgeschlossene Felder aus dem gemeinsamen
/// Projektionsbereich aus. Zähler und Navigation können dadurch dieselbe Definition
/// verwenden. Der Bereich folgt den Ersatzregeln des Projektionsauflösers.
/// </summary>
internal sealed class FormOpenRequiredFieldSelector
{
    private readonly FormDialogProjectionScopeResolver projectionScopeResolver;
    private readonly FormFieldCompletionPolicy fieldCompletionService;
    private readonly FormErrorViolationFilter errorViolationFilter;

    public FormOpenRequiredFieldSelector(
        FormDialogProjectionScopeResolver projectionScopeResolver,
        FormFieldCompletionPolicy fieldCompletionService,
        FormErrorViolationFilter errorViolationFilter)
    {
        this.projectionScopeResolver = projectionScopeResolver ?? throw new ArgumentNullException(nameof(projectionScopeResolver));
        this.fieldCompletionService = fieldCompletionService ?? throw new ArgumentNullException(nameof(fieldCompletionService));
        this.errorViolationFilter = errorViolationFilter ?? throw new ArgumentNullException(nameof(errorViolationFilter));
    }

    /// <summary>
    /// Liefert offene Pflichtfelder in der Reihenfolge des Projektionsbereichs.
    /// Navigation mit eigener Dokumentreihenfolge muss die Kennungen entsprechend einordnen.
    /// </summary>
    public ImmutableArray<string> SelectOpenRequiredFieldIds(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<string, bool> isFieldEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var visibleFieldIds = projectionScopeResolver
            .Resolve(isFieldEffectivelyVisible, fallbackFieldGroupId: string.Empty)
            .VisibleFieldIds;
        var blockingFieldIds = errorViolationFilter
            .Filter(violations)
            .Select(violation => violation.FieldId)
            .Where(fieldId => !string.IsNullOrWhiteSpace(fieldId))
            .ToImmutableHashSet(StringComparer.Ordinal);

        return visibleFieldIds
            .Where(fieldId =>
                fieldCompletionService.IsRequiredField(fieldId)
                && !fieldCompletionService.IsFieldCompleted(fieldId, blockingFieldIds, valuesByVariableId))
            .ToImmutableArray();
    }
}
