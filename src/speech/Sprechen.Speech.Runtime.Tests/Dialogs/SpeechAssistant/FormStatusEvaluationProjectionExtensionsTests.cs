using System.Collections.Immutable;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Feldzuordnung und Normalisierung der Dialogstatusdaten für die Telemetrie.
/// Unterschiedliche Werte je Quellfeld machen Vertauschungen sichtbar. Enthalten sind
/// positive Werte, Begrenzung auf null sowie Feld- und Feldgruppenstatus.
/// </summary>
public sealed class FormStatusEvaluationProjectionExtensionsTests
{
    [Fact]
    public void ToEvaluationStatusSnapshot_RoutesEveryConsumedFieldToItsProjectionSlot()
    {
        var snapshot = new FormStatusSnapshot(
            new FormStatusNavigationPosition(
                CurrentFormId: "form-1",
                CurrentFormIndex: 0,
                TotalForms: 1,
                CurrentFieldGroupId: "group-3",
                CurrentFieldGroupIndex: 2,
                TotalFieldGroups: 4,
                CurrentFieldId: "field-7"),
            new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(Completed: 5, Total: 9),
                RequiredFieldsOnly: new FormStatusProgressDimension(Completed: 2, Total: 4),
                FieldGroupBased: new FormStatusProgressDimension(Completed: 1, Total: 3)),
            CurrentFieldLabel: "ignored-label",
            CurrentFieldIndex: 3,
            CurrentFieldGroupTitle: "ignored-title",
            OpenRequiredFieldCount: 8,
            ValidationErrorCount: 6);

        var projection = snapshot.ToEvaluationStatusSnapshot();

        Assert.Equal("field-7", projection.Navigation.CurrentFieldId);
        Assert.Equal("group-3", projection.Navigation.CurrentFieldGroupId);
        // Der Feldindex stammt aus CurrentFieldIndex, nicht aus dem Feldgruppenindex.
        Assert.Equal(3, projection.Navigation.Index);
        // Die Gesamtzahl stammt aus Progress.AllFields.Total, nicht aus Index oder Feldgruppenzahl.
        Assert.Equal(9, projection.Navigation.Total);
        Assert.Equal(5, projection.Progress.AllFields.Completed);
        Assert.Equal(9, projection.Progress.AllFields.Total);
        Assert.Equal(2, projection.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(4, projection.Progress.RequiredFieldsOnly.Total);
        Assert.Equal(1, projection.Progress.FieldGroupBased.Completed);
        Assert.Equal(3, projection.Progress.FieldGroupBased.Total);
    }

    [Fact]
    public void ToEvaluationStatusSnapshot_ClampsNegativeIndexAndCountsToZero()
    {
        var snapshot = new FormStatusSnapshot(
            new FormStatusNavigationPosition(null, null, null, "group-1", 0, 1, CurrentFieldId: null),
            new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(Completed: -2, Total: -5),
                RequiredFieldsOnly: new FormStatusProgressDimension(Completed: -1, Total: -1),
                FieldGroupBased: new FormStatusProgressDimension(Completed: 0, Total: 0)),
            CurrentFieldLabel: "x",
            CurrentFieldIndex: -1,
            CurrentFieldGroupTitle: "y",
            OpenRequiredFieldCount: 0,
            ValidationErrorCount: 0);

        var projection = snapshot.ToEvaluationStatusSnapshot();

        Assert.Equal(0, projection.Navigation.Index);
        Assert.Equal(0, projection.Navigation.Total);
        Assert.Equal(0, projection.Progress.AllFields.Completed);
        Assert.Equal(0, projection.Progress.AllFields.Total);
        Assert.Equal(0, projection.Progress.RequiredFieldsOnly.Completed);
        Assert.Equal(0, projection.Progress.RequiredFieldsOnly.Total);
        Assert.Null(projection.Navigation.CurrentFieldId);
    }

    [Fact]
    public void ToEvaluationDetailedStatus_ProjectsEveryFieldAndFieldGroupValue()
    {
        var snapshot = new FormDetailedStatusSnapshot(
            [
                new FormFieldStatusSnapshot(
                    FieldId: "f1",
                    FieldGroupId: "g1",
                    Relevant: true,
                    Required: false,
                    Completed: true,
                    BlockingErrorCodes: ["E1", "E2"]),
            ],
            [
                new FormFieldGroupStatusSnapshot(
                    FieldGroupId: "g1",
                    Relevant: false,
                    Completed: true,
                    OpenGroupErrorCodes: ["G1"]),
            ]);

        var projection = snapshot.ToEvaluationDetailedStatus();

        var field = Assert.Single(projection.Fields);
        Assert.Equal("f1", field.FieldId);
        Assert.Equal("g1", field.FieldGroupId);
        Assert.True(field.Relevant);
        Assert.False(field.Required);
        Assert.True(field.Completed);
        Assert.Equal(new[] { "E1", "E2" }, field.BlockingErrorCodes.ToArray());

        var group = Assert.Single(projection.FieldGroups);
        Assert.Equal("g1", group.FieldGroupId);
        Assert.False(group.Relevant);
        Assert.True(group.Completed);
        Assert.Equal(new[] { "G1" }, group.OpenGroupErrorCodes.ToArray());
    }
}
