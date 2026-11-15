using Sprechen.Mda.Contracts.V1.FormSurfaces;
using System.Collections.Immutable;
using Sprechen.Web.FormSurfaces;
using Xunit;

namespace Sprechen.Contracts.Tests.Services;

public sealed class FormSurfacePresenterTests
{
    private readonly FormSurfacePresenter _presenter = new();

    [Fact]
    public void PresentPreview_OrdersFieldGroupsByFieldTraversal_AndUsesFallbackMetadata()
    {
        var referenceForm = new FormSurfaceDefinition(
            entryFieldGroupId: "entry",
            fieldGroupsById: new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["entry"] = new("entry", "Entry", "First group", null),
                ["details"] = new("details", "Details", "Second group", null)
            },
            fields:
            [
                new FormSurfaceField("email", "entry", "E-Mail", "string", false, "email", true, true, null, null, variableId: "email"),
                new FormSurfaceField("city", "details", "City", "string", false, "text", true, true, null, null, variableId: "city"),
                new FormSurfaceField("other", "fallback", "Other", "string", false, "text", false, true, null, null, variableId: "other")
            ]);

        var presentation = _presenter.PresentPreview(referenceForm);

        Assert.Equal(3, presentation.FieldGroups.Length);
        Assert.Equal("entry", presentation.FieldGroups[0].FieldGroup.FieldGroupId);
        Assert.True(presentation.FieldGroups[0].IsEntryFieldGroup);
        Assert.Equal("details", presentation.FieldGroups[1].FieldGroup.FieldGroupId);
        Assert.Equal("fallback", presentation.FieldGroups[2].FieldGroup.FieldGroupId);
        Assert.Equal("fallback", presentation.FieldGroups[2].FieldGroup.Title);
    }

    [Fact]
    public void Present_MapsOptionLabels_Focus_AndStatuses()
    {
        var referenceForm = new FormSurfaceDefinition(
            entryFieldGroupId: "main",
            fieldGroupsById: new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["main"] = new("main", "Passenger details", null, null)
            },
            fields:
            [
                new FormSurfaceField("seatClass", "main", "Seat class", "enum", false, "select", true, true, null, null,
                    options:
                    [
                        new FormSurfaceOption("economy", "Economy"),
                        new FormSurfaceOption("business", "Business")
                    ],
                    variableId: "seatClass"),
                new FormSurfaceField("notes", "main", "Notes", "string", false, "text", false, true, null, null, variableId: "notes")
            ]);

        var snapshot = new FormSurfaceSnapshot(
            FormSurfaceStage.Active,
            "en",
            "travel-assistant",
            "Travel assistant",
            "passenger-details",
            "Passenger details",
            referenceForm,
            new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)
            {
                ["seatClass"] = new(FormSurfaceFieldValueKind.PublicValue, "business")
            }.ToImmutableDictionary(StringComparer.Ordinal),
            "seatClass",
            new Dictionary<string, FormSurfaceFieldStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["seatClass"] = new("seatClass", "main", true, true, false, []),
                ["notes"] = new("notes", "main", false, false, false, [])
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, FormSurfaceFieldGroupStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["main"] = new("main", true, false, [])
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));

        var presentation = _presenter.Present(snapshot);

        var group = Assert.Single(presentation.FieldGroups);
        var seatClass = group.Fields[0];
        var notes = group.Fields[1];

        Assert.Equal("Seat class", presentation.FocusedFieldLabel);
        Assert.True(seatClass.IsFocused);
        Assert.True(seatClass.HasDisplayValue);
        Assert.Equal("Business", seatClass.DisplayValue);
        Assert.NotNull(seatClass.Status);
        Assert.False(seatClass.Status!.Completed);
        Assert.False(notes.HasDisplayValue);
        Assert.NotNull(group.Status);
        Assert.False(group.Status!.Completed);
    }

    [Fact]
    public void FormDisplayValuePresenter_MapsDelimitedOptionValuesAndKeepsUnknownTokens()
    {
        var field = new FormSurfaceField("seatClass", "main", "Seat class", "enum", false, "select", true, true, null, null,
            options:
            [
                new FormSurfaceOption("economy", "Economy"),
                new FormSurfaceOption("business", "Business")
            ],
            variableId: "seatClass");
        var values = new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)
        {
            ["seatClass"] = new(FormSurfaceFieldValueKind.PublicValue, "business|premium")
        }.ToImmutableDictionary(StringComparer.Ordinal);

        var displayValue = new FormDisplayValuePresenter().TryResolveDisplayValue(field, values);

        Assert.Equal("Business, premium", displayValue);
    }

    [Fact]
    public void FormFocusedFieldLabelPresenter_WhenFocusedFieldMissing_ReturnsFocusedFieldId()
    {
        var referenceForm = new FormSurfaceDefinition(
            entryFieldGroupId: "main",
            fieldGroupsById: new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase),
            fields: []);
        var snapshot = new FormSurfaceSnapshot(
            FormSurfaceStage.Active,
            "en",
            "travel-assistant",
            "Travel assistant",
            "passenger-details",
            "Passenger details",
            referenceForm,
            new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal),
            "missing",
            new Dictionary<string, FormSurfaceFieldStatus>(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, FormSurfaceFieldGroupStatus>(StringComparer.OrdinalIgnoreCase));

        var label = new FormFocusedFieldLabelPresenter().Resolve(snapshot);

        Assert.Equal("missing", label);
    }
}



