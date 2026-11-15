using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Contracts.ApiClient;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class FormsCatalogDataClientTests
{
    [Fact]
    public async Task GetFormsCatalogAsync_WhenCalledMultipleTimes_RefreshesPointerAndCachesImmutableArtifacts()
    {
        var document = CreateCatalogDocument();
        var selectionDialog = CreateSelectionDialog();
        var profile = CreateApplicationSelectionDialogProfile();

        var handler = new MappedContractsHttpMessageHandler();
        handler.Add("/contracts/forms-catalog", () => SerializeToStream(document));
        handler.Add(
            $"/contracts/application-selection-dialogs/{document.SelectionDialog.SelectionDialogId}/{document.SelectionDialog.ModelVersion}",
            () => SerializeToStream(selectionDialog));
        handler.Add(
            $"/contracts/application-selection-dialog-profiles/{document.SelectionDialogProfile!.ProfileId}/{document.SelectionDialogProfile.ProfileVersion}",
            () => SerializeToStream(profile));

        using var provider = CreateContractsApiClientProvider(handler);
        using var scope = provider.CreateScope();
        var client = new FormsCatalogDataClient(scope.ServiceProvider.GetRequiredService<FormsCatalogTypedClient>());

        var first = await client.GetFormsCatalogAsync(CancellationToken.None);
        var second = await client.GetFormsCatalogAsync(CancellationToken.None);

        Assert.NotSame(first, second);
        Assert.Equal("1", first.SchemaVersion);
        Assert.Equal("1", first.CatalogVersion);
        Assert.Equal("sample.checkout", first.Applications.Single().ApplicationId);
        Assert.Equal(3, first.VoiceDialogSettings?.MaxRetries);

        Assert.Equal(2, handler.GetCallCount("/contracts/forms-catalog"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/application-selection-dialogs/{document.SelectionDialog.SelectionDialogId}/{document.SelectionDialog.ModelVersion}"));
        Assert.Equal(
            1,
            handler.GetCallCount($"/contracts/application-selection-dialog-profiles/{document.SelectionDialogProfile!.ProfileId}/{document.SelectionDialogProfile.ProfileVersion}"));
    }

    private static FormsCatalogDocument CreateCatalogDocument()
    {
        return new FormsCatalogDocument
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            GeneratedAt = "2026-01-01T00:00:00Z",
            SelectionDialog = new ApplicationSelectionDialogReference
            {
                SelectionDialogId = "sample.catalog.selection",
                ModelVersion = "5"
            },
            SelectionDialogProfile = new ApplicationSelectionDialogProfileReference
            {
                ProfileId = "sample.catalog.default",
                ProfileVersion = "1"
            },
            Applications =
            [
                new ApplicationDescriptorDocument
                {
                    ApplicationId = "sample.checkout",
                    DisplayName = new LocalizedText { ["de"] = "Checkout" },
                    Description = new LocalizedText { ["de"] = "Demo" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("address-demo"),
                            DisplayName = new LocalizedText { ["de"] = "Adresse" },
                            Description = new LocalizedText { ["de"] = "Demo" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "5",
            SelectionDialogId = "sample.catalog.selection",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = false,
            Prompts =
            [
                new CatalogVoicePrompt
                {
                    PromptId = "catalog.app.help",
                    Text = new LocalizedText { ["de"] = "Hilfe." }
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.AppConfirmationRepeat,
                    Text = new LocalizedText { ["de"] = "Bitte sagen Sie ja oder nein." }
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.FormConfirmationRepeat,
                    Text = new LocalizedText { ["de"] = "Bitte sagen Sie ja oder nein." }
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.AppReListen,
                    Text = new LocalizedText { ["de"] = "Verstanden. Ich höre weiter zu." }
                },
                new CatalogVoicePrompt
                {
                    PromptId = ApplicationSelectionRequiredPromptId.FormReListen,
                    Text = new LocalizedText { ["de"] = "Verstanden. Ich höre weiter zu." }
                }
            ],
            Intents =
            [
                new CatalogVoiceIntent
                {
                    IntentId = CatalogVoiceIntentId.Help,
                    MatchMode = CatalogVoiceIntentMatchMode.Phrase,
                    Utterances = [new LocalizedText { ["de"] = "hilfe" }]
                }
            ],
            NumberWords =
            [
                new CatalogVoiceNumberWord
                {
                    Value = 1,
                    Utterances = [new LocalizedText { ["de"] = "eins" }]
                }
            ]
        };
    }

    private static ApplicationSelectionDialogProfile CreateApplicationSelectionDialogProfile()
    {
        return new ApplicationSelectionDialogProfile
        {
            SchemaVersion = "1",
            ProfileVersion = "1",
            ProfileId = "sample.catalog.default",
            MaxRetries = 3
        };
    }

    private static MemoryStream SerializeToStream<T>(T value)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));
    }

    private static ServiceProvider CreateContractsApiClientProvider(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddContractsApiClient(
            "ContractsHttp",
            (_, httpClient) => httpClient.BaseAddress = new Uri("https://contracts.test", UriKind.Absolute));
        services
            .AddHttpClient("ContractsHttp")
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider(validateScopes: true);
    }
}
