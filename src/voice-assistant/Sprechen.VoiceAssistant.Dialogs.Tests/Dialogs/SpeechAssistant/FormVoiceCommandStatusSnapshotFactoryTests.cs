using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandStatusSnapshotFactoryTests
{
    [Fact]
    public void Enrich_WhenApplicationHasMultipleForms_AddsCurrentFormNavigation()
    {
        var application = CreateApplication(("form1", "Erstes Formular"), ("form2", "Zweites Formular"));
        var currentForm = application.Forms[1];
        var snapshot = CreateSnapshot();

        var enriched = FormVoiceCommandStatusSnapshotFactory.Enrich(application, currentForm, snapshot);

        Assert.Equal("form2", enriched.Navigation.CurrentFormId);
        Assert.Equal(2, enriched.Navigation.CurrentFormIndex);
        Assert.Equal(2, enriched.Navigation.TotalForms);
    }

    [Fact]
    public void Enrich_WhenApplicationHasSingleForm_LeavesFormNavigationAbsent()
    {
        var application = CreateApplication(("form1", "Einziges Formular"));
        var snapshot = CreateSnapshot() with
        {
            Navigation = CreateSnapshot().Navigation with
            {
                CurrentFormId = "stale",
                CurrentFormIndex = 9,
                TotalForms = 9
            }
        };

        var enriched = FormVoiceCommandStatusSnapshotFactory.Enrich(application, application.Forms[0], snapshot);

        Assert.Null(enriched.Navigation.CurrentFormId);
        Assert.Null(enriched.Navigation.CurrentFormIndex);
        Assert.Null(enriched.Navigation.TotalForms);
    }

    private static ApplicationDescriptor CreateApplication(params (string FormId, string DisplayName)[] forms)
    {
        return new ApplicationDescriptor
        {
            ApplicationId = "app",
            DefaultLanguage = "de",
            SupportedLanguages = ["de"],
            Forms = forms.Select(form => new FormDescriptor
            {
                FormId = FormId.From(form.FormId),
                DisplayName = new LocalizedText { ["de"] = form.DisplayName }
            }).ToList()
        };
    }

    private static FormStatusSnapshot CreateSnapshot()
    {
        return new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: null,
                CurrentFormIndex: null,
                TotalForms: null,
                CurrentFieldGroupId: "main",
                CurrentFieldGroupIndex: 1,
                TotalFieldGroups: 2,
                CurrentFieldId: "field-1"),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(1, 4),
                RequiredFieldsOnly: new FormStatusProgressDimension(1, 2),
                FieldGroupBased: new FormStatusProgressDimension(0, 2)),
            CurrentFieldLabel: "Vorname",
            CurrentFieldIndex: 1,
            CurrentFieldGroupTitle: "Persoenliche Daten",
            OpenRequiredFieldCount: 1,
            ValidationErrorCount: 1);
    }
}
