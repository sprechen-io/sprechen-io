using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class EvaluationTelemetryDetailedStatusChangeDetectorTests
{
    [Fact]
    public void DetectChanges_RequiresTheCallerToPrimeThePreviousSnapshot()
    {
        var snapshot = CreateSnapshot(fieldCompleted: false, groupCompleted: false);

        Assert.Throws<ArgumentNullException>(() => CreateDetector().DetectChanges(null!, snapshot));
    }

    [Fact]
    public void DetectChanges_WhenOnlyCompletionChanges_EmitsOnlyCompletion()
    {
        var previous = CreateSnapshot(fieldCompleted: false, groupCompleted: false);
        var current = CreateSnapshot(fieldCompleted: true, groupCompleted: true);

        var changes = CreateDetector().DetectChanges(previous, current);

        Assert.Collection(
            changes,
            change => Assert.Equal(TelemetryEventNames.FieldCompletionChanged, change.EventName),
            change => Assert.Equal(TelemetryEventNames.FieldGroupCompletionChanged, change.EventName));
    }

    private static EvaluationDetailedFormStatus CreateSnapshot(
        bool fieldCompleted,
        bool groupCompleted,
        bool fieldRequired = true,
        ImmutableArray<string> fieldBlockingErrorCodes = default,
        ImmutableArray<string> groupOpenErrorCodes = default)
        => new(
            [
                new EvaluationFieldStatus(
                    "field1",
                    "group1",
                    Relevant: true,
                    fieldRequired,
                    fieldCompleted,
                    fieldBlockingErrorCodes.IsDefault ? ImmutableArray<string>.Empty : fieldBlockingErrorCodes)
            ],
            [
                new EvaluationFieldGroupStatus(
                    "group1",
                    Relevant: true,
                    groupCompleted,
                    groupOpenErrorCodes.IsDefault ? ImmutableArray<string>.Empty : groupOpenErrorCodes)
            ]);

    internal static EvaluationTelemetryDetailedStatusChangeDetector CreateDetector()
    {
        return new EvaluationTelemetryDetailedStatusChangeDetector(new EvaluationStatusTelemetryPayloads());
    }
}
