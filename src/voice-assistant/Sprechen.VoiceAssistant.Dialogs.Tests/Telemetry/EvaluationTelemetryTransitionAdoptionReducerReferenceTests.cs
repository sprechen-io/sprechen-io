using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Vergleicht Zustandsversion und übernommene Übergänge mit den vorgegebenen Referenzfolgen. Dieser Test isoliert die reine Zustandsberechnung; Speicherung und Wiederanlauf werden separat geprüft.
/// </summary>
public sealed class EvaluationTelemetryTransitionAdoptionReducerReferenceTests
{
    [Fact]
    public void CompletionBaseline_PrimesWithoutEmissionOrVersionAdvance_AndRejectsConflicts()
    {
        var initial = new[]
        {
            Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged("field-a", completed: false)),
            Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldGroupCompletionChanged("group-a", completed: false)),
        };

        var primed = EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(1, null, initial);

        Assert.Equal(1, primed.StateVersion);
        Assert.Empty(primed.EmittedTransitions);
        Assert.Equal(
            initial.Select(EvaluationTelemetryTransitionAdoptionReducer.BuildIdentity),
            primed.AdoptedTransitionIdentities);

        var extended = EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(
            primed.StateVersion,
            primed.AdoptedTransitionIdentities,
            [
                .. initial,
                Assert.Single(TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged("field-b", completed: true)),
            ]);

        Assert.Equal(1, extended.StateVersion);
        Assert.Empty(extended.EmittedTransitions);
        Assert.Equal(3, extended.AdoptedTransitionIdentities!.Count);
        Assert.Throws<InvalidDataException>(() => EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(
            extended.StateVersion,
            extended.AdoptedTransitionIdentities,
            TelemetryCanonicalTransitionProjection.ForFieldCompletionChanged("field-a", completed: true)));
        Assert.Throws<InvalidDataException>(() => EvaluationTelemetryTransitionAdoptionReducer.PrimeCompletionBaseline(
            extended.StateVersion,
            extended.AdoptedTransitionIdentities,
            TelemetryCanonicalTransitionProjection.ForFocusChange("field-a")));
    }
}
