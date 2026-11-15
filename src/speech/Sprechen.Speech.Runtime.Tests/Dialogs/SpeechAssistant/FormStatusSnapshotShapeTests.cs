using WireStatus = Sprechen.Mda.Contracts.V1.Status;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormStatusSnapshotShapeTests
{
    [Fact]
    public void FormStatusSnapshot_UsesStructuredNavigationAndProgressShape()
    {
        var snapshotProperties = typeof(FormStatusSnapshot)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "CurrentFieldGroupTitle",
                "CurrentFieldIndex",
                "CurrentFieldLabel",
                "Navigation",
                "OpenRequiredFieldCount",
                "Progress",
                "ValidationErrorCount"
            ],
            snapshotProperties);
        Assert.DoesNotContain("FieldId", snapshotProperties);
        Assert.DoesNotContain("CompletedFields", snapshotProperties);
    }

    [Fact]
    public void FormStatusNavigationPosition_MatchesSchemaAlignedMembers()
    {
        var propertyNames = typeof(FormStatusNavigationPosition)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "CurrentFieldGroupId",
                "CurrentFieldGroupIndex",
                "CurrentFieldId",
                "CurrentFormId",
                "CurrentFormIndex",
                "TotalFieldGroups",
                "TotalForms"
            ],
            propertyNames);
    }

    [Fact]
    public void FormStatusCompletionProgress_MatchesSchemaAlignedDimensions()
    {
        var propertyNames = typeof(FormStatusCompletionProgress)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "AllFields",
                "FieldGroupBased",
                "RequiredFieldsOnly"
            ],
            propertyNames);
    }

    [Fact]
    public void FormStatusProgressDimension_MatchesCompletedTotalShape()
    {
        var propertyNames = typeof(FormStatusProgressDimension)
            .GetProperties()
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "Completed",
                "Total"
            ],
            propertyNames);
    }

    [Fact]
    public void FormStatusNavigationPosition_MatchesWireDtoMembers_Bidirectional()
    {
        AssertMembersMatchWireDto<FormStatusNavigationPosition, WireStatus.NavigationPosition>();
    }

    [Fact]
    public void FormStatusCompletionProgress_MatchesWireDtoMembers_Bidirectional()
    {
        AssertMembersMatchWireDto<FormStatusCompletionProgress, WireStatus.CompletionProgress>();
        AssertMembersMatchWireDto<FormStatusProgressDimension, WireStatus.ProgressDimension>();
    }

    [Fact]
    public void FormStatusSnapshot_ProjectsToStatusAnnouncementWireFormat()
    {
        var snapshot = new FormStatusSnapshot(
            Navigation: new FormStatusNavigationPosition(
                CurrentFormId: "main",
                CurrentFormIndex: 1,
                TotalForms: 2,
                CurrentFieldGroupId: "address",
                CurrentFieldGroupIndex: 2,
                TotalFieldGroups: 4,
                CurrentFieldId: null),
            Progress: new FormStatusCompletionProgress(
                AllFields: new FormStatusProgressDimension(3, 9),
                RequiredFieldsOnly: new FormStatusProgressDimension(2, 5),
                FieldGroupBased: new FormStatusProgressDimension(1, 2)),
            CurrentFieldLabel: "IBAN",
            CurrentFieldIndex: 3,
            CurrentFieldGroupTitle: "Adresse",
            OpenRequiredFieldCount: 3,
            ValidationErrorCount: 1);

        var wire = snapshot.ToStatusAnnouncement();

        Assert.Equal("main", wire.Navigation.CurrentFormId);
        Assert.Equal(1, wire.Navigation.CurrentFormIndex);
        Assert.Equal(2, wire.Navigation.TotalForms);
        Assert.Equal("address", wire.Navigation.CurrentFieldGroupId);
        Assert.Equal(2, wire.Navigation.CurrentFieldGroupIndex);
        Assert.Equal(4, wire.Navigation.TotalFieldGroups);
        Assert.Null(wire.Navigation.CurrentFieldId);
        Assert.Equal(3, wire.Progress.AllFields!.Completed);
        Assert.Equal(9, wire.Progress.AllFields.Total);
        Assert.Equal(2, wire.Progress.RequiredFieldsOnly!.Completed);
        Assert.Equal(5, wire.Progress.RequiredFieldsOnly.Total);
        Assert.Equal(1, wire.Progress.FieldGroupBased!.Completed);
        Assert.Equal(2, wire.Progress.FieldGroupBased.Total);
        Assert.Equal(3, wire.OpenRequiredFieldCount);
        Assert.Equal(1, wire.ValidationErrorCount);

        // Zusätzliche Daten für Ansagen gehören nicht in den Übertragungsvertrag.
        Assert.DoesNotContain(
            typeof(WireStatus.StatusAnnouncement).GetProperties(),
            property => property.Name is "CurrentFieldLabel" or "CurrentFieldIndex" or "CurrentFieldGroupTitle");
    }

    private static void AssertMembersMatchWireDto<TSnapshot, TWireDto>()
    {
        var snapshotNames = typeof(TSnapshot)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);
        var wireNames = typeof(TWireDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(snapshotNames.Order(StringComparer.Ordinal), wireNames.Order(StringComparer.Ordinal));
    }
}
