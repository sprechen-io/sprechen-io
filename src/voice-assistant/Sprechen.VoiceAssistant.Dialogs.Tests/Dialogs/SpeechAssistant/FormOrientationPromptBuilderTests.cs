using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormOrientationPromptBuilderTests
{
    [Fact]
    public void BuildOrientationPrompt_ComposesFrameThenGroupsThenProgressThenCommandsPointer()
    {
        var application = CreateApplicationWithForm("main", "Hauptformular");
        var form = application.Forms[0];

        var result = FormOrientationPromptBuilder.BuildOrientationPrompt(
            application,
            form,
            CreateContract(),
            CreateTwoGroupDiscovery(),
            CreateEntryStatus(allFilled: 0, allTotal: 6, groupFilled: 0, groupTotal: 2),
            "de",
            CreateVoiceSettings());

        Assert.StartsWith("Formular Hauptformular.", result, StringComparison.Ordinal);
        Assert.Contains("Es gibt 2 relevante Feldgruppen", result, StringComparison.Ordinal);
        Assert.Contains("Eingabefelder abgeschlossen 0 von 6", result, StringComparison.Ordinal);
        Assert.Contains("welche Befehle", result, StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", result, StringComparison.Ordinal);
        // Der Einstiegstext bleibt auf Formular, Gruppen, Fortschritt und Befehlshinweis
        // begrenzt. Die Erläuterung des Befehlspräfixes gehört zur Hilfe.
        Assert.DoesNotContain("Okay Guide", result, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildOrientationSegments_SplitsStateFromCommandsPointer()
    {
        var application = CreateApplicationWithForm("main", "Hauptformular");
        var form = application.Forms[0];

        var segments = FormOrientationPromptBuilder.BuildOrientationSegments(
            application,
            form,
            CreateContract(),
            CreateTwoGroupDiscovery(),
            CreateEntryStatus(allFilled: 0, allTotal: 6, groupFilled: 0, groupTotal: 2),
            "de",
            CreateVoiceSettings());

        // Zwei Textsegmente trennen die Zustandsübersicht vom Befehlshinweis.
        // So kann die Sprachausgabe dazwischen pausieren; eine hörbare Pause prüft dieser Test nicht.
        Assert.Equal(2, segments.Count);
        Assert.StartsWith("Formular Hauptformular.", segments[0], StringComparison.Ordinal);
        Assert.Contains("Es gibt 2 relevante Feldgruppen", segments[0], StringComparison.Ordinal);
        Assert.Contains("Eingabefelder abgeschlossen 0 von 6", segments[0], StringComparison.Ordinal);
        Assert.Contains("welche Befehle", segments[1], StringComparison.Ordinal);
        Assert.Contains("Okay Alex Hilfe", segments[1], StringComparison.Ordinal);
        Assert.DoesNotContain("zuerst Okay Alex", segments[1], StringComparison.Ordinal);
        Assert.DoesNotContain("Okay Guide", string.Join(" ", segments), StringComparison.Ordinal);
    }

    [Fact]
    public void BuildOrientationPrompt_ForcesAllFieldsProgress_EvenWhenSettingsConfigureFieldGroupBased()
    {
        var application = CreateApplicationWithForm("main", "Hauptformular");
        var form = application.Forms[0];

        var result = FormOrientationPromptBuilder.BuildOrientationPrompt(
            application,
            form,
            CreateContract(),
            CreateTwoGroupDiscovery(),
            CreateEntryStatus(allFilled: 0, allTotal: 6, groupFilled: 3, groupTotal: 3),
            "de",
            CreateVoiceSettings(progressBasis: ProgressCalculationBasis.FieldGroupBased));

        Assert.Contains("Eingabefelder abgeschlossen 0 von 6", result, StringComparison.Ordinal);
        Assert.DoesNotContain("3 von 3", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildOrientationPrompt_WhenStatusNull_ReturnsEmpty()
    {
        var application = CreateApplicationWithForm("main", "Hauptformular");
        var form = application.Forms[0];

        var result = FormOrientationPromptBuilder.BuildOrientationPrompt(
            application,
            form,
            CreateContract(),
            CreateTwoGroupDiscovery(),
            status: null!,
            "de",
            CreateVoiceSettings());

        Assert.Equal(string.Empty, result);
    }

    private static ApplicationDescriptor CreateApplicationWithForm(string formId, string name)
    {
        return new ApplicationDescriptor
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
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };
    }

    private static FormDiscoverySnapshot CreateTwoGroupDiscovery()
    {
        return new FormDiscoverySnapshot(
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
    }

    private static FormStatusSnapshot CreateEntryStatus(int allFilled, int allTotal, int groupFilled, int groupTotal)
    {
        return new FormStatusSnapshot(
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
    }

    private static VoiceDialogSettings CreateVoiceSettings(
        ProgressCalculationBasis progressBasis = ProgressCalculationBasis.AllFields)
    {
        return new VoiceDialogSettings
        {
            PolitenessLevel = PolitenessLevel.Formal,
            ProgressCalculationBasis = progressBasis
        };
    }
}
