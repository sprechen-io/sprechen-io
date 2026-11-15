using Sprechen.Mda.Contracts.V1.Catalog;
using CatalogLocalizedText = Sprechen.Mda.Contracts.V1.LocalizedText;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

internal sealed class SpeechAssistantCatalogBuilder
{
    internal FormsCatalog CreateMultiFormCatalog(
        string applicationId,
        string defaultLanguage,
        VoiceDialogSettings voiceDialogSettings,
        params (string FormId, string DisplayName)[] forms)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultLanguage);
        ArgumentNullException.ThrowIfNull(voiceDialogSettings);

        var formDescriptors = forms
            .Select(form => new FormDescriptor
            {
                FormId = FormId.From(form.FormId),
                DisplayName = new CatalogLocalizedText { [defaultLanguage] = form.DisplayName },
                Description = new CatalogLocalizedText { [defaultLanguage] = form.DisplayName },
                LatestContractVersion = "1",
                AvailableContractVersions = ["1"]
            })
            .ToList();

        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            VoiceDialogSettings = voiceDialogSettings,
            SelectionDialog = CreateSelectionDialog(),
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = applicationId,
                    DisplayName = new CatalogLocalizedText { [defaultLanguage] = applicationId },
                    Description = new CatalogLocalizedText { [defaultLanguage] = applicationId },
                    SupportedLanguages = [defaultLanguage],
                    DefaultLanguage = defaultLanguage,
                    Forms = formDescriptors
                }
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de", "en"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Prompts =
            [
                CreatePrompt(
                    CatalogDialogPromptIds.FormAnnouncing,
                    ("de@formal", "Diese Anwendung hat {{count}} Formulare. Sagen Sie alle Formulare fuer eine Liste."),
                    ("de@informal", "Diese Anwendung hat {{count}} Formulare. Sag alle Formulare fuer eine Liste."),
                    ("en", "This application has {{count}} forms. Say all forms for a list.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormSingle,
                    ("de", "Starte Formular {{name}}."),
                    ("en", "Starting form {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListIntro,
                    ("de", "Verfuegbare Formulare: {{count}}."),
                    ("en", "Available forms: {{count}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListItem,
                    ("de", "Nummer {{number}}: {{name}}."),
                    ("en", "Number {{number}}: {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormListSelectPrompt,
                    ("de", "Sagen Sie die Nummer oder den Namen."),
                    ("en", "Say the number or name.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormConfirmation,
                    ("de", "{{name}}. Ist das richtig?"),
                    ("en", "{{name}}. Is that correct?")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormLowConfidenceConfirm,
                    ("de", "Ich habe {{name}} verstanden. Ist das richtig?"),
                    ("en", "I understood {{name}}. Is that correct?")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormExplicitAuthorization,
                    ("de@formal", "Ich habe Ihr „Ja“ verstanden, aber nicht sicher genug, um {{name}} zu öffnen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie {{name}}“ oder „Nummer {{number}}“. Sie können auch „nein“ oder „abbrechen“ sagen."),
                    ("de@informal", "Ich habe dein „Ja“ verstanden, aber nicht sicher genug, um {{name}} zu öffnen. Sag bitte noch einmal „ja“. Alternativ sag „Öffne {{name}}“ oder „Nummer {{number}}“. Du kannst auch „nein“ oder „abbrechen“ sagen."),
                    ("en", "I heard your yes, but not clearly enough to open {{name}}. Please say yes again. Alternatively, say open {{name}} or number {{number}}. You can also say no or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormVerificationUnavailable,
                    ("de@formal", "Ich habe Ihr „Ja“ verstanden, konnte die Erkennung aber technisch nicht sicher prüfen. Sagen Sie bitte noch einmal „ja“. Alternativ sagen Sie „Öffnen Sie {{name}}“ oder „Nummer {{number}}“. Sie können auch „nein“ oder „abbrechen“ sagen."),
                    ("de@informal", "Ich habe dein „Ja“ verstanden, konnte die Erkennung aber technisch nicht sicher prüfen. Sag bitte noch einmal „ja“. Alternativ sag „Öffne {{name}}“ oder „Nummer {{number}}“. Du kannst auch „nein“ oder „abbrechen“ sagen."),
                    ("en", "I heard your yes, but could not verify recognition reliably. Please say yes again. Alternatively, say open {{name}} or number {{number}}. You can also say no or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormInitialConfirmationHelp,
                    ("de@formal", "Sie bestätigen das Formular {{name}}. Sagen Sie „ja“, um es zu öffnen, „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("de@informal", "Du bestätigst das Formular {{name}}. Sag „ja“, um es zu öffnen, „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("en", "You are confirming the form {{name}}. Say yes to open it, no to return to selection, or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormExplicitAuthorizationHelp,
                    ("de@formal", "Sie bestätigen das Formular {{name}}. Sagen Sie „ja“, „Öffnen Sie {{name}}“ oder „Nummer {{number}}“, um es zu öffnen. Sagen Sie „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("de@informal", "Du bestätigst das Formular {{name}}. Sag „ja“, „Öffne {{name}}“ oder „Nummer {{number}}“, um es zu öffnen. Sag „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("en", "You are confirming the form {{name}}. Say yes, open {{name}}, or number {{number}} to open it. Say no to return to selection, or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormConfidenceFaultRetryHelp,
                    ("de@formal", "Die Erkennung konnte Ihr „Ja“ nicht sicher prüfen. Sagen Sie „ja“, „Öffnen Sie {{name}}“ oder „Nummer {{number}}“, um das Formular zu öffnen. Sagen Sie „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("de@informal", "Die Erkennung konnte dein „Ja“ nicht sicher prüfen. Sag „ja“, „Öffne {{name}}“ oder „Nummer {{number}}“, um das Formular zu öffnen. Sag „nein“, um zur Auswahl zurückzukehren, oder „abbrechen“."),
                    ("en", "Recognition could not reliably verify your yes. Say yes, open {{name}}, or number {{number}} to open the form. Say no to return to selection, or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNotOpenedReselect,
                    ("de@formal", "Das Formular {{name}} wurde nicht geöffnet. Wählen Sie erneut per Name oder Nummer, oder sagen Sie „abbrechen“."),
                    ("de@informal", "Das Formular {{name}} wurde nicht geöffnet. Wähle erneut per Name oder Nummer, oder sag „abbrechen“."),
                    ("en", "The form {{name}} was not opened. Choose again by name or number, or say cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormVerificationFailedTerminal,
                    ("de", "Bestätigungen können derzeit nicht zuverlässig geprüft werden. Das Formular {{name}} wurde nicht geöffnet. Die Formularauswahl ist beendet."),
                    ("en", "Confirmations cannot currently be verified reliably. The form {{name}} was not opened. Form selection has ended.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormRepairExhaustedTerminal,
                    ("de", "Das Formular {{name}} konnte nicht eindeutig bestätigt werden und wurde nicht geöffnet. Die Formularauswahl ist beendet."),
                    ("en", "The form {{name}} could not be confirmed unambiguously and was not opened. Form selection has ended.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormCaptureFailedTerminal,
                    ("de", "Die Spracherkennung ist derzeit nicht verfügbar. Es wurde kein Formular geöffnet. Die Formularauswahl ist beendet."),
                    ("en", "Speech recognition is currently unavailable. No form was opened. Form selection has ended.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormConfirmationRepeat,
                    ("de", "Bitte sagen Sie ja oder nein."),
                    ("en", "Please say yes or no.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormLaunch,
                    ("de", "Starte {{name}}."),
                    ("en", "Starting {{name}}.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormPrerequisitesNotMet,
                    ("de", "Das Formular {{name}} benoetigt zuerst {{missingPrerequisites}}."),
                    ("en", "The form {{name}} requires {{missingPrerequisites}} first.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormHelp,
                    ("de@formal", "Sagen Sie alle Formulare, Name, Nummer, wiederholen oder abbrechen."),
                    ("de@informal", "Sag alle Formulare, Name, Nummer, wiederholen oder abbrechen."),
                    ("en", "Say all forms, name, number, repeat or cancel.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormCancelled,
                    ("de", "Formularauswahl abgebrochen."),
                    ("en", "Form selection cancelled.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoInput,
                    ("de@formal", "Ich habe nichts gehört. Bitte sprechen Sie noch einmal."),
                    ("de@informal", "Ich habe nichts gehört. Bitte sprich noch einmal."),
                    ("en", "I didn't hear anything. Please speak again.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormReListen,
                    ("de", "Verstanden. Ich höre weiter zu."),
                    ("en", "Got it. I am still listening.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoForms,
                    ("de", "Diese Anwendung hat keine Formulare."),
                    ("en", "This application has no forms.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormNoMatch,
                    ("de", "{{name}} wurde nicht gefunden."),
                    ("en", "{{name}} was not found.")),
                CreatePrompt(
                    CatalogDialogPromptIds.FormInvalidNumber,
                    ("de", "Bitte waehlen Sie eine Zahl zwischen 1 und {{max}}."),
                    ("en", "Please choose a number between 1 and {{max}}."))
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
