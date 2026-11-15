using System.Net;
using System.Net.Http.Headers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Voice.App.Tests.TestDoubles;
using Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.Voice.App.Tests.EvaluationTelemetry;

public sealed class OperationalProbeHttpTests
{
    private const string ProbeId = "probe-0123456789abcdef0123456789abcdef";
    private const string Body = "{ \"record_provenance\":\"operational_probe\", \"participant_id\":\"probe-0123456789abcdef0123456789abcdef\", \"condition\":\"B\", \"application_id\":\"study.umfrage\", \"task_id\":\"hintergrund-und-setup\" }";

    [Fact]
    public async Task Bootstrap_UsesExactGetAndNoBrowserKey()
    {
        var handler = new RecordingProbeHttpMessageHandler(() => BootstrapResponse());
        using var client = Client(handler);
        var context = await new HttpOperationalProbeBootstrapClient(client).BootstrapAsync(CancellationToken.None);
        Assert.Equal(ProbeId, context.ProbeId);
        Assert.Equal("B", context.Condition);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("/api/telemetry/probe/context", request.Path);
        Assert.Null(request.Body);
        Assert.False(request.HasAuthorization);
    }

    [Theory]
    [InlineData(401, true, "valid")]
    [InlineData(403, true, "valid")]
    [InlineData(200, false, "valid")]
    [InlineData(200, true, "wrong-condition")]
    [InlineData(200, true, "wrong-id")]
    [InlineData(200, true, "extra-member")]
    [InlineData(200, true, "malformed")]
    public async Task Bootstrap_RefusesUntrustedResponse(int status, bool noStore, string shape)
    {
        var handler = new RecordingProbeHttpMessageHandler(() => BootstrapResponse(status, noStore, shape));
        using var client = Client(handler);
        await Assert.ThrowsAnyAsync<Exception>(() => new HttpOperationalProbeBootstrapClient(client).BootstrapAsync(CancellationToken.None));
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(204, EvaluationTelemetryDeliveryStatus.Accepted)]
    [InlineData(503, EvaluationTelemetryDeliveryStatus.TransientFailure)]
    [InlineData(403, EvaluationTelemetryDeliveryStatus.PermanentFailure)]
    public async Task Delivery_UsesProbeRouteAndOriginalBytesWithRealStatus(int status, EvaluationTelemetryDeliveryStatus expected)
    {
        var javascript = new RecordingEvaluationTelemetryDeliverJavascript(
            new EvaluationTelemetryBrowserDeliveryResult { Status = status });
        var transport = new HttpEvaluationTelemetryDeliveryTransport(javascript, BoundProbe());
        var result = await transport.DeliverAsync(Body, CancellationToken.None);
        Assert.Equal(expected, result.Status);
        Assert.Equal("evaluationTelemetryOutbox.deliver", javascript.Identifier);
        Assert.Equal("/api/telemetry/probe", javascript.Arguments[0]);
        Assert.Equal(Body, javascript.Arguments[1]);
        Assert.Equal(1, javascript.InvokeCount);
    }

    [Theory]
    [InlineData("missing-context")]
    [InlineData("unbound")]
    [InlineData("revoked")]
    [InlineData("foreign-id")]
    [InlineData("foreign-application")]
    [InlineData("foreign-task")]
    [InlineData("wrong-condition")]
    [InlineData("participant-during-probe")]
    [InlineData("demo-during-probe")]
    public async Task Delivery_RefusesMismatchedLifecycleWithoutSending(string boundary)
    {
        var probe = BoundProbe();
        var body = Body;
        if (boundary is "missing-context" or "revoked") probe.Reset();
        if (boundary == "unbound")
        {
            probe.Request();
            probe.AcceptContext(new OperationalProbeContext(ProbeId, "B"));
        }
        body = boundary switch
        {
            "foreign-id" => body.Replace(ProbeId, "probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", StringComparison.Ordinal),
            "foreign-application" => body.Replace("study.umfrage", "sample.complex", StringComparison.Ordinal),
            "foreign-task" => body.Replace("hintergrund-und-setup", "other-task", StringComparison.Ordinal),
            "wrong-condition" => body.Replace("\"B\"", "\"A\"", StringComparison.Ordinal),
            "participant-during-probe" => body.Replace("operational_probe", "participant", StringComparison.Ordinal),
            "demo-during-probe" => body.Replace("operational_probe", "demonstration", StringComparison.Ordinal),
            _ => body,
        };
        var javascript = new RecordingEvaluationTelemetryDeliverJavascript(
            new EvaluationTelemetryBrowserDeliveryResult { Status = 204 });
        var result = await new HttpEvaluationTelemetryDeliveryTransport(javascript, probe).DeliverAsync(body, CancellationToken.None);
        Assert.Equal(EvaluationTelemetryDeliveryStatus.PermanentFailure, result.Status);
        Assert.Equal(0, javascript.InvokeCount);
        Assert.Null(javascript.Identifier);
    }

    private static OperationalProbeSession BoundProbe()
    {
        var probe = new OperationalProbeSession();
        probe.Request();
        var context = new OperationalProbeContext(ProbeId, "B");
        probe.AcceptContext(context);
        probe.BindStudy(context, "study.umfrage", "hintergrund-und-setup");
        return probe;
    }

    private static HttpClient Client(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://example.test/") };

    private static HttpResponseMessage BootstrapResponse(int status = 200, bool noStore = true, string shape = "valid")
    {
        var body = "{\"probe_id\":\"" + ProbeId + "\",\"condition\":\"B\"}";
        body = shape switch
        {
            "wrong-condition" => body.Replace("\"B\"", "\"A\"", StringComparison.Ordinal),
            "wrong-id" => body.Replace(ProbeId, "xyz001", StringComparison.Ordinal),
            "extra-member" => body.Replace("}", ",\"participant_id\":\"xyz001\"}", StringComparison.Ordinal),
            "malformed" => "[]",
            _ => body,
        };
        var response = new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) };
        response.Headers.CacheControl = new CacheControlHeaderValue { NoStore = noStore };
        return response;
    }
}
