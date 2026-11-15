using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.Telemetry;

public sealed class CanonicalTransitionProjectionTableTests
{
    [Fact]
    public void DeduplicationKey_CountsAReturnToTheSameTargetOnlyAtAHigherStateVersion()
    {
        var transition = new TelemetryCanonicalTransition(
            TelemetryCanonicalClasses.FieldTargetChanged,
            TelemetryTransitionScopes.Field,
            "contactEmail",
            TelemetryResultingStates.Targeted);

        string Key(int stateVersion) => TelemetryCanonicalTransition.BuildDeduplicationKey(
            "P001", "study.umfrage", "B", "T03", stateVersion, transition);

        // Dasselbe Ziel innerhalb einer Zustandsversion zählt nur einmal. Die Versionen
        // sind hier vorgegeben; der Test belegt die Deduplizierung.
        Assert.Equal(1, TelemetryCanonicalTransitionProjection.CountDeduplicated([Key(4), Key(4)]));

        // Nach einem Zustandswechsel zählt die Rückkehr zum selben Ziel als weiterer Schritt.
        Assert.Equal(2, TelemetryCanonicalTransitionProjection.CountDeduplicated([Key(4), Key(5)]));
    }

    [Fact]
    public void DeduplicationKey_JoinsTheNineMembersInTheSpecifiedOrder()
    {
        var key = TelemetryCanonicalTransition.BuildDeduplicationKey(
            participantId: "member-participant",
            applicationId: "member-application",
            condition: "member-condition",
            taskId: "member-task",
            stateVersion: 7,
            transition: new TelemetryCanonicalTransition(
                TelemetryCanonicalClasses.FieldTargetChanged,
                TelemetryTransitionScopes.Field,
                "member-object",
                TelemetryResultingStates.Targeted));

        Assert.Equal(
            [
                "member-participant",
                "member-application",
                "member-condition",
                "member-task",
                "7",
                TelemetryCanonicalClasses.FieldTargetChanged,
                TelemetryTransitionScopes.Field,
                "member-object",
                TelemetryResultingStates.Targeted,
            ],
            key.Split('\u001F'));
    }

    [Fact]
    public void DeduplicationKey_SeparatesTransitionsThatDifferInAnySingleMember()
    {
        var baseline = new TelemetryCanonicalTransition(
            TelemetryCanonicalClasses.FieldTargetChanged,
            TelemetryTransitionScopes.Field,
            "contactEmail",
            TelemetryResultingStates.Targeted);

        string Key(
            string participantId = "P001",
            string applicationId = "study.umfrage",
            string condition = "B",
            string taskId = "T03",
            int stateVersion = 4,
            TelemetryCanonicalTransition? transition = null)
            => TelemetryCanonicalTransition.BuildDeduplicationKey(
                participantId, applicationId, condition, taskId, stateVersion, transition ?? baseline);

        var keys = new[]
        {
            Key(),
            Key(participantId: "P002"),
            Key(applicationId: "sample.simple"),
            Key(condition: "A"),
            Key(taskId: "T04"),
            Key(stateVersion: 5),
            Key(transition: baseline with { CanonicalClass = TelemetryCanonicalClasses.FieldCompletionChanged }),
            Key(transition: baseline with { Scope = TelemetryTransitionScopes.FieldGroup }),
            Key(transition: baseline with { ObjectId = "contactPhone" }),
            Key(transition: baseline with { ResultingState = TelemetryResultingStates.Complete }),
        };

        Assert.Equal(keys.Length, TelemetryCanonicalTransitionProjection.CountDeduplicated(keys));
    }
}
