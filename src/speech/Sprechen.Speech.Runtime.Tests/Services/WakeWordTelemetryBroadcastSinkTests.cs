using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.ClientInfrastructure.Extensions;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.Runtime.Tests.Testing;
using Sprechen.Speech.WakeWord.ClientInfrastructure.Services;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class WakeWordTelemetryBroadcastSinkTests
{
    [Fact]
    public void AddWebSprechenClientCore_WithoutTelemetryGraph_ResolvesTheWakeTelemetrySink()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services.AddWebSprechenClientCore();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IEvaluationWakeWordTelemetry));

        using var scope = ValidatedServiceProviderScope.Create(services);

        var sink = scope.GetRequiredService<IWakeWordTelemetrySink>();
        var evaluationTelemetry = scope.GetRequiredService<IEvaluationWakeWordTelemetry>();

        Assert.NotNull(sink);
        Assert.Equal(typeof(NoOpEvaluationWakeWordTelemetry), descriptor.ImplementationType);
        Assert.IsType<NoOpEvaluationWakeWordTelemetry>(evaluationTelemetry);
    }

    [Fact]
    public void StudyTelemetryComposition_ReplacesTheNoOpWithExactlyOneDescriptor()
    {
        var services = WebSpeechClientServiceCollectionFactory.Create();
        services.AddWebSprechenClientCore();
        services.AddWebSprechenSpeechAssistantTelemetry();

        var descriptor = Assert.Single(
            services,
            candidate => candidate.ServiceType == typeof(IEvaluationWakeWordTelemetry));

        Assert.Equal(typeof(EvaluationWakeWordTelemetry), descriptor.ImplementationType);
        Assert.DoesNotContain(
            services,
            candidate => candidate.ServiceType == typeof(IEvaluationWakeWordTelemetry)
                && candidate.ImplementationType == typeof(NoOpEvaluationWakeWordTelemetry));
    }

    [Fact]
    public void ProjectMiss_ProducesTheModeledPayloadForEveryMissPhase()
    {
        foreach (var phase in Enum.GetValues<WakeWordMissPhase>())
        {
            var recorder = new RecordingEvaluationWakeWordTelemetry();
            var projection = CreateProjection(recorder);

            projection.ProjectMiss(CreateMiss(phase));

            var miss = Assert.Single(recorder.Recorded);
            Assert.Contains(miss.Phase, TelemetryWakeWordMissPhases.All);
            Assert.Contains(miss.EventCode, TelemetryWakeWordMissEventCodes.All);
            Assert.Contains(miss.Status, TelemetryWakeWordActivationStatuses.All);
        }
    }

    [Fact]
    public void ProjectMiss_WhenOptionalValuesAreAbsent_KeepsThemAbsent()
    {
        var recorder = new RecordingEvaluationWakeWordTelemetry();
        var projection = CreateProjection(recorder);
        var miss = new WakeWordMissEvent(
            "cid",
            WakeWordMissPhase.MissTrace,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            ConfidencePercent: null,
            TranscriptLength: null,
            ActivationFormCount: null,
            RecognitionOnlyActivationFormCount: null,
            Exception: null,
            Diagnostic: null);

        projection.ProjectMiss(miss);

        var recorded = Assert.Single(recorder.Recorded);
        Assert.Null(recorded.ConfidencePercent);
        Assert.Null(recorded.TranscriptTextLength);
        Assert.Null(recorded.ActivationFormCount);
        Assert.Null(recorded.RecognitionOnlyActivationFormCount);
        Assert.Null(recorded.ExceptionType);
    }

    [Fact]
    public async Task RecordMissAsync_WhenTheEvaluationProjectionThrows_KeepsTheDiagnosticProjection()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var diagnostic = new WakeWordTelemetryForwardingSink(
            trace,
            NullLogger<WakeWordTelemetryForwardingSink>.Instance);
        var broadcast = new WakeWordTelemetryBroadcastSink(
            diagnostic,
            new WakeWordEvaluationTelemetryProjection(new ThrowingEvaluationWakeWordTelemetry()),
            NullLogger<WakeWordTelemetryBroadcastSink>.Instance);

        await broadcast.RecordMissAsync(CreateMiss(WakeWordMissPhase.MissTrace), CancellationToken.None);

        Assert.NotEmpty(trace.Entries);
    }

    [Fact]
    public async Task RecordMissAsync_ReachesBothProjections()
    {
        var trace = new RecordingSpeechAssistantTrace();
        var recorder = new RecordingEvaluationWakeWordTelemetry();
        var broadcast = new WakeWordTelemetryBroadcastSink(
            new WakeWordTelemetryForwardingSink(trace, NullLogger<WakeWordTelemetryForwardingSink>.Instance),
            new WakeWordEvaluationTelemetryProjection(recorder),
            NullLogger<WakeWordTelemetryBroadcastSink>.Instance);

        await broadcast.RecordMissAsync(CreateMiss(WakeWordMissPhase.MissTrace), CancellationToken.None);

        Assert.NotEmpty(trace.Entries);
        Assert.Single(recorder.Recorded);
    }

    private static WakeWordEvaluationTelemetryProjection CreateProjection(IEvaluationWakeWordTelemetry evaluationTelemetry)
        => new(evaluationTelemetry);

    private static WakeWordMissEvent CreateMiss(WakeWordMissPhase phase)
        => new(
            "cid",
            phase,
            WakeWordActivationStatus.MissingLeadingWakeWord,
            ConfidencePercent: 78,
            TranscriptLength: 3,
            ActivationFormCount: 2,
            RecognitionOnlyActivationFormCount: 1,
            Exception: null,
            Diagnostic: null);

}
