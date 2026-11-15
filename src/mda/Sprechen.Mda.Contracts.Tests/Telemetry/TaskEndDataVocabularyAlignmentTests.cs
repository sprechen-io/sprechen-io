using System.Text;
using System.Text.Json;
using Sprechen.Mda.Contracts.Tests.Schema;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Mda.Contracts.Tests.Telemetry;

/// <summary>
/// Hält die Aufzählungen des Abschlussereignisses im eingebetteten Telemetrieschema und die
/// Konstanten der Laufzeit in beide Richtungen deckungsgleich.
/// </summary>
public sealed class TaskEndDataVocabularyAlignmentTests
{
    private const string SchemaPath = "evaluationsmodell/schemas/telemetry-events.schema.json";

    [Fact]
    public void MismatchCode_MatchesSchema_Bidirectional()
        => Assert.Equal(
            TaskEndDataEnum("mismatch_code"),
            ConstantSchemaAlignmentTests.GetPublicConstStringValues(typeof(TelemetryMismatchCodes)));

    [Fact]
    public void CorrectnessBasis_MatchesSchema_Bidirectional()
        => Assert.Equal(
            TaskEndDataEnum("correctness_basis"),
            ConstantSchemaAlignmentTests.GetPublicConstStringValues(typeof(TelemetryCorrectnessBases)));

    private static HashSet<string> TaskEndDataEnum(string propertyName)
    {
        using var schema = JsonDocument.Parse(
            Encoding.UTF8.GetString(MdaEmbeddedRuntimeArtifacts.ReadAllBytes(SchemaPath)));
        var values = schema.RootElement
            .GetProperty("$defs")
            .GetProperty("TaskEndData")
            .GetProperty("properties")
            .GetProperty(propertyName)
            .GetProperty("enum")
            .EnumerateArray()
            .Select(value => value.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        Assert.NotEmpty(values);
        return values;
    }
}
