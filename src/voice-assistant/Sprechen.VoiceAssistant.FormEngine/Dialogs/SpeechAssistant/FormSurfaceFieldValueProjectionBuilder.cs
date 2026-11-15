using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert sichtbare gespeicherte Werte für die Oberfläche nach Datenschutzklasse, damit geschützte Inhalte ausgelassen oder auf Vorhandensein beschränkt bleiben.
/// </summary>
internal sealed class FormSurfaceFieldValueProjectionBuilder
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormFieldVisibilityPolicy _visibility;

    public FormSurfaceFieldValueProjectionBuilder(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormFieldVisibilityPolicy visibility)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _visibility = visibility ?? throw new ArgumentNullException(nameof(visibility));
    }

    public ImmutableDictionary<string, FormSurfaceFieldValue> Build()
    {
        var builder = ImmutableDictionary.CreateBuilder<string, FormSurfaceFieldValue>(StringComparer.Ordinal);
        foreach (var fieldId in _definitionState.OrderedFieldIds)
        {
            if (!_definitionState.FieldToVariableId.TryGetValue(FieldId.From(fieldId), out var variableId)
                || !_visibility.IsFieldEffectivelyVisible(fieldId)
                || !_mutableState.ValuesByVariableId.TryGetValue(variableId, out var value))
            {
                continue;
            }

            var policy = _definitionState.RequireEffectivePolicy(VariableId.From(variableId));
            if (policy.PrivacyClass == PrivacyClasses.Secret)
            {
                continue;
            }

            if (policy.PrivacyClass is PrivacyClasses.Personal or PrivacyClasses.Sensitive)
            {
                builder.Add(fieldId, new FormSurfaceFieldValue(FormSurfaceFieldValueKind.StoredEntryPresence, value: null));
                continue;
            }

            builder.Add(fieldId, new FormSurfaceFieldValue(FormSurfaceFieldValueKind.PublicValue, value));
        }

        return builder.ToImmutable();
    }
}
