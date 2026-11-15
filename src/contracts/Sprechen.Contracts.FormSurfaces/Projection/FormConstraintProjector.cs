using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Sammelt anwendbare Validierungsregeln je Feld, damit die Formularoberfläche nur im Vertrag festgelegte Anforderungen übernimmt.
/// </summary>
internal sealed class FormConstraintProjector
{
    private readonly FormFieldConstraintApplicability _applicability;
    private readonly FormSurfaceCarriedRules _carriedRules;

    public FormConstraintProjector()
        : this(
            new FormFieldConstraintApplicability(),
            new FormSurfaceCarriedRules(new FormConstraintPropertyReader(), new FormStatedBoundReader()))
    {
    }

    internal FormConstraintProjector(
        FormFieldConstraintApplicability applicability,
        FormSurfaceCarriedRules carriedRules)
    {
        _applicability = applicability ?? throw new ArgumentNullException(nameof(applicability));
        _carriedRules = carriedRules ?? throw new ArgumentNullException(nameof(carriedRules));
    }

    public IReadOnlyDictionary<string, FieldConstraints> ProjectByFieldId(
        FormContract contract,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        IReadOnlyDictionary<string, VariableDefinition> variablesById)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(fieldsById);
        ArgumentNullException.ThrowIfNull(variablesById);

        var statedRulesByFieldId = new Dictionary<string, List<ConstraintDefinition>>(StringComparer.Ordinal);
        CollectApplicable(statedRulesByFieldId, contract.Constraints);

        foreach (var (fieldId, field) in fieldsById)
        {
            CollectApplicable(statedRulesByFieldId, field?.FieldConstraints);
            CollectValueTypeStated(statedRulesByFieldId, fieldId, field, variablesById);
        }

        var result = new Dictionary<string, FieldConstraints>(StringComparer.Ordinal);
        foreach (var (fieldId, statedRules) in statedRulesByFieldId)
        {
            // Bei nicht kombinierbaren Regeln entscheidet die kleinste Regelkennung.
            // Die feste Reihenfolge sorgt dafür, dass Projektion und Fehlerzuordnung
            // dieselbe Regel bevorzugen.
            statedRules.Sort(static (left, right) => string.CompareOrdinal(left.ConstraintId, right.ConstraintId));
            result[fieldId] = _carriedRules.From(statedRules);
        }

        return result;
    }

    private void CollectApplicable(
        Dictionary<string, List<ConstraintDefinition>> statedRulesByFieldId,
        IEnumerable<ConstraintDefinition?>? constraints)
    {
        if (constraints is null)
        {
            return;
        }

        foreach (var constraint in constraints)
        {
            if (_applicability.TryResolveFieldId(constraint, out var fieldId))
            {
                RulesOf(statedRulesByFieldId, fieldId).Add(constraint!);
            }
        }
    }

    // Regeln aus dem Werttyp werden wie ausdrückliche Feldregeln gefiltert,
    // damit die Oberfläche nur anwendbare Validierungsregeln übernimmt.
    private void CollectValueTypeStated(
        Dictionary<string, List<ConstraintDefinition>> statedRulesByFieldId,
        string fieldId,
        FieldDefinition? field,
        IReadOnlyDictionary<string, VariableDefinition> variablesById)
    {
        if (field is null
            || field.VariableId == default
            || !variablesById.TryGetValue(field.VariableId.Value, out var variable))
        {
            return;
        }

        foreach (var stated in FormValueTypeStatedRules.From(variable))
        {
            if (_applicability.ReachesSurface(stated))
            {
                RulesOf(statedRulesByFieldId, fieldId).Add(stated);
            }
        }
    }

    private static List<ConstraintDefinition> RulesOf(
        Dictionary<string, List<ConstraintDefinition>> statedRulesByFieldId,
        string fieldId)
    {
        if (!statedRulesByFieldId.TryGetValue(fieldId, out var rules))
        {
            rules = [];
            statedRulesByFieldId[fieldId] = rules;
        }

        return rules;
    }
}
