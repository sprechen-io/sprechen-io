using Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.EvaluationTelemetry;

public sealed class HttpEvaluationTelemetryDeliveryTransportTests
{
    private const string ParticipantBody = "{\"record_provenance\":\"participant\"}";

    [Theory]
    [InlineData(408)]
    [InlineData(425)]
    [InlineData(429)]
    public async Task DeliverAsync_RetryableClientResponse_UsesRetryAfter(int statusCode)
    {
        var javascript = new RecordingEvaluationTelemetryDeliverJavascript(
            new EvaluationTelemetryBrowserDeliveryResult
            {
                Status = statusCode,
                RetryAfterMs = 7000,
            });
        var transport = CreateTransport(javascript);

        var result = await transport.DeliverAsync(ParticipantBody, CancellationToken.None);

        Assert.Equal(EvaluationTelemetryDeliveryStatus.TransientFailure, result.Status);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), result.RetryAfter);
        AssertDelivered(javascript, "/api/telemetry", ParticipantBody);
    }

    [Fact]
    public async Task DeliverAsync_NonRetryableClientResponse_IsPermanent()
    {
        var javascript = new RecordingEvaluationTelemetryDeliverJavascript(
            new EvaluationTelemetryBrowserDeliveryResult { Status = 422 });
        var transport = CreateTransport(javascript);

        var result = await transport.DeliverAsync(ParticipantBody, CancellationToken.None);

        Assert.Equal(EvaluationTelemetryDeliveryStatus.PermanentFailure, result.Status);
        Assert.Equal(422, result.StatusCode);
        AssertDelivered(javascript, "/api/telemetry", ParticipantBody);
    }

    [Fact]
    public async Task DeliverAsync_AcceptedResponse_Is204()
    {
        var javascript = new RecordingEvaluationTelemetryDeliverJavascript(
            new EvaluationTelemetryBrowserDeliveryResult { Status = 204 });
        var transport = CreateTransport(javascript);

        var result = await transport.DeliverAsync(ParticipantBody, CancellationToken.None);

        Assert.Equal(EvaluationTelemetryDeliveryStatus.Accepted, result.Status);
        Assert.Equal(204, result.StatusCode);
        AssertDelivered(javascript, "/api/telemetry", ParticipantBody);
    }

    [Fact]
    public async Task DeliverAsync_HungBrowserCall_TimesOutAsTransientWithoutCallerCancel()
    {
        var javascript = RecordingEvaluationTelemetryDeliverJavascript.HangUntilCanceled();
        var transport = new HttpEvaluationTelemetryDeliveryTransport(
            javascript,
            new OperationalProbeSession(),
            TimeSpan.FromMilliseconds(40));
        using var caller = new CancellationTokenSource();

        var result = await transport.DeliverAsync(ParticipantBody, caller.Token);

        Assert.Equal(EvaluationTelemetryDeliveryStatus.TransientFailure, result.Status);
        Assert.False(caller.IsCancellationRequested);
        Assert.Equal(1, javascript.InvokeCount);
        AssertDelivered(javascript, "/api/telemetry", ParticipantBody);
    }

    [Fact]
    public async Task DeliverAsync_BrowserNetworkFailure_IsTransient()
    {
        var javascript = RecordingEvaluationTelemetryDeliverJavascript.ThrowJsException();
        var transport = CreateTransport(javascript);

        var result = await transport.DeliverAsync(ParticipantBody, CancellationToken.None);

        Assert.Equal(EvaluationTelemetryDeliveryStatus.TransientFailure, result.Status);
        Assert.Null(result.StatusCode);
        AssertDelivered(javascript, "/api/telemetry", ParticipantBody);
    }

    private static HttpEvaluationTelemetryDeliveryTransport CreateTransport(
        RecordingEvaluationTelemetryDeliverJavascript javascript)
        => new(javascript, new OperationalProbeSession());

    private static void AssertDelivered(
        RecordingEvaluationTelemetryDeliverJavascript javascript,
        string route,
        string body)
    {
        Assert.Equal("evaluationTelemetryOutbox.deliver", javascript.Identifier);
        Assert.Equal(3, javascript.Arguments.Length);
        Assert.Equal(route, javascript.Arguments[0]);
        Assert.Equal(body, javascript.Arguments[1]);
        Assert.IsType<int>(javascript.Arguments[2]);
        Assert.True((int)javascript.Arguments[2]! >= 1);
    }
}
