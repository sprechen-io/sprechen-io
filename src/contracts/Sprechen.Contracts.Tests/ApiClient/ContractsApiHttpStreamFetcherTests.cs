using System.Net;
using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class ContractsApiHttpStreamFetcherTests
{
    [Fact]
    public async Task GetStreamAsync_WhenResponseIsSuccess_ReturnsReusableBodyStream()
    {
        var handler = TestHttpMessageHandler.WithStatus(HttpStatusCode.OK, """{"ok":true}""");
        using var httpClient = CreateHttpClient(handler);
        var fetcher = new ContractsApiHttpStreamFetcher(httpClient);

        await using var stream = await fetcher.GetStreamAsync("/contracts/forms-catalog", CancellationToken.None);

        using var reader = new StreamReader(stream);
        Assert.Equal("""{"ok":true}""", await reader.ReadToEndAsync(CancellationToken.None));
        Assert.Equal("/contracts/forms-catalog", handler.RequestUris.Single()?.PathAndQuery);
    }

    [Fact]
    public async Task GetStreamAsync_WhenResponseIsNotSuccess_ThrowsHttpRequestException()
    {
        var handler = TestHttpMessageHandler.WithStatus(HttpStatusCode.NotFound, """{"error":"missing"}""");
        using var httpClient = CreateHttpClient(handler);
        var fetcher = new ContractsApiHttpStreamFetcher(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => fetcher.GetStreamAsync("/contracts/forms-catalog", CancellationToken.None));
    }

    [Fact]
    public async Task GetStreamAsync_WhenResponseBodyIsEmpty_ThrowsInvalidOperationException()
    {
        var handler = TestHttpMessageHandler.WithStatus(HttpStatusCode.OK);
        using var httpClient = CreateHttpClient(handler);
        var fetcher = new ContractsApiHttpStreamFetcher(httpClient);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fetcher.GetStreamAsync("/contracts/forms-catalog", CancellationToken.None));

        Assert.Contains("No response body received", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetStreamAsync_WhenCancellationIsRequested_ThrowsOperationCanceledException()
    {
        var handler = new TestHttpMessageHandler((_, cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var httpClient = CreateHttpClient(handler);
        var fetcher = new ContractsApiHttpStreamFetcher(httpClient);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fetcher.GetStreamAsync("/contracts/forms-catalog", cancellation.Token));
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://contracts.example")
        };
    }
}
