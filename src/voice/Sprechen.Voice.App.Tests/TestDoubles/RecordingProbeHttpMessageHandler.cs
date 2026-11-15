namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class RecordingProbeHttpMessageHandler(Func<HttpResponseMessage> response) : HttpMessageHandler
{
    internal List<(HttpMethod Method, string Path, string? Body, bool HasAuthorization)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add((request.Method, request.RequestUri!.AbsolutePath,
            request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken),
            request.Headers.Authorization is not null || request.Headers.Contains("X-Evaluation-Probe-Key")));
        return response();
    }
}
