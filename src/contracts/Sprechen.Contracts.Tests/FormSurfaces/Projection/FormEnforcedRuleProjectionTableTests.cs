using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Contracts.Tests.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Contracts.Tests.FormSurfaces.Projection;

/// <summary>
/// Prüft abgeleitete Formularattribute gegen gemeinsame Testfälle, damit nur vorgesehene Regeln die Eingabe im Browser beschränken.
/// </summary>
public sealed class FormEnforcedRuleProjectionTableTests
{
    private readonly FormSurfaceProjectionTestContractFactory _factory = new();

    [Fact]
    public void Page_CarriesExactlyTheRulesTheTableSaysItCarries()
    {
        var rows = ContractRuleEvaluationTable.EnforcedRuleProjectionCases();
        Assert.NotEmpty(rows);

        // Alle Abweichungen sammeln, damit ein Fehler nicht die übrigen Fälle verdeckt.
        var disagreements = new List<string>();
        foreach (var row in rows)
        {
            var carried = ProjectOneField(row);
            var expected = ExpectedSides(row);

            foreach (var side in CarriedSides)
            {
                var actualValue = CarriedSide(carried, side);
                var expectedValue = expected.TryGetValue(side, out var value) ? value : null;
                if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
                {
                    disagreements.Add(
                        $"'{row.GetProperty("case").GetString()}': the table says the page carries {Describe(side, expectedValue)}, this channel carries {Describe(side, actualValue)}.");
                }
            }
        }

        Assert.True(disagreements.Count == 0, string.Join("\n", disagreements));
    }

    [Fact]
    public void Table_StatesRulesAnAuthoringOrderReaderAndASeverityBlindReaderWouldGetWrong()
    {
        var rows = ContractRuleEvaluationTable.EnforcedRuleProjectionCases();

        // Ein eigener Fall für eine nicht blockierende Regel prüft, ob der Schweregrad berücksichtigt wird.
        Assert.Contains(rows, row => StatedRules(row).Any(rule =>
            rule.GetProperty("severity").GetString() != "error"
            && !ExpectedSides(row).ContainsKey(SideOf(rule))));

        // Umgekehrte Reihenfolgen derselben Regeln prüfen, ob das Ergebnis von der Eingabereihenfolge abhängt.
        var authoredPairs = rows
            .SelectMany(row => CarriedSides
                .Select(side => (Side: side, Ids: BlockingRuleIdsOfSide(row, side))))
            .Where(entry => entry.Ids.Count == 2)
            .ToList();

        Assert.True(
            authoredPairs.Any(one => authoredPairs.Any(other =>
                other.Side == one.Side && other.Ids[0] == one.Ids[1] && other.Ids[1] == one.Ids[0])),
            "No side is authored with the same two blocking rules in both orders, so a channel that simply kept the first or the last rule it read would stay green.");

        // Gleiche Grenzwerte in umgekehrter Reihenfolge ergänzen die Prüfung um Gleichstände.
        var equalBoundPairs = rows
            .SelectMany(row => new[] { "min", "max" }.Select(side => (Side: side, Ids: BlockingRuleIdsOfSide(row, side), Bounds: BlockingBoundsOfSide(row, side))))
            .Where(entry => entry.Ids.Count == 2 && entry.Bounds.Distinct(StringComparer.Ordinal).Count() == 1)
            .ToList();

        Assert.True(
            equalBoundPairs.Any(one => equalBoundPairs.Any(other =>
                other.Side == one.Side && other.Ids[0] == one.Ids[1] && other.Ids[1] == one.Ids[0])),
            "No magnitude side is authored with two blocking rules stating the same bound in both orders, so a channel that kept whichever of them it read first would stay green.");
    }

    [Fact]
    public void Table_CoversEverySideAnEnforcingSurfaceCarries()
    {
        // Jedes erwartete Formularattribut muss in mindestens einem Testfall vorkommen.
        var rows = ContractRuleEvaluationTable.EnforcedRuleProjectionCases();
        var carriedSides = rows
            .SelectMany(row => row.GetProperty("expectedCarried").EnumerateArray())
            .Select(carried => carried.GetProperty("side").GetString() ?? string.Empty)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(CarriedSides.OrderBy(side => side, StringComparer.Ordinal), carriedSides.OrderBy(side => side, StringComparer.Ordinal));
    }

    private static List<string> BlockingBoundsOfSide(JsonElement row, string side)
        => StatedRules(row)
            .Where(rule => rule.GetProperty("severity").GetString() == SeverityLevels.Error && SideOf(rule) == side)
            .Select(rule => rule.TryGetProperty(side, out var bound) ? bound.ToString() : string.Empty)
            .ToList();

    // Grenzwerte werden als Text verglichen, damit auch Kalenderdaten unverändert geprüft werden.
    private static readonly string[] CarriedSides = ["required", "pattern", "minLength", "maxLength", "min", "max"];

    private static List<string?> BlockingRuleIdsOfSide(JsonElement row, string side)
        => StatedRules(row)
            .Where(rule => rule.GetProperty("severity").GetString() == SeverityLevels.Error && SideOf(rule) == side)
            .Select(rule => rule.GetProperty("constraintId").GetString())
            .ToList();

    private static string Describe(string side, string? value)
        => value is null ? $"no {side} at all" : $"{side} '{value}'";

    private static IEnumerable<JsonElement> StatedRules(JsonElement row)
        => row.GetProperty("statedRules").EnumerateArray();

    private static string SideOf(JsonElement statedRule)
    {
        var ruleKind = statedRule.GetProperty("ruleKind").GetString() ?? string.Empty;
        return ruleKind == "oneOf" ? "oneOf" : ruleKind;
    }

    // Erwartete Attributwerte werden für den Vergleich einheitlich als Text dargestellt.
    private static Dictionary<string, string> ExpectedSides(JsonElement row)
    {
        var sides = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var carried in row.GetProperty("expectedCarried").EnumerateArray())
        {
            var side = carried.GetProperty("side").GetString() ?? string.Empty;
            sides[side] = side switch
            {
                "required" => "true",
                "pattern" => carried.GetProperty("pattern").GetString() ?? string.Empty,
                "min" or "max" => carried.GetProperty("boundText").GetString() ?? string.Empty,
                _ => carried.GetProperty("bound").GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        return sides;
    }

    private static string? CarriedSide(FieldConstraints carried, string side) => side switch
    {
        "required" => carried.Required ? "true" : null,
        "pattern" => carried.Pattern,
        "minLength" => carried.MinLength?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "maxLength" => carried.MaxLength?.ToString(System.Globalization.CultureInfo.InvariantCulture),
        "min" => carried.Min,
        _ => carried.Max
    };

    private FieldConstraints ProjectOneField(JsonElement row)
    {
        var contract = _factory.CreateContractShell();
        var field = _factory.CreateField("target", "targetVar", "Ziel");
        contract.Constraints = StatedRules(row).Select(BuildConstraint).ToList();
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["target"] = field
        };

        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
        {
            ["targetVar"] = _factory.CreateVariable("targetVar", """{"kind":"string"}""")
        };

        var projected = new FormConstraintProjector().ProjectByFieldId(contract, fieldsById, variablesById);
        return projected.TryGetValue("target", out var carried) ? carried : FieldConstraints.Empty;
    }

    private ConstraintDefinition BuildConstraint(JsonElement statedRule)
    {
        var ruleKind = statedRule.GetProperty("ruleKind").GetString() ?? string.Empty;
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        if (statedRule.TryGetProperty("bound", out var bound))
        {
            properties[ruleKind] = bound.Clone();
        }

        // Bereichsregeln können zwei getrennt benannte Grenzen enthalten; beide werden übernommen.
        foreach (var boundName in new[]
        {
            ConstraintPropertyNames.Min,
            ConstraintPropertyNames.Max,
            ConstraintPropertyNames.MinDate,
            ConstraintPropertyNames.MaxDate
        })
        {
            if (statedRule.TryGetProperty(boundName, out var statedBound))
            {
                properties[boundName] = statedBound.Clone();
            }
        }

        if (statedRule.TryGetProperty("pattern", out var pattern))
        {
            properties[ConstraintPropertyNames.Pattern] = pattern.Clone();
        }

        if (statedRule.TryGetProperty("values", out var values))
        {
            properties[ConstraintPropertyNames.Values] = values.Clone();
        }

        var constraint = _factory.CreateConstraint(
            statedRule.GetProperty("constraintId").GetString() ?? string.Empty,
            "target",
            ruleKind,
            properties);

        constraint.Severity = statedRule.GetProperty("severity").GetString() switch
        {
            SeverityLevels.Error => ConstraintSeverity.Error,
            SeverityLevels.Warning => ConstraintSeverity.Warning,
            SeverityLevels.Info => ConstraintSeverity.Info,
            _ => ConstraintSeverity.Unknown
        };

        return constraint;
    }
}
