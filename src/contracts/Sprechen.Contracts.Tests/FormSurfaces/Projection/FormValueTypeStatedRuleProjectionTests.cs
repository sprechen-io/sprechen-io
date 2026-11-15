using System.Text.Json;
using Sprechen.Contracts.FormSurfaces;
using Sprechen.Contracts.Tests.FormSurfaces;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Contracts.Tests.FormSurfaces.Projection;

/// <summary>
/// Prüft die Projektion von Werttypregeln und ausdrücklichen Feldregeln, damit die Oberfläche beide Quellen gleich behandelt.
/// </summary>
public sealed class FormValueTypeStatedRuleProjectionTests
{
    private readonly FormSurfaceProjectionTestContractFactory _factory = new();

    [Fact]
    public void Page_CarriesTheBoundsTheValueTypeStates()
    {
        var carried = ProjectOneField("""{"kind":"integer","min":18,"max":99}""");

        Assert.Equal("18", carried.Min);
        Assert.Equal("99", carried.Max);
    }

    [Fact]
    public void Page_CarriesTheLengthsTheValueTypeStates()
    {

        var carried = ProjectOneField("""{"kind":"string","minLength":3,"maxLength":5}""");

        Assert.Equal(3, carried.MinLength);
        Assert.Equal(5, carried.MaxLength);
    }

    [Fact]
    public void Page_CarriesTheNarrowerOfAContractRuleAndTheValueTypeBound()
    {
        // Die strengere Grenze muss gelten, damit beide Anforderungen erfüllt sind.
        var carried = ProjectOneField(
            """{"kind":"integer","min":0}""",
            _factory.CreateConstraint(
                "atLeastOne",
                "target",
                ConstraintKinds.Min,
                new Dictionary<string, JsonElement> { [ConstraintPropertyNames.Min] = _factory.ParseJsonElement("1") }));

        Assert.Equal("1", carried.Min);
    }

    [Fact]
    public void Page_DoesNotCarryABoundOfASetItHasNoRuleFor()
    {
        // Grenzen für die Anzahl ausgewählter Werte sind keine numerischen Feldgrenzen.
        var carried = ProjectOneField("""{"kind":"set","minSelected":1,"maxSelected":3}""");

        Assert.Null(carried.Min);
        Assert.Null(carried.Max);
    }

    private FieldConstraints ProjectOneField(string valueTypeJson, params ConstraintDefinition[] contractRules)
    {
        var contract = _factory.CreateContractShell();
        contract.Constraints = [.. contractRules];
        var fieldsById = new Dictionary<string, FieldDefinition>(StringComparer.Ordinal)
        {
            ["target"] = _factory.CreateField("target", "targetVar", "Ziel")
        };
        var variablesById = new Dictionary<string, VariableDefinition>(StringComparer.Ordinal)
        {
            ["targetVar"] = _factory.CreateVariable("targetVar", valueTypeJson)
        };

        var projected = new FormConstraintProjector().ProjectByFieldId(contract, fieldsById, variablesById);
        return projected.TryGetValue("target", out var carried) ? carried : FieldConstraints.Empty;
    }
}
