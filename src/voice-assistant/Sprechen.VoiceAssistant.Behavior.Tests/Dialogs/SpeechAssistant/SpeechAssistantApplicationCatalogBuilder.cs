using Sprechen.Mda.Contracts.V1.Catalog;
using CatalogLocalizedText = Sprechen.Mda.Contracts.V1.LocalizedText;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt Katalogdaten für Tests der Anwendungsauswahl.
/// Die Formularauswahl liegt im getrennten SpeechAssistantCatalogBuilder, damit die Ansagen beider Auswahlschritte unabhängig vorbereitet werden können.
/// </summary>
internal static class SpeechAssistantApplicationCatalogBuilder
{
    internal static FormsCatalog CreateCatalog(VoiceDialogSettings settings, params ApplicationDescriptor[] applications)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(applications);

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = settings,
            SelectionDialog = CreateSelectionDialog(),
            Applications = [.. applications]
        };
    }

    internal static ApplicationDescriptor CreateApplication(string id, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        return new ApplicationDescriptor
        {
            ApplicationId = id,
            DisplayName = new CatalogLocalizedText { ["de"] = displayName },
            Description = new CatalogLocalizedText { ["de"] = $"{displayName} Beschreibung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("main-form"),
                    DisplayName = new CatalogLocalizedText { ["de"] = "Hauptformular" },
                    Description = new CatalogLocalizedText { ["de"] = "Hauptformular" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };
    }

    internal static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingSingle, ("de", "Es gibt eine Anwendung: {{name}}. Sagen Sie den Namen oder Hilfe.")),
                CreatePrompt(CatalogDialogPromptIds.AppAnnouncingMultiple, ("de", "Es gibt {{count}} Anwendungen. Ich nenne sie jetzt.")),
                CreatePrompt(CatalogDialogPromptIds.AppListIntro, ("de", "Verfügbare Anwendungen: {{count}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListItem, ("de", "Nummer {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppListSelectPrompt, ("de", "Sagen Sie die Nummer oder den Namen.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsIntro, ("de", "Gefunden: {{count}} Anwendungen für {{query}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppSearchResultsItem, ("de", "Nummer {{number}}: {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmation, ("de", "{{name}}. Ist das richtig?")),
                CreatePrompt(CatalogDialogPromptIds.AppConfirmationRepeat, ("de", "Bitte sagen Sie ja oder nein.")),
                CreatePrompt(CatalogDialogPromptIds.AppLaunch, ("de", "Starte {{name}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppHelp, ("de", "Sagen Sie alle Anwendungen, Suche nach, Name, Nummer, wiederholen oder abbrechen.")),
                CreatePrompt(CatalogDialogPromptIds.AppCancelled, ("de", "Anwendungsauswahl abgebrochen.")),
                CreatePrompt(CatalogDialogPromptIds.AppChangeLanguage, ("de", "Sagen Sie Sprache wechseln plus eine unterstützte Sprache.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoInput, ("de", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal.")),
                CreatePrompt(
                    CatalogDialogPromptIds.AppReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoApplications, ("de", "Keine Anwendungen verfügbar.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoSearchResults, ("de", "Keine Anwendungen für {{query}} gefunden.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoMatch, ("de", "{{name}} wurde nicht gefunden.")),
                CreatePrompt(CatalogDialogPromptIds.AppInvalidNumber, ("de", "Bitte wählen Sie eine Zahl zwischen 1 und {{max}}.")),
                CreatePrompt(CatalogDialogPromptIds.AppNoDescription, ("de", "Keine Beschreibung verfügbar."))
            ],
            Intents = [.. SpeechAssistantCatalogIntentHints.CreateIntents()],
            NumberWords = [.. SpeechAssistantCatalogIntentHints.CreateNumberWords()]
        };
    }

    private static CatalogVoicePrompt CreatePrompt(string promptId, params (string LanguageCode, string Text)[] entries)
    {
        var localizedText = new CatalogLocalizedText();
        foreach (var (languageCode, text) in entries)
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
