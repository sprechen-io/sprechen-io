using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Trennung zwischen kurzem Einstiegshinweis und ausführlichem Hinweis zum Befehlsanker.
/// Die Orientierung enthält die Einladung zum Auflisten von Befehlen; der separate Hilfetext erklärt weiterhin den Anker.
/// </summary>
public sealed class FormOrientationTrimTests
{
    [Fact]
    public void Orientation_OmitsAnchorHint_While_Help_Retains_It()
    {
        var application = CreateApplicationWithForm("main", "Hauptformular");
        var form = application.Forms[0];
        var contract = CreateContract();

        var orientation = FormOrientationPromptBuilder.BuildOrientationPrompt(
            application,
            form,
            contract,
            CreateTwoGroupDiscovery(),
            CreateEntryStatus(allFilled: 0, allTotal: 6, groupFilled: 0, groupTotal: 2),
            "de",
            CreateVoiceSettings());
        var helpAnchorHint = FormCommandAnchorHintPromptBuilder.BuildPrompt(contract, "de", PolitenessLevel.Formal);

        // Die Orientierung nennt den Hilfe-Befehl und die Befehlsliste, aber nicht den vollständigen Ankerhinweis.
        Assert.DoesNotContain(helpAnchorHint, orientation, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", orientation, StringComparison.Ordinal);
        Assert.Contains("Okay Alex welche Befehle", orientation, StringComparison.Ordinal);

        Assert.Contains("Okay Alex", helpAnchorHint, StringComparison.Ordinal);
    }

    private static ApplicationDescriptor CreateApplicationWithForm(string formId, string name)
        => new()
        {
            ApplicationId = "test-app",
            DefaultLanguage = "de",
            SupportedLanguages = ["de", "en"],
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From(formId),
                    DisplayName = new LocalizedText { ["de"] = name, ["en"] = name }
                }
            ]
        };

    private static FormContract CreateContract()
        => new()
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };

    private static FormDiscoverySnapshot CreateTwoGroupDiscovery()
        => new(
            CurrentFieldGroupId: "personal",
            CurrentFieldGroupTitle: "Persönliche Daten",
            FieldGroups:
            [
                new FormFieldGroupDiscoveryItem("personal", "Persönliche Daten", 1, IsCurrent: true),
                new FormFieldGroupDiscoveryItem("address", "Adresse", 2, IsCurrent: false)
            ],
            CurrentFieldGroupFields:
            [
                new FormFieldDiscoveryItem("street", "personal", "Straße", 1, IsCurrent: true)
            ]);

    private static FormStatusSnapshot CreateEntryStatus(int allFilled, int allTotal, int groupFilled, int groupTotal)
        => new(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "personal",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "street"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(allFilled, allTotal),
                RequiredFieldsOnly: new FormStatusProgressDimension(0, 0),
                FieldGroupBased: new FormStatusProgressDimension(groupFilled, groupTotal)),
            CurrentFieldLabel: "Straße",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Persönliche Daten",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);

    private static VoiceDialogSettings CreateVoiceSettings()
        => new()
        {
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = ProgressCalculationBasis.AllFields
        };
}
