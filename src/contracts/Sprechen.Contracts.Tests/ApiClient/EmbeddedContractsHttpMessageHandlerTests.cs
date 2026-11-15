using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class EmbeddedContractsHttpMessageHandlerTests
{
    [Theory]
    [InlineData("/contracts/application-selection-dialogs/sample.application-selection-dialog/2")]
    [InlineData("/contracts/application-selection-dialogs/unknown.dialog/3")]
    public async Task SendAsync_WhenSelectionDialogKeyIsNotPublished_RejectsRequest(string path)
    {
        using var client = new HttpClient(new EmbeddedContractsHttpMessageHandler())
        {
            BaseAddress = new Uri("https://contracts.test", UriKind.Absolute)
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync(path));

        Assert.Contains("Unknown application selection dialog key", ex.Message, StringComparison.Ordinal);
    }
}
