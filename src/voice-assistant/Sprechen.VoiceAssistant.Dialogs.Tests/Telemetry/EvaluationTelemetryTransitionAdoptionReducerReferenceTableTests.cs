using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Vergleicht Zustandsversion und übernommene Übergänge mit den vorgegebenen Referenzfolgen. Dieser Test isoliert die reine Zustandsberechnung; Speicherung und Wiederanlauf werden separat geprüft.
/// </summary>
public sealed class EvaluationTelemetryTransitionAdoptionReducerReferenceTableTests
{

    [Fact]
    public void Reducer_MatchesEveryAuthoredAdoptionSequence()
    {
        using var table = LoadTable();
        var sequences = table.RootElement.GetProperty("adoptionSequences").EnumerateArray().ToArray();

        Assert.Equal(4, sequences.Length);
        Assert.Contains(sequences, sequence => sequence.GetProperty("steps").EnumerateArray()
            .Any(step => step.TryGetProperty("restoreBoundaryBefore", out var boundary) && boundary.GetBoolean()));

        foreach (var sequence in sequences)
        {
            var sequenceName = sequence.GetProperty("sequence").GetString();
            Assert.False(string.IsNullOrWhiteSpace(sequenceName));

            EvaluationTelemetryTransitionAdoptionResult? previous = null;
            foreach (var step in sequence.GetProperty("steps").EnumerateArray())
            {
                var priorStateVersion = step.GetProperty("priorStateVersion").GetInt32();
                var priorAdopted = ReadNullableTransitions(step.GetProperty("priorAdoptedTransitions"))?
                    .Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity)
                    .ToArray();
                var projected = ReadTransitions(step.GetProperty("projected"));

                if (previous is not null)
                {
                    Assert.Equal(previous.StateVersion, priorStateVersion);
                    Assert.Equal(previous.AdoptedTransitionIdentities, priorAdopted);
                }

                var actual = EvaluationTelemetryTransitionAdoptionReducer.Advance(
                    priorStateVersion,
                    priorAdopted,
                    projected);
                var expectedAdopted = ReadTransitions(step.GetProperty("expectedAdoptedTransitions"))
                    .Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity)
                    .ToArray();
                var expectedDelta = ReadTransitions(step.GetProperty("expectedDelta"));

                Assert.True(
                    actual.StateVersion == step.GetProperty("expectedStateVersion").GetInt32(),
                    $"'{sequenceName}' produced the wrong state version.");
                Assert.Equal(expectedAdopted, actual.AdoptedTransitionIdentities);
                Assert.Equal(expectedDelta, actual.EmittedTransitions);
                previous = actual;
            }
        }
    }

    [Fact]
    public void Reducer_MatchesTheAuthoredTransitionAdoptionLedgerSequence()
    {
        using var table = LoadTable();
        var sequence = Assert.Single(table.RootElement
            .GetProperty("transitionAdoptionLedgerSequences")
            .EnumerateArray());
        var stateVersion = 1;
        IReadOnlyList<string>? adoptedTransitionIdentities = null;

        foreach (var operation in sequence.GetProperty("operations").EnumerateArray())
        {
            var kind = operation.GetProperty("kind").GetString();
            EvaluationTelemetryTransitionAdoptionResult actual;
            if (string.Equals(kind, "completion_baseline", StringComparison.Ordinal))
            {
                actual = EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(
                    stateVersion,
                    adoptedTransitionIdentities,
                    ReadTransitions(operation.GetProperty("completionTransitions")));
                Assert.Empty(actual.EmittedTransitions);
            }
            else
            {
                var projected = operation.GetProperty("projectedByEvent")
                    .EnumerateArray()
                    .SelectMany(ReadTransitions)
                    .ToArray();
                actual = EvaluationTelemetryTransitionAdoptionReducer.Advance(
                    stateVersion,
                    adoptedTransitionIdentities,
                    projected);
                var expectedDelta = operation.GetProperty("expectedDeltaByEvent")
                    .EnumerateArray()
                    .SelectMany(ReadTransitions)
                    .ToArray();
                Assert.Equal(expectedDelta, actual.EmittedTransitions);
            }

            Assert.Equal(operation.GetProperty("expectedStateVersion").GetInt32(), actual.StateVersion);
            stateVersion = actual.StateVersion;
            adoptedTransitionIdentities = actual.AdoptedTransitionIdentities;
        }

        Assert.Equal(3, stateVersion);
    }

    private static TelemetryCanonicalTransition[]? ReadNullableTransitions(JsonElement value)
        => value.ValueKind is JsonValueKind.Null ? null : ReadTransitions(value);

    private static TelemetryCanonicalTransition[] ReadTransitions(JsonElement value)
        => value.EnumerateArray().Select(transition => new TelemetryCanonicalTransition(
            transition.GetProperty("canonicalClass").GetString()!,
            transition.GetProperty("scope").GetString()!,
            transition.GetProperty("objectId").GetString()!,
            transition.GetProperty("resultingState").GetString()!)).ToArray();

    private static JsonDocument LoadTable()
        => ReferenceTableResources.Read("canonical-transition-projection.reference.json");

}
