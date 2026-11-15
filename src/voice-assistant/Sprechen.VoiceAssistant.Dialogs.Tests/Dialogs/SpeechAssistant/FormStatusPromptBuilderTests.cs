using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormStatusPromptBuilderTests
{
    [Fact]
    public void BuildFormListPrompt_MultipleFormsGerman_ListsAllForms()
    {
        var app = CreateApplicationWithForms(("form1", "Erstes Formular"), ("form2", "Zweites Formular"));

        var result = FormStatusPromptBuilder.BuildFormListPrompt(app, "de", CreateVoiceSettings());

        Assert.Contains("Diese Anwendung hat 2 Formulare", result);
        Assert.Contains("Nummer 1 ist Erstes Formular", result);
        Assert.Contains("Nummer 2 ist Zweites Formular", result);
    }

    [Fact]
    public void BuildFormStatusPrompt_FormalGerman_UsesFormalAddressing()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var status = CreateStatus();

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(
            app,
            form,
            status,
            "de",
            CreateVoiceSettings(politenessLevel: PolitenessLevel.Formal));

        Assert.Contains("Sie sind im Formular Hauptformular", result);
        Assert.Contains("Aktuelles Feld Vorname", result);
        Assert.Contains("Eingabefelder abgeschlossen 2 von 5", result);
    }

    [Fact]
    public void BuildFormStatusPrompt_InformalGerman_UsesInformalAddressing()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var status = CreateStatus();

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(
            app,
            form,
            status,
            "de",
            CreateVoiceSettings(politenessLevel: PolitenessLevel.Informal));

        Assert.Contains("Du bist im Formular Hauptformular", result);
    }

    [Fact]
    public void BuildFieldGroupListPrompt_German_ListsRelevantFieldGroups()
    {
        var discovery = CreateDiscoverySnapshot();

        var result = FormStatusPromptBuilder.BuildFieldGroupListPrompt(
            discovery,
            "de",
            CreateVoiceSettings());

        Assert.Contains("Es gibt 2 relevante Feldgruppen", result);
        Assert.Contains("Nummer 1 ist Persönliche Daten", result);
        Assert.Contains("Nummer 2 ist Adresse", result);
    }

    [Fact]
    public void BuildFieldListPrompt_WithModelTemplate_UsesContractPrompt()
    {
        var discovery = CreateDiscoverySnapshot();
        var contract = CreateContractWithPrompts(
            (FormDialogPromptIds.DiscoveryFieldListSummary, "MODEL {{fieldGroupTitle}} | {{count}} | {{items}}"),
            (FormDialogPromptIds.DiscoveryFieldListItem, "ITEM {{number}} {{fieldLabel}}"));

        var result = FormStatusPromptBuilder.BuildFieldListPrompt(
            discovery,
            contract,
            "de",
            CreateVoiceSettings());

        Assert.StartsWith("MODEL Adresse | 2 | ITEM 1 Straße ITEM 2 Hausnummer", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildStatusPrompt_RequiredFieldsOnly_UsesRequiredProgress()
    {
        var app = CreateApplicationWithForms(("main", "Main Form"));
        var form = app.Forms[0];
        var status = CreateStatus();

        var result = FormStatusPromptBuilder.BuildStatusPrompt(
            app,
            form,
            status,
            "en",
            CreateVoiceSettings(progressBasis: ProgressCalculationBasis.RequiredFieldsOnly));

        Assert.Contains("required fields completed 1 of 3", result);
    }

    [Fact]
    public void BuildStatusPrompt_FieldGroupBased_UsesFieldGroupProgress()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var status = CreateStatus();

        var result = FormStatusPromptBuilder.BuildStatusPrompt(
            app,
            form,
            status,
            "de",
            CreateVoiceSettings(progressBasis: ProgressCalculationBasis.FieldGroupBased));

        Assert.Contains("Feldgruppen abgeschlossen 1 von 2", result);
        Assert.Contains("Aktuelle Feldgruppe 1 von 2, Persönliche Daten", result);
    }

    [Fact]
    public void BuildStatusPrompt_French_UsesLocalizedStatusText()
    {
        var app = CreateApplicationWithForms(("main", "Formulaire principal"));
        app.DefaultLanguage = "fr";
        app.SupportedLanguages = ["fr", "en"];
        app.Forms[0].DisplayName = new LocalizedText { ["fr"] = "Formulaire principal", ["en"] = "Main form" };

        var status = new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "field-group-1",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(2, 5),
                RequiredFieldsOnly: new FormStatusProgressDimension(1, 3),
                FieldGroupBased: new FormStatusProgressDimension(1, 2)),
            CurrentFieldLabel: "Prénom",
            CurrentFieldIndex: 2,
            CurrentFieldGroupTitle: "Données personnelles",
            OpenRequiredFieldCount: 3,
            ValidationErrorCount: 1);

        var result = FormStatusPromptBuilder.BuildStatusPrompt(
            app,
            app.Forms[0],
            status,
            "fr",
            CreateVoiceSettings(progressBasis: ProgressCalculationBasis.FieldGroupBased));

        Assert.Contains("Statut", result);
        Assert.Contains("Groupe de champs actuel", result);
        Assert.Contains("sur 2", result);
    }

    [Fact]
    public void BuildStatusPrompt_RequiredFieldsOnlyWithoutRequiredFields_UsesSpecialText()
    {
        var app = CreateApplicationWithForms(("main", "Main Form"));
        var form = app.Forms[0];
        var status = new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "field-group-1",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(2, 5),
                RequiredFieldsOnly: new FormStatusProgressDimension(0, 0),
                FieldGroupBased: new FormStatusProgressDimension(0, 2)),
            CurrentFieldLabel: "First name",
            CurrentFieldIndex: 2,
            CurrentFieldGroupTitle: "Personal Data",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);

        var result = FormStatusPromptBuilder.BuildStatusPrompt(
            app,
            form,
            status,
            "en",
            CreateVoiceSettings(progressBasis: ProgressCalculationBasis.RequiredFieldsOnly));

        Assert.Contains("No relevant required fields at the moment", result);
        Assert.DoesNotContain("0 of 0", result);
    }

    [Fact]
    public void BuildStatusPrompt_NullInputs_ReturnsEmpty()
    {
        var app = CreateApplicationWithForms(("main", "Form"));
        var form = app.Forms[0];
        var status = CreateStatus();
        var settings = CreateVoiceSettings();

        Assert.Equal(string.Empty, FormStatusPromptBuilder.BuildStatusPrompt(null!, form, status, "de", settings));
        Assert.Equal(string.Empty, FormStatusPromptBuilder.BuildStatusPrompt(app, null!, status, "de", settings));
        Assert.Equal(string.Empty, FormStatusPromptBuilder.BuildStatusPrompt(app, form, null!, "de", settings));
        Assert.Equal(string.Empty, FormStatusPromptBuilder.BuildStatusPrompt(app, form, status, "", settings));
        Assert.Equal(string.Empty, FormStatusPromptBuilder.BuildStatusPrompt(app, form, status, "de", null!));
    }

    [Fact]
    public void BuildFormListPrompt_WithModelTemplates_UsesContractPrompts()
    {
        var app = CreateApplicationWithForms(("form1", "Erstes Formular"), ("form2", "Zweites Formular"));
        var contract = CreateContractWithPrompts(
            (FormDialogPromptIds.FormListItem, "Modell Nummer {{number}}: {{formName}}"),
            (FormDialogPromptIds.FormListSummary, "Modell hat {{count}} Formulare. {{items}}."));

        var result = FormStatusPromptBuilder.BuildFormListPrompt(
            app,
            contract,
            "de",
            CreateVoiceSettings());

        Assert.Contains("Modell hat 2 Formulare", result);
        Assert.Contains("Modell Nummer 1: Erstes Formular", result);
        Assert.Contains("Modell Nummer 2: Zweites Formular", result);
    }

    [Fact]
    public void BuildStatusPrompt_WithModelTemplate_UsesContractPrompt()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var status = CreateStatus();
        var contract = CreateContractWithPrompts(
            (FormDialogPromptIds.StatusSummary, "MODEL {{formName}} | {{currentField}} | {{progressText}}"));

        var result = FormStatusPromptBuilder.BuildStatusPrompt(
            app,
            form,
            status,
            contract,
            "de",
            CreateVoiceSettings());

        Assert.StartsWith("MODEL Hauptformular | Vorname | ", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFormStatusPrompt_MultiForm_UsesSnapshotFormPosition()
    {
        var app = CreateApplicationWithForms(("form1", "Erstes Formular"), ("form2", "Zweites Formular"));
        var form = app.Forms[1];
        var baseStatus = CreateStatus();
        var status = baseStatus with
        {
            Navigation = baseStatus.Navigation with
            {
                CurrentFormId = "form2",
                CurrentFormIndex = 2,
                TotalForms = 2
            }
        };

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(
            app,
            form,
            status,
            "de",
            CreateVoiceSettings());

        Assert.Contains("2 von 2", result);
    }

    [Fact]
    public void BuildFormStatusPrompt_BlankFieldGroupTitleWithoutIndex_SpeaksNoneSegmentNotId()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var baseStatus = CreateStatus();
        var status = baseStatus with
        {
            CurrentFieldGroupTitle = string.Empty,
            Navigation = baseStatus.Navigation with
            {
                CurrentFieldGroupId = "fg-personal-data",
                CurrentFieldGroupIndex = 0,
                TotalFieldGroups = 0
            }
        };

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(app, form, status, "de", CreateVoiceSettings());

        Assert.DoesNotContain("fg-personal-data", result, StringComparison.Ordinal);
        Assert.Contains("keine Feldgruppe ausgew", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFormStatusPrompt_BlankFieldGroupTitleWithIndex_SpeaksIndexOnlyNotId()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var baseStatus = CreateStatus();
        var status = baseStatus with
        {
            CurrentFieldGroupTitle = string.Empty,
            Navigation = baseStatus.Navigation with
            {
                CurrentFieldGroupId = "fg-personal-data",
                CurrentFieldGroupIndex = 1,
                TotalFieldGroups = 2
            }
        };

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(app, form, status, "de", CreateVoiceSettings());

        Assert.DoesNotContain("fg-personal-data", result, StringComparison.Ordinal);
        Assert.Contains("Aktuelle Feldgruppe 1 von 2.", result, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildFormStatusPrompt_BlankCurrentFieldLabel_SpeaksNoneValueNotFragment()
    {
        var app = CreateApplicationWithForms(("main", "Hauptformular"));
        var form = app.Forms[0];
        var status = CreateStatus() with { CurrentFieldLabel = string.Empty };

        var result = FormStatusPromptBuilder.BuildFormStatusPrompt(app, form, status, "de", CreateVoiceSettings());

        Assert.Contains("Aktuelles Feld noch kein Feld ausgew", result, StringComparison.Ordinal);
        Assert.DoesNotContain("Aktuelles Feld .", result, StringComparison.Ordinal);
        Assert.DoesNotContain("{{", result, StringComparison.Ordinal);
    }

    private static ApplicationDescriptor CreateApplicationWithForms(params (string formId, string name)[] forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "test-app",
            DefaultLanguage = "de",
            SupportedLanguages = ["de", "en"],
            Forms = forms.Select(f => new FormDescriptor
            {
                FormId = FormId.From(f.formId),
                DisplayName = new LocalizedText { ["de"] = f.name, ["en"] = f.name }
            }).ToList()
        };
    }

    private static VoiceDialogSettings CreateVoiceSettings(
        PolitenessLevel politenessLevel = PolitenessLevel.Formal,
        ProgressCalculationBasis progressBasis = ProgressCalculationBasis.AllFields)
    {
        return new VoiceDialogSettings
        {
            PolitenessLevel = politenessLevel,
            ProgressCalculationBasis = progressBasis
        };
    }

    private static FormStatusSnapshot CreateStatus()
    {
        return new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "field-group-1",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(2, 5),
                RequiredFieldsOnly: new FormStatusProgressDimension(1, 3),
                FieldGroupBased: new FormStatusProgressDimension(1, 2)),
            CurrentFieldLabel: "Vorname",
            CurrentFieldIndex: 2,
            CurrentFieldGroupTitle: "Persönliche Daten",
            OpenRequiredFieldCount: 3,
            ValidationErrorCount: 1);
    }

    private static FormDiscoverySnapshot CreateDiscoverySnapshot()
    {
        return new FormDiscoverySnapshot(
            CurrentFieldGroupId: "address",
            CurrentFieldGroupTitle: "Adresse",
            FieldGroups:
            [
                new FormFieldGroupDiscoveryItem("personal", "Persönliche Daten", 1, IsCurrent: false),
                new FormFieldGroupDiscoveryItem("address", "Adresse", 2, IsCurrent: true)
            ],
            CurrentFieldGroupFields:
            [
                new FormFieldDiscoveryItem("street", "address", "Straße", 1, IsCurrent: true),
                new FormFieldDiscoveryItem("houseNumber", "address", "Hausnummer", 2, IsCurrent: false)
            ]);
    }

    private static FormContract CreateContractWithPrompts(params (string PromptId, string DeText)[] prompts)
    {
        return new FormContract
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        Prompts = prompts.Select(prompt => new VoicePrompt
                        {
                            PromptId = prompt.PromptId,
                            Text = new LocalizedText
                            {
                                ["de"] = prompt.DeText
                            }
                        }).ToList()
                    }
                }
            }
        };
    }
}
