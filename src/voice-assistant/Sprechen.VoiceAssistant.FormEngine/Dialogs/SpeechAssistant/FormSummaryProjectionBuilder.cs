using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert gespeicherte Angaben in Formularreihenfolge für eine Zusammenfassung. Die Projektion erteilt selbst keine Sichtbarkeits- oder Vorlesefreigabe.
/// </summary>
internal sealed class FormSummaryProjectionBuilder
{
    private readonly ImmutableArray<string> _orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> _fieldsById;
    private readonly ImmutableDictionary<FieldId, string> _fieldToVariableId;
    private readonly ImmutableDictionary<VariableId, VariableDefinition> _variablesById;
    private readonly FormLocalizationResolver _localizationResolver;
    private readonly FormSpokenValueProjector _spokenValueProjector;

    public FormSummaryProjectionBuilder(
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableDictionary<FieldId, string> fieldToVariableId,
        ImmutableDictionary<VariableId, VariableDefinition> variablesById,
        FormLocalizationResolver localizationResolver,
        string requestedIsoCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedIsoCode);

        _orderedFieldIds = orderedFieldIds;
        _fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
        _fieldToVariableId = fieldToVariableId ?? throw new ArgumentNullException(nameof(fieldToVariableId));
        _variablesById = variablesById ?? throw new ArgumentNullException(nameof(variablesById));
        _localizationResolver = localizationResolver ?? throw new ArgumentNullException(nameof(localizationResolver));
        _spokenValueProjector = new FormSpokenValueProjector(requestedIsoCode);
    }

    public ImmutableArray<FormSummaryItem> BuildSummaryItems(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        Func<string, bool> shouldSpeakValueInConfirmation)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(shouldSpeakValueInConfirmation);

        var items = new List<FormSummaryItem>();

        foreach (var fieldId in _orderedFieldIds)
        {
            if (!TryCreateSummaryItem(fieldId, valuesByVariableId, shouldSpeakValueInConfirmation, out var item))
            {
                continue;
            }

            items.Add(item);
        }

        return items.ToImmutableArray();
    }

    private bool TryCreateSummaryItem(
        string fieldId,
        IReadOnlyDictionary<string, string> valuesByVariableId,
        Func<string, bool> shouldSpeakValueInConfirmation,
        out FormSummaryItem item)
    {
        item = default!;
        if (!_fieldsById.TryGetValue(FieldId.From(fieldId), out var field)
            || !_fieldToVariableId.TryGetValue(FieldId.From(fieldId), out var variableId)
            || !valuesByVariableId.TryGetValue(variableId, out var rawValue))
        {
            return false;
        }

        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_variablesById, variableId)
            ?? ValueTypeKinds.String;
        var isSet = string.Equals(valueTypeKind, ValueTypeKinds.Set, StringComparison.OrdinalIgnoreCase);
        var isEmpty = string.IsNullOrWhiteSpace(rawValue);
        if (isEmpty && !isSet)
        {
            // Leere Nicht-Mengenwerte auslassen. Eine gespeicherte leere Menge bleibt als
            // Wert darstellbar.
            return false;
        }

        var label = _localizationResolver.ResolveText(field.Label, fieldId);
        var disposition = shouldSpeakValueInConfirmation(variableId)
            ? FormSpokenValueSpeakDisposition.SpeakFull
            : FormSpokenValueSpeakDisposition.Withhold;
        var projection = _spokenValueProjector.Project(
            isEmpty ? string.Empty : rawValue.Trim(),
            valueTypeKind,
            disposition,
            _localizationResolver.ResolveChoiceOptions(variableId),
            maskedWording: null);

        item = new FormSummaryItem(fieldId, label, projection.SpokenValue, projection.Status);
        return true;
    }
}
