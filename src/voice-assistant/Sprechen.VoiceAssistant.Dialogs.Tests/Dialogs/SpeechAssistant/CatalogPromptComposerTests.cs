using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class CatalogPromptComposerTests
{
    [Fact]
    public void ResolvePromptOrThrow_ResolvesSupportedLanguageInThatLanguage_NeverTheForeignFallback()
    {
        // Bei strikter Lokalisierung wird der deutsche Text auch für die formelle Anrede gewählt.
        // Die vorhandene englische Variante darf ihn nicht ersetzen.
        var promptContext = CreatePromptContext(
            CreatePrompt("app.help", ("de", "Deutscher Hilfetext"), ("en", "English help")));

        var prompt = CatalogPromptComposer.ResolvePromptOrThrow(
            promptContext,
            "app.help",
            "de",
            PolitenessLevel.Formal);

        Assert.Equal("Deutscher Hilfetext", prompt);
    }

    [Fact]
    public void ResolvePromptOrThrow_FailsClosedForUnresolvableSupportedLanguagePrompt()
    {
        // Fehlt der angeforderte Hinweis, wird ein Fehler für die fehlende lokalisierte Ressource ausgelöst.
        var promptContext = CreatePromptContext();

        var exception = Assert.Throws<ModeledVoiceResourcePromptException>(() =>
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                "missing.prompt",
                "de",
                PolitenessLevel.Formal));

        Assert.Equal(ModeledVoiceResourcePromptException.MissingLocalizedTextCodeValue, exception.Code);
    }

    [Fact]
    public void BuildNumberedSequencePrompt_ComposesIntroItemsAndTrailingPrompt()
    {
        var promptContext = CreatePromptContext(
            CreatePrompt(CatalogDialogPromptIds.FormListIntro, ("de", "INTRO {{count}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListItem, ("de", "ITEM {{number}} {{name}}")),
            CreatePrompt(CatalogDialogPromptIds.FormListSelectPrompt, ("de", "TRAIL")));

        var forms = new[]
        {
            new FormInfo("a", "Alpha", []),
            new FormInfo("b", "Beta", [])
        };

        var prompt = CatalogPromptComposer.BuildNumberedSequencePrompt(
            new CatalogNumberedSequencePromptRequest<FormInfo>(
                promptContext,
                forms,
                "de",
                PolitenessLevel.Formal,
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.FormListIntro,
                    new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Count] = "2"
                    }),
                new CatalogPromptItemSegment<FormInfo>(
                    CatalogDialogPromptIds.FormListItem,
                    static (index, form) => new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Number] = (index + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [CatalogPromptTemplateParameterNames.Name] = form.DisplayName
                    }),
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.FormListSelectPrompt)));

        Assert.Equal("INTRO 2 ITEM 1 Alpha ITEM 2 Beta TRAIL", prompt);
    }

    private static CatalogPromptContext CreatePromptContext(params CatalogVoicePrompt[] prompts)
    {
        var catalog = new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = new ApplicationSelectionDialog
            {
                SchemaVersion = "1",
                ModelVersion = "1",
                SelectionDialogId = "default.selectionDialog",
                SupportedLanguages = ["de", "en", "fr", "es", "it"],
                DefaultLanguage = "de",
                StrictLocalization = true,
                Prompts = [.. prompts]
            },
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.app",
                    DisplayName = new LocalizedText { ["en"] = "Sample" },
                    Description = new LocalizedText { ["en"] = "Sample app" },
                    SupportedLanguages = ["de", "en", "fr", "es", "it"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("form-1"),
                            DisplayName = new LocalizedText { ["en"] = "Form 1" },
                            Description = new LocalizedText { ["en"] = "Description" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };

        return CatalogPromptContext.CreateForCatalog(catalog);
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId, params (string LanguageCode, string Text)[] texts)
    {
        var localizedText = new LocalizedText();
        foreach (var (languageCode, text) in texts)
        {
            localizedText[languageCode] = text;
        }

        return new CatalogVoicePrompt
        {
            PromptId = promptId,
            Text = localizedText
        };
    }
}
