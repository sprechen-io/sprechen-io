using System.Collections.Immutable;
using System.Text.Json;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Leitet Validierungsregeln aus minLength/maxLength von Zeichenketten und min/max
/// von Ganzzahlen ab. Prüfung und Oberflächenprojektion können so dieselben Regeln
/// und Fehlerkennungen für Grenzen aus dem Variablentyp verwenden.
/// </summary>
public static class FormValueTypeStatedRules
{
    /// <summary>
    /// Gemeinsamer Fehlercode für aus dem Variablentyp abgeleitete Regeln.
    /// Ein einheitlicher Code verhindert kanalabhängige Kennungen desselben Fehlers.
    /// Die erzeugten Regeln haben den Schweregrad Error.
    /// </summary>
    public const string StatedRuleCode = "form.valueType.invalid";

    private static readonly ImmutableArray<(string BoundName, string RuleKind)> StringBounds =
    [
        (ValueTypePropertyNames.MinLength, ConstraintKinds.MinLength),
        (ValueTypePropertyNames.MaxLength, ConstraintKinds.MaxLength)
    ];

    private static readonly ImmutableArray<(string BoundName, string RuleKind)> IntegerBounds =
    [
        (ValueTypePropertyNames.Min, ConstraintKinds.Min),
        (ValueTypePropertyNames.Max, ConstraintKinds.Max)
    ];

    public static string StatedRuleId(string boundName) => $"valueType.{boundName}";

    public static IReadOnlyList<ConstraintDefinition> From(VariableDefinition variable)
    {
        ArgumentNullException.ThrowIfNull(variable);

        if (variable.ValueType.ValueKind != JsonValueKind.Object
            || !variable.ValueType.TryGetProperty(ValueTypePropertyNames.Kind, out var kindElement)
            || kindElement.ValueKind != JsonValueKind.String)
        {
            return [];
        }

        var statedBounds = (kindElement.GetString() ?? string.Empty).Trim() switch
        {
            ValueTypeKinds.String => StringBounds,
            ValueTypeKinds.Integer => IntegerBounds,
            _ => ImmutableArray<(string BoundName, string RuleKind)>.Empty
        };

        var rules = new List<ConstraintDefinition>();
        foreach (var (boundName, ruleKind) in statedBounds)
        {
            // Vorhandene Grenzen werden unverändert übernommen, auch bei ungültiger Notation.
            // Die Ableitung darf keine Regel still entfernen. Ungültige Angaben müssen
            // bei der anschließenden Verarbeitung erkennbar bleiben.
            if (!variable.ValueType.TryGetProperty(boundName, out var bound))
            {
                continue;
            }

            rules.Add(new ConstraintDefinition
            {
                ConstraintId = StatedRuleId(boundName),
                Target = new ConstraintTargetRef
                {
                    Kind = TargetKinds.Variable,
                    VariableId = variable.VariableId
                },
                Severity = ConstraintSeverity.Error,
                Code = StatedRuleCode,
                Message = new LocalizedText(),
                Kind = ConstraintKindExtensions.TryFromWireToken(ruleKind, out var parsedKind)
                    ? parsedKind
                    : throw new InvalidOperationException(
                        $"Value type bound '{boundName}' states a rule kind the constraint vocabulary does not know: '{ruleKind}'."),
                Scope = ConstraintScope.Field,
                AdditionalProperties = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                {
                    [boundName] = bound.Clone()
                }
            });
        }

        return rules;
    }
}
