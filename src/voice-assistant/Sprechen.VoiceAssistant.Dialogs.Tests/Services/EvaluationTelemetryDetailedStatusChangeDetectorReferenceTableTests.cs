using static Sprechen.VoiceAssistant.Dialogs.Tests.Services.EvaluationTelemetryDetailedStatusChangeDetectorTests;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Services;

public sealed class EvaluationTelemetryDetailedStatusChangeDetectorReferenceTableTests
{

    [Fact]
    public void DetectChanges_WhenOnlyCompletionMetadataChanges_EmitsNoCompletionTransition()
    {
        using var oracle = LoadCanonicalTransitionOracle();
        var metadata = oracle.RootElement
            .GetProperty("transitionAdoptionLedgerSequences")[0]
            .GetProperty("operations")
            .EnumerateArray()
            .Single(operation => operation.GetProperty("operation").GetString() == "metadata_only")
            .GetProperty("metadataOnlyStatusChanges");
        var field = metadata.GetProperty("fields")[0];
        var group = metadata.GetProperty("fieldGroups")[0];
        var current = new EvaluationDetailedFormStatus(
            [
                new EvaluationFieldStatus(
                    field.GetProperty("fieldId").GetString()!,
                    field.GetProperty("fieldGroupId").GetString()!,
                    field.GetProperty("relevant").GetBoolean(),
                    field.GetProperty("required").GetBoolean(),
                    field.GetProperty("completed").GetBoolean(),
                    field.GetProperty("blockingErrorCodes").EnumerateArray()
                        .Select(static code => code.GetString()!).ToImmutableArray()),
            ],
            [
                new EvaluationFieldGroupStatus(
                    group.GetProperty("fieldGroupId").GetString()!,
                    group.GetProperty("relevant").GetBoolean(),
                    group.GetProperty("completed").GetBoolean(),
                    group.GetProperty("openGroupErrorCodes").EnumerateArray()
                        .Select(static code => code.GetString()!).ToImmutableArray()),
            ]);
        var previous = new EvaluationDetailedFormStatus(
            [current.Fields[0] with { Required = false, BlockingErrorCodes = ["prior-field-error"] }],
            [current.FieldGroups[0] with { OpenGroupErrorCodes = ["prior-group-error"] }]);

        var changes = CreateDetector().DetectChanges(previous, current);

        Assert.Empty(changes);
    }

    private static JsonDocument LoadCanonicalTransitionOracle()
        => ReferenceTableResources.Read("canonical-transition-projection.reference.json");

}
