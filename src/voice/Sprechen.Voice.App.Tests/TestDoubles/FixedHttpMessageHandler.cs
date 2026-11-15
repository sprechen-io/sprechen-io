namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class FixedHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    private readonly HttpResponseMessage _response = response;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(_response);
}
