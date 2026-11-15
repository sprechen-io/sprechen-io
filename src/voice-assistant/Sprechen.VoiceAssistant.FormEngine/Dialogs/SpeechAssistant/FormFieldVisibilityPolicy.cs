using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt wirksame Sichtbarkeit sowie Eingabe- und Vorleseverfügbarkeit anhand von Zustand und Gruppenpfad für Navigation und Projektion.
/// </summary>
internal sealed class FormFieldVisibilityPolicy
{
    private readonly IReadOnlyDictionary<FieldGroupId, bool> _fieldGroupVisibleById;
    private readonly IReadOnlyDictionary<FieldId, bool> _fieldVisibleById;
    private readonly IReadOnlyDictionary<FieldId, bool> _fieldEnabledById;
    private readonly IReadOnlyDictionary<FieldId, bool> _fieldReadOnlyById;
    private readonly ImmutableDictionary<FieldGroupId, string?> _parentFieldGroupById;
    private readonly ImmutableDictionary<FieldId, string> _fieldToFieldGroupId;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> _fieldsById;

    public FormFieldVisibilityPolicy(
        IReadOnlyDictionary<FieldGroupId, bool> fieldGroupVisibleById,
        IReadOnlyDictionary<FieldId, bool> fieldVisibleById,
        IReadOnlyDictionary<FieldId, bool> fieldEnabledById,
        IReadOnlyDictionary<FieldId, bool> fieldReadOnlyById,
        ImmutableDictionary<FieldGroupId, string?> parentFieldGroupById,
        ImmutableDictionary<FieldId, string> fieldToFieldGroupId,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById)
    {
        _fieldGroupVisibleById = fieldGroupVisibleById ?? throw new ArgumentNullException(nameof(fieldGroupVisibleById));
        _fieldVisibleById = fieldVisibleById ?? throw new ArgumentNullException(nameof(fieldVisibleById));
        _fieldEnabledById = fieldEnabledById ?? throw new ArgumentNullException(nameof(fieldEnabledById));
        _fieldReadOnlyById = fieldReadOnlyById ?? throw new ArgumentNullException(nameof(fieldReadOnlyById));
        _parentFieldGroupById = parentFieldGroupById ?? throw new ArgumentNullException(nameof(parentFieldGroupById));
        _fieldToFieldGroupId = fieldToFieldGroupId ?? throw new ArgumentNullException(nameof(fieldToFieldGroupId));
        _fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
    }

    /// <summary>
    /// Prüft die aktuelle Eingabeverfügbarkeit aus wirksamer Sichtbarkeit, Aktivierung,
    /// Laufzeit-Schreibschutz und deklarierter Bearbeitbarkeit. Unbekannte Bearbeitbarkeit
    /// und <see cref="FieldEditability.UserEditable"/> sind dafür zugelassen.
    /// </summary>
    public bool IsFieldInputAvailable(string fieldId)
    {
        if (!IsFieldEffectivelyVisible(fieldId))
        {
            return false;
        }

        var typedFieldId = FieldId.From(fieldId);

        if (_fieldEnabledById.TryGetValue(typedFieldId, out var enabled) && !enabled)
        {
            return false;
        }

        if (_fieldReadOnlyById.TryGetValue(typedFieldId, out var readOnly) && readOnly)
        {
            return false;
        }

        if (_fieldsById.TryGetValue(typedFieldId, out var field))
        {
            return field.Editability is FieldEditability.Unknown or FieldEditability.UserEditable;
        }

        return false;
    }

    /// <summary>
    /// Prüft die Verfügbarkeit für Ansage oder Abfrage: Eingabeverfügbarkeit genügt.
    /// Zusätzlich sind wirksam sichtbare Felder mit <see cref="FieldEditability.ReadOnly"/>
    /// zugelassen. Berechnete Felder erhalten durch diesen Zusatz keine Freigabe.
    /// </summary>
    public bool IsFieldAudiblyAvailable(string fieldId)
    {
        if (IsFieldInputAvailable(fieldId))
        {
            return true;
        }

        if (!IsFieldEffectivelyVisible(fieldId))
        {
            return false;
        }

        return _fieldsById.TryGetValue(FieldId.From(fieldId), out var field)
            && field.Editability is FieldEditability.ReadOnly;
    }

    public bool IsVariableEffectivelyVisible(string variableId)
    {
        var referencingFields = _fieldsById.Values
            .Where(field => string.Equals(field.VariableId, variableId, StringComparison.Ordinal))
            .ToList();

        if (referencingFields.Count == 0)
        {
            return true;
        }

        return referencingFields.Any(field => IsFieldEffectivelyVisible(field.FieldId));
    }

    public bool IsFieldEffectivelyVisible(string fieldId)
    {
        var typedFieldId = FieldId.From(fieldId);

        if (_fieldVisibleById.TryGetValue(typedFieldId, out var visible) && !visible)
        {
            return false;
        }

        if (!_fieldToFieldGroupId.TryGetValue(typedFieldId, out var fieldGroupId))
        {
            return false;
        }

        return IsFieldGroupVisible(fieldGroupId);
    }

    public bool IsFieldGroupVisible(string fieldGroupId)
    {
        var current = fieldGroupId;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (visited.Add(current))
        {
            if (_fieldGroupVisibleById.TryGetValue(FieldGroupId.From(current), out var visible) && !visible)
            {
                return false;
            }

            if (!_parentFieldGroupById.TryGetValue(FieldGroupId.From(current), out var parent) || parent is null)
            {
                return true;
            }

            current = parent;
        }

        // Bei einem Zyklus die Vorfahrenprüfung beenden. Ohne zuvor gefundenen
        // verborgenen Vorfahren bleibt das Feld auf diesem Prüfpfad sichtbar.
        return true;
    }

    public bool IsFieldGroupOnPath(string fieldGroupId, string ancestorFieldGroupId)
    {
        var current = fieldGroupId;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (visited.Add(current))
        {
            if (string.Equals(current, ancestorFieldGroupId, StringComparison.Ordinal))
            {
                return true;
            }

            if (!_parentFieldGroupById.TryGetValue(FieldGroupId.From(current), out var parent) || parent is null)
            {
                return false;
            }

            current = parent;
        }

        // Bei einem Zyklus die Suche abbrechen. Der gesuchte Vorfahr wurde nicht gefunden.
        return false;
    }
}


