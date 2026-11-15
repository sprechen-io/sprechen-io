using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Telemetry;

/// <summary>
/// Vergleicht den Aufgabenendzustand mit der unabhängig von dieser Implementierung
/// verfassten Referenztabelle. Die Fälle müssen Zielerreichung ohne Absenden,
/// Nichterreichung ohne Absenden und fehlende Pflichtfelder unterscheiden.
/// </summary>
public sealed class TaskEndProjectionTableTests
{
    [Theory]
    [MemberData(nameof(TableCases))]
    public void Projection_MatchesTheAuthoredTable(string caseName, JsonElement input, JsonElement expected)
    {
        var actual = TelemetryTaskEndProjection.Project(
            input.GetProperty("processSubmitted").GetBoolean(),
            input.GetProperty("requiredFieldsTotal").GetInt32(),
            input.GetProperty("requiredFieldsCompleted").GetInt32(),
            input.GetProperty("openBlockingValidationCount").GetInt32(),
            input.GetProperty("terminationClassification").GetString()!);

        Assert.False(string.IsNullOrWhiteSpace(caseName));
        Assert.Equal(expected.GetProperty("endState").GetString(), actual.EndState);
        Assert.Equal(input.GetProperty("terminationClassification").GetString(), actual.TerminationClassification);
    }

    [Fact]
    public void Table_CoversTheCasesThatMakeADegeneratedProjectionVisible()
    {
        var cases = LoadTable().RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.NotEmpty(cases);

        // Ziel erreicht, ohne abzusenden: Zielerreichung darf nicht mit dem Absenden gleichgesetzt werden.
        Assert.Contains(cases, entry =>
            !entry.GetProperty("input").GetProperty("processSubmitted").GetBoolean()
            && entry.GetProperty("expected").GetProperty("endState").GetString()
                == TelemetryTaskEndStates.TaskTargetReached);

        // Auch ohne Absenden kann das Ziel verfehlt sein; allein der Sendestatus entscheidet das nicht.
        Assert.Contains(cases, entry =>
            !entry.GetProperty("input").GetProperty("processSubmitted").GetBoolean()
            && entry.GetProperty("expected").GetProperty("endState").GetString()
                == TelemetryTaskEndStates.NotSubmitted);

        // Ohne Bewertungsgrundlage darf die Projektion keine Zielerreichung annehmen.
        Assert.Contains(cases, entry =>
            entry.GetProperty("input").GetProperty("requiredFieldsTotal").GetInt32() == 0
            && entry.GetProperty("expected").GetProperty("endState").GetString()
                != TelemetryTaskEndStates.TaskTargetReached);
    }

    [Fact]
    public void Table_UsesOnlyDeclaredVocabulary()
    {
        foreach (var entry in LoadTable().RootElement.GetProperty("cases").EnumerateArray())
        {
            var expected = entry.GetProperty("expected");
            Assert.Contains(expected.GetProperty("endState").GetString(), TelemetryTaskEndStates.All);
        }
    }

    public static IEnumerable<object[]> TableCases()
    {
        foreach (var entry in LoadTable().RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return
            [
                entry.GetProperty("case").GetString()!,
                entry.GetProperty("input").Clone(),
                entry.GetProperty("expected").Clone(),
            ];
        }
    }

    private static JsonDocument LoadTable()
        => ReferenceTableResources.Read("task-end-projection.reference.json");
}
