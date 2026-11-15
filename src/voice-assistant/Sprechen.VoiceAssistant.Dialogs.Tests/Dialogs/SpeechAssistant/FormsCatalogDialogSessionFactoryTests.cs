using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogDialogSessionFactoryTests
{
    [Fact]
    public void Create_FiltersSortsAndProjectsDialogRuntimeSettings()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Informal);
        var factory = new FormsCatalogDialogSessionFactory(userLanguageState);
        var catalog = CreateCatalog(
            new VoiceDialogSettings { ConfirmationPolicy = ConfirmationPolicy.Always },
            CreateApplication("beta", "Beta", ["de"]),
            CreateApplication("english", "English", ["en"]),
            CreateApplication("alpha", "Alpha", ["de"]));

        var session = factory.Create(catalog, "de");

        Assert.Equal(["alpha", "beta"], session.Applications.Select(application => application.Id));
        Assert.Equal(ConfirmationPolicy.Always, session.ConfirmationPolicy);
        Assert.Equal(PolitenessLevel.Informal, session.PolitenessLevel);
        Assert.Equal("de", session.IsoCode);
        Assert.Same(catalog, session.Catalog);
        Assert.Same(session.PromptContext, session.PromptRuntime.PromptContext);
    }

    private static FormsCatalog CreateCatalog(VoiceDialogSettings settings, params ApplicationDescriptor[] applications)
        => new()
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = settings,
            SelectionDialog = FormsCatalogIntentTestCatalog.Create().SelectionDialog,
            Applications = applications.ToList()
        };

    private static ApplicationDescriptor CreateApplication(
        string id,
        string displayName,
        string[] supportedLanguages)
        => new()
        {
            ApplicationId = id,
            DisplayName = new LocalizedText { ["de"] = displayName, ["en"] = displayName },
            Description = new LocalizedText { ["de"] = displayName, ["en"] = displayName },
            SupportedLanguages = supportedLanguages.ToList(),
            DefaultLanguage = supportedLanguages[0],
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From($"{id}.form"),
                    DisplayName = new LocalizedText { ["de"] = "Form", ["en"] = "Form" },
                    Description = new LocalizedText { ["de"] = "Form", ["en"] = "Form" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
}
