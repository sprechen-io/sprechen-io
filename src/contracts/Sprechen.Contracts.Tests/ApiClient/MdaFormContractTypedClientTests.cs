using Sprechen.Contracts.ApiClient;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class MdaFormContractTypedClientTests
{
    [Fact]
    public async Task GetFormContractAsync_LoadsTypedContractAndCachesResolvedContract()
    {
        var handler = new EmbeddedContractsHttpMessageHandler();
        var client = CreateClient(handler);

        var first = await client.GetFormContractAsync(
            " sample.simple ",
            " two-groups ",
            " 1 ",
            CancellationToken.None);
        var second = await client.GetFormContractAsync(
            "sample.simple",
            "two-groups",
            "1",
            CancellationToken.None);

        Assert.Same(first, second);
        Assert.Equal("sample.simple", first.ApplicationId);
        Assert.Equal("two-groups", first.FormId);
        Assert.Equal(1, handler.GetCallCount("form-contract:sample.simple/two-groups/1"));
        Assert.Equal(1, handler.GetCallCount("field-definitions:sample.simple.fields.multilingual/1"));
        Assert.Equal(1, handler.GetCallCount("variable-definitions:sample.simple.variables.multilingual/1"));
    }

    private static MdaFormContractTypedClient CreateClient(HttpMessageHandler handler)
    {
        var jsonReader = new ContractsApiJsonStreamReader();
        var preflightFailureMapper = new ContractsApiPreflightFailureMapper();
        var cacheKeyFactory = new ContractsApiResourceCacheKeyFactory();
        var resources = new ContractsApiResourceStreamClient(
            new ContractsApiResourceRoutes(),
            new ContractsApiHttpStreamFetcher(new HttpClient(handler)
            {
                BaseAddress = new Uri("https://contracts.test", UriKind.Absolute)
            }));
        return new MdaFormContractTypedClient(
            cacheKeyFactory,
            new FormContractDocumentResourceClient(
                resources,
                jsonReader),
            new FieldDefinitionsResourceClient(
                resources,
                jsonReader,
                preflightFailureMapper,
                cacheKeyFactory),
            new VariableDefinitionsResourceClient(
                resources,
                jsonReader,
                preflightFailureMapper,
                cacheKeyFactory),
            new FormVoiceProfileResourceClient(
                resources,
                jsonReader,
                preflightFailureMapper,
                cacheKeyFactory),
            preflightFailureMapper);
    }
}
