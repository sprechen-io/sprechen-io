using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt eine Übersicht relevanter Feldgruppen und der Felder der aktuellen Gruppe
/// mit lokalisierten Bezeichnungen, Positionen und Markierung des aktuellen Fokus.
/// </summary>
internal sealed class FormDiscoveryProjectionBuilder
{
    private readonly ImmutableDictionary<FieldId, FieldDefinition> _fieldsById;
    private readonly ImmutableDictionary<FieldId, string> _fieldToFieldGroupId;
    private readonly FormLocalizationResolver _localizationResolver;
    private readonly FormDialogProjectionScopeResolver _projectionScopeResolver;

    public FormDiscoveryProjectionBuilder(
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        FormLocalizationResolver localizationResolver,
        FormDialogProjectionScopeResolver projectionScopeResolver)
    {
        _fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
        _fieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
        _localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
        _projectionScopeResolver = projectionScopeResolver ?? throw new ArgumentNullException(nameof(projectionScopeResolver));
    }

    public FormDiscoverySnapshot BuildDiscoverySnapshot(
        FormFieldSnapshot focusedField,
        Func<string, bool> isFieldEffectivelyVisible)
    {
        ArgumentNullException.ThrowIfNull(focusedField);
        ArgumentNullException.ThrowIfNull(isFieldEffectivelyVisible);

        var scope = _projectionScopeResolver.Resolve(isFieldEffectivelyVisible, focusedField.FieldGroupId);

        var fieldGroups = scope.RelevantFieldGroupIds
            .Select((fieldGroupId, index) => new FormFieldGroupDiscoveryItem(
                fieldGroupId,
                ResolveFieldGroupTitle(fieldGroupId),
                index + 1,
                string.Equals(fieldGroupId, focusedField.FieldGroupId, StringComparison.Ordinal)))
            .ToImmutableArray();

        var currentFieldGroupFields = scope.VisibleFieldIds
            .Where(fieldId =>
                _fieldToFieldGroupId.TryGetValue(FieldId.From(fieldId), out var fieldGroupId)
                && string.Equals(fieldGroupId, focusedField.FieldGroupId, StringComparison.Ordinal))
            .Select((fieldId, index) => new FormFieldDiscoveryItem(
                fieldId,
                focusedField.FieldGroupId,
                ResolveFieldLabel(fieldId),
                index + 1,
                string.Equals(fieldId, focusedField.FieldId, StringComparison.Ordinal)))
            .ToImmutableArray();

        var currentFieldGroupTitle = string.IsNullOrWhiteSpace(focusedField.FieldGroupTitle)
            ? ResolveFieldGroupTitle(focusedField.FieldGroupId)
            : focusedField.FieldGroupTitle;

        return new FormDiscoverySnapshot(
            focusedField.FieldGroupId,
            currentFieldGroupTitle,
            fieldGroups,
            currentFieldGroupFields);
    }

    private string ResolveFieldGroupTitle(string fieldGroupId)
    {
        var resolved = _localizationResolver.ResolveFieldGroupTitle(fieldGroupId);
        return string.IsNullOrWhiteSpace(resolved) ? fieldGroupId : resolved;
    }

    private string ResolveFieldLabel(string fieldId)
    {
        if (!_fieldsById.TryGetValue(FieldId.From(fieldId), out var field))
        {
            return fieldId;
        }

        var resolved = _localizationResolver.ResolveText(field.Label, fieldId);
        return string.IsNullOrWhiteSpace(resolved) ? fieldId : resolved;
    }
}
