using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Telemetry;

public sealed class CanonicalTransitionProjectionReferenceTableTests
{
    [Theory]
    [MemberData(nameof(TableCases))]
    public void Projection_MatchesTheAuthoredTable(string caseName, string eventName, JsonElement input, JsonElement expected)
    {
        Assert.False(string.IsNullOrWhiteSpace(caseName));

        var actual = Project(eventName, input);
        var expectedTransitions = expected.EnumerateArray().ToArray();

        Assert.Equal(expectedTransitions.Length, actual.Length);
        for (var index = 0; index < expectedTransitions.Length; index++)
        {
            Assert.Equal(expectedTransitions[index].GetProperty("canonicalClass").GetString(), actual[index].CanonicalClass);
            Assert.Equal(expectedTransitions[index].GetProperty("scope").GetString(), actual[index].Scope);
            Assert.Equal(expectedTransitions[index].GetProperty("objectId").GetString(), actual[index].ObjectId);
            Assert.Equal(expectedTransitions[index].GetProperty("resultingState").GetString(), actual[index].ResultingState);
        }
    }

    [Fact]
    public void Table_SeparatesLinesWithoutAStateChangeFromStepCapableOnes()
    {
        var cases = LoadTable().RootElement.GetProperty("cases").EnumerateArray().ToArray();
        Assert.NotEmpty(cases);

        // Beide Richtungen müssen vertreten sein; sonst würde die Tabelle nur eine davon prüfen.
        Assert.Contains(cases, entry => entry.GetProperty("expected").GetArrayLength() > 0);
        Assert.Contains(cases, entry => entry.GetProperty("expected").GetArrayLength() == 0);

        // Dieser Fall mit zwei Übergängen deckt die falsche Annahme auf, jede Zeile erzeuge höchstens einen Schritt.
        Assert.Contains(cases, entry => entry.GetProperty("expected").GetArrayLength() == 2);

        foreach (var entry in cases)
        {
            var isStepCapable = TelemetryCanonicalTransitionProjection.StepCapableEvents
                .Contains(entry.GetProperty("event").GetString()!);
            if (entry.GetProperty("expected").GetArrayLength() > 0)
            {
                Assert.True(
                    isStepCapable,
                    $"'{entry.GetProperty("case").GetString()}' projects a transition from an event the projection does not consider step-capable.");
            }
        }

        var positiveTableEvents = cases
            .Where(entry => entry.GetProperty("expected").GetArrayLength() > 0)
            .Select(entry => entry.GetProperty("event").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            TelemetryCanonicalTransitionProjection.StepCapableEvents.Order(StringComparer.Ordinal),
            positiveTableEvents.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void StepCapable_MeansAProjectionExistsForIt()
    {
        // Für jeden als schrittbildend deklarierten Ereignistyp muss die Referenztabelle
        // mindestens einen projizierbaren Fall enthalten. Fehlende Übergänge können
        // andernfalls die Schrittkennzahl der Aufgabe unauswertbar machen.
        var projectable = LoadTable().RootElement.GetProperty("cases").EnumerateArray()
            .Where(static entry => entry.GetProperty("expected").GetArrayLength() > 0)
            .Select(static entry => entry.GetProperty("event").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var declaredWithoutProjection = TelemetryCanonicalTransitionProjection.StepCapableEvents
            .Where(eventName => !projectable.Contains(eventName))
            .ToArray();

        Assert.True(
            declaredWithoutProjection.Length == 0,
            $"declared step-capable but no case of the reference table projects anything for it: {string.Join(", ", declaredWithoutProjection)}. "
            + "Every such line would set the step metric of its task to not_assessed.");
    }

    [Fact]
    public void Table_UsesOnlyDeclaredVocabulary()
    {
        foreach (var entry in LoadTable().RootElement.GetProperty("cases").EnumerateArray())
        {
            foreach (var transition in entry.GetProperty("expected").EnumerateArray())
            {
                Assert.Contains(transition.GetProperty("canonicalClass").GetString()!, TelemetryCanonicalClasses.All);
                Assert.Contains(transition.GetProperty("scope").GetString()!, TelemetryTransitionScopes.All);
                Assert.Contains(transition.GetProperty("resultingState").GetString()!, TelemetryResultingStates.All);
            }
        }
    }

    [Fact]
    public void AdoptionSequences_ContainAStepThatProjectsButRecordsNothingNew()
    {
        // Die Tabelle muss auch Projektionen ohne neue zu übernehmende Übergänge abdecken:
        // etwa Navigation innerhalb derselben Feldgruppe, deren Übergänge bereits bekannt sind.
        // Sonst bliebe eine irrtümliche Ablehnung dieses Falls in den tabellengestützten Tests unbemerkt.
        var steps = LoadTable().RootElement.GetProperty("adoptionSequences").EnumerateArray()
            .SelectMany(sequence => sequence.GetProperty("steps").EnumerateArray())
            .Where(step => step.TryGetProperty("projected", out _))
            .ToArray();

        Assert.Contains(steps, step =>
            step.GetProperty("projected").GetArrayLength() > 0
            && step.GetProperty("expectedDelta").GetArrayLength() == 0);
    }

    [Fact]
    public void AdoptionSequences_UseOnlyExecutableParticipantProjectionEvents()
    {
        var actual = LoadTable().RootElement.GetProperty("adoptionSequences").EnumerateArray()
            .SelectMany(sequence => sequence.GetProperty("steps").EnumerateArray())
            .SelectMany(step => step.TryGetProperty("events", out var events)
                ? events.EnumerateArray().Select(eventName => eventName.GetString()!)
                : [step.GetProperty("event").GetString()!])
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                TelemetryEventNames.FocusChange,
                TelemetryEventNames.NavigationChanged,
                TelemetryEventNames.ValueChange,
                TelemetryEventNames.FieldCompletionChanged,
                TelemetryEventNames.FieldGroupCompletionChanged,
            },
            actual);
        Assert.All(actual, eventName => Assert.Contains(
            eventName,
            TelemetryCanonicalTransitionProjection.StepCapableEvents));
    }

    [Fact]
    public void TransitionAdoptionLedgerSequences_UseClosedOperationsAndNonEmptyCompletionBaselines()
    {
        var operations = LoadTable().RootElement
            .GetProperty("transitionAdoptionLedgerSequences")
            .EnumerateArray()
            .SelectMany(sequence => sequence.GetProperty("operations").EnumerateArray())
            .ToArray();

        Assert.NotEmpty(operations);
        Assert.Equal(
            new[] { "completion_baseline", "event_batch" },
            operations.Select(operation => operation.GetProperty("kind").GetString()!)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal));

        foreach (var operation in operations)
        {
            var kind = operation.GetProperty("kind").GetString();
            if (string.Equals(kind, "completion_baseline", StringComparison.Ordinal))
            {
                var transitions = operation.GetProperty("completionTransitions").EnumerateArray().ToArray();
                Assert.NotEmpty(transitions);
                Assert.All(transitions, transition => Assert.Contains(
                    transition.GetProperty("canonicalClass").GetString()!,
                    new[]
                    {
                        TelemetryCanonicalClasses.FieldCompletionChanged,
                        TelemetryCanonicalClasses.FieldGroupCompletionChanged,
                    }));
                continue;
            }

            Assert.Equal("event_batch", kind);
            var eventCount = operation.GetProperty("events").GetArrayLength();
            Assert.True(eventCount > 0);
            Assert.Equal(eventCount, operation.GetProperty("projectedByEvent").GetArrayLength());
            Assert.Equal(eventCount, operation.GetProperty("expectedDeltaByEvent").GetArrayLength());
        }
    }

    // Übersetzt die Namen der Referenztabelle in die Ereignisnutzlast und ruft die
    // produktive Projektion auf. Dadurch prüft der Test keine Kopie des Projektionsalgorithmus.
    private static TelemetryCanonicalTransition[] Project(string eventName, JsonElement input)
    {
        string? Text(string name)
            => input.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.String
                ? value.GetString()
                : null;

        var payload = new Dictionary<string, object?>(StringComparer.Ordinal);

        if (string.Equals(eventName, TelemetryEventNames.ManualStep, StringComparison.Ordinal))
        {
            payload[TelemetryPayloadProperties.CanonicalTransitions] = new object[]
            {
                new TelemetryCanonicalTransition(
                    Text("canonicalClass")!,
                    Text("scope")!,
                    Text("objectId")!,
                    Text("resultingState")!),
            };
        }
        else if (string.Equals(eventName, TelemetryEventNames.NavigationChanged, StringComparison.Ordinal))
        {
            payload[TelemetryPayloadProperties.CurrentFieldId] = Text("fieldId");
            payload[TelemetryPayloadProperties.CurrentFieldGroupId] = Text("fieldGroupId");
        }
        else
        {
            if (Text("fieldId") is { } fieldId)
            {
                payload[TelemetryPayloadProperties.FieldId] = fieldId;
            }

            if (Text("fieldGroupId") is { } fieldGroupId)
            {
                payload[TelemetryPayloadProperties.FieldGroupId] = fieldGroupId;
            }
        }

        if (input.TryGetProperty("completed", out var completed)
            && completed.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            payload[TelemetryPayloadProperties.Completed] = completed.ValueKind is JsonValueKind.True;
        }

        return [.. TelemetryCanonicalTransitionProjection.ForEvent(eventName, payload)];
    }

    public static IEnumerable<object[]> TableCases()
    {
        foreach (var entry in LoadTable().RootElement.GetProperty("cases").EnumerateArray())
        {
            yield return
            [
                entry.GetProperty("case").GetString()!,
                entry.GetProperty("event").GetString()!,
                entry.GetProperty("input").Clone(),
                entry.GetProperty("expected").Clone(),
            ];
        }
    }

    private static JsonDocument LoadTable()
        => ReferenceTableResources.Read("canonical-transition-projection.reference.json");
}
