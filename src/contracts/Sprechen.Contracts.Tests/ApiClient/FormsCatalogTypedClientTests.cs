using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1.Catalog;
using Xunit;

namespace Sprechen.Contracts.Tests.ApiClient;

public sealed class FormsCatalogTypedClientTests
{
    private static readonly string[] SelectionIntegrityPromptIds =
    [
        "catalog.app.ambiguity.short.intro",
        "catalog.form.ambiguity.short.intro",
        "catalog.app.ambiguity.refineOrList",
        "catalog.form.ambiguity.refineOrList",
        "catalog.app.confirmation.explicitAuthorization",
        "catalog.form.confirmation.explicitAuthorization",
        "catalog.app.confirmation.verificationUnavailable",
        "catalog.form.confirmation.verificationUnavailable",
        ApplicationSelectionRequiredPromptId.AppConfirmationRepeat,
        ApplicationSelectionRequiredPromptId.FormConfirmationRepeat,
        "catalog.app.confirmation.notOpenedReselect",
        "catalog.form.confirmation.notOpenedReselect",
        "catalog.app.confirmation.verificationFailedTerminal",
        "catalog.form.confirmation.verificationFailedTerminal",
        "catalog.app.confirmation.repairExhaustedTerminal",
        "catalog.form.confirmation.repairExhaustedTerminal",
        "catalog.app.captureFailedTerminal",
        "catalog.form.captureFailedTerminal"
    ];

    [Fact]
    public async Task GetFormsCatalogAsync_RefreshesReleasePointerAndCachesImmutableReferenceResources()
    {
        var handler = new EmbeddedContractsHttpMessageHandler();
        var client = CreateClient(handler);

        var first = await client.GetFormsCatalogAsync(CancellationToken.None);
        var second = await client.GetFormsCatalogAsync(CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.Single(first.Applications, app => app.ApplicationId == "sample.simple");
        Assert.Single(first.Applications, app => app.ApplicationId == "sample.complex");
        Assert.Equal("sample.application-selection-dialog", first.SelectionDialog.SelectionDialogId);
        Assert.Equal("5", first.SelectionDialog.ModelVersion);
        Assert.Single(first.SelectionDialog.Intents, intent => intent.IntentId == "openCandidate");
        AssertSelectionIntegrityPrompts(first.SelectionDialog);
        Assert.Equal(2, handler.GetCallCount("forms-catalog"));
        Assert.Equal(1, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/5"));
        Assert.Equal(0, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/2"));
        Assert.Equal(1, handler.GetCallCount("application-selection-dialog-profile:sample.application-selection-dialog.default/1"));
    }

    [Fact]
    public async Task GetFormsCatalogAsync_UnsupportedV2PointerFailsClosedWithoutLoadingV2Dialog()
    {
        var handler = new EmbeddedContractsHttpMessageHandler(selectionDialogModelVersion: "2");
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetFormsCatalogAsync(CancellationToken.None));

        Assert.Contains("requires application selection dialog model version '5'", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, handler.GetCallCount("forms-catalog"));
        Assert.Equal(0, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/2"));
        Assert.Equal(0, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/5"));
    }

    [Fact]
    public async Task GetFormsCatalogAsync_SameSessionObservesV5CutoverAndCannotRetainV2()
    {
        var handler = new EmbeddedContractsHttpMessageHandler(selectionDialogModelVersion: "2");
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetFormsCatalogAsync(CancellationToken.None));
        handler.SetSelectionDialogModelVersion("5");

        var catalog = await client.GetFormsCatalogAsync(CancellationToken.None);

        Assert.Equal("5", catalog.SelectionDialog.ModelVersion);
        Assert.Equal(2, handler.GetCallCount("forms-catalog"));
        Assert.Equal(0, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/2"));
        Assert.Equal(1, handler.GetCallCount("application-selection-dialog:sample.application-selection-dialog/5"));
    }

    private static void AssertSelectionIntegrityPrompts(ApplicationSelectionDialog selectionDialog)
    {
        foreach (var promptId in SelectionIntegrityPromptIds)
        {
            Assert.Single(selectionDialog.Prompts, prompt => prompt.PromptId == promptId);
        }
    }

    private static FormsCatalogTypedClient CreateClient(HttpMessageHandler handler)
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
        return new FormsCatalogTypedClient(
            new FormsCatalogDocumentResourceClient(
                resources,
                jsonReader),
            new ApplicationSelectionDialogResourceClient(
                resources,
                jsonReader,
                preflightFailureMapper,
                cacheKeyFactory),
            new ApplicationSelectionDialogProfileResourceClient(
                resources,
                jsonReader,
                preflightFailureMapper,
                cacheKeyFactory),
            new FormsCatalogProfileReferenceCollector(),
            preflightFailureMapper);
    }
}
