using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.FormSurface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.FormSurfaces;
using Sprechen.Web.SpeechComponents;
using System.Collections.Immutable;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechAssistantFormSurfaceTests : TestContext
{
    private static readonly SpeechAssistantFormSurfaceCopy Copy = new();

    [Fact]
    public void Render_WhenNoSnapshotAvailable_ShowsOwnerEmptyState()
    {
        var state = new SpeechAssistantFormSurfaceStore();
        Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        Services.AddSingleton<ISpeechAssistantFormSurfaceState>(state);
        Services.AddSingleton<ISpeechAssistantSilentInputController>(Mock.Of<ISpeechAssistantSilentInputController>());

        var cut = Render<SpeechAssistantFormSurface>();

        Assert.Contains(Copy.EmptyState.Kicker, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Copy.EmptyState.Title, cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Copy.EmptyState.Description, cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenActiveSnapshotAvailable_ShowsOwnerMetadataAndFocusedStatuses()
    {
        var state = new SpeechAssistantFormSurfaceStore();
        Publish(state, CreateSnapshot(
            stage: FormSurfaceStage.Active,
            focusedFieldId: "seatClass",
            firstNameStatus: new FormSurfaceFieldStatus("firstName", "main", relevant: true, required: true, completed: true, []),
            seatClassStatus: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: false, []),
            fieldGroupStatus: new FormSurfaceFieldGroupStatus("main", relevant: true, completed: false, []),
            fieldValuesById: new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)
            {
                ["firstName"] = new(FormSurfaceFieldValueKind.PublicValue, "Ada Lovelace"),
                ["seatClass"] = new(FormSurfaceFieldValueKind.PublicValue, "business")
            }));

        Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        Services.AddSingleton<ISpeechAssistantFormSurfaceState>(state);
        Services.AddSingleton<ISpeechAssistantSilentInputController>(Mock.Of<ISpeechAssistantSilentInputController>());

        var cut = Render<SpeechAssistantFormSurface>();

        Assert.Contains(Copy.Language.GetSummary("en"), cut.Markup, StringComparison.Ordinal);
        Assert.Contains(Copy.Language.GetFocusedFieldSummary("Seat class", "en"), cut.Markup, StringComparison.Ordinal);
        Assert.Contains($">{Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, CreatePresentationField("seatClass", required: true, isFocused: true, status: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: false, []), displayValue: "Business"), "en")}<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains($">{Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, CreatePresentationField("firstName", required: true, isFocused: false, status: new FormSurfaceFieldStatus("firstName", "main", relevant: true, required: true, completed: true, []), displayValue: "Ada Lovelace"), "en")}<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains($">{Copy.FieldGroups.GetStatusLabel(CreatePresentationFieldGroup(new FormSurfaceFieldGroupStatus("main", relevant: true, completed: false, [])), "en")}<", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenFieldAndGroupAreIrrelevant_ShowsOwnerIrrelevantStatuses()
    {
        var state = new SpeechAssistantFormSurfaceStore();
        Publish(state, CreateSnapshot(
            stage: FormSurfaceStage.Active,
            focusedFieldId: null,
            firstNameStatus: new FormSurfaceFieldStatus("firstName", "main", relevant: false, required: true, completed: false, []),
            seatClassStatus: new FormSurfaceFieldStatus("seatClass", "main", relevant: false, required: true, completed: false, []),
            fieldGroupStatus: new FormSurfaceFieldGroupStatus("main", relevant: false, completed: false, []),
            fieldValuesById: new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)));

        Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        Services.AddSingleton<ISpeechAssistantFormSurfaceState>(state);
        Services.AddSingleton<ISpeechAssistantSilentInputController>(Mock.Of<ISpeechAssistantSilentInputController>());

        var cut = Render<SpeechAssistantFormSurface>();
        var expectedStatus = Copy.Fields.GetStatusLabel(
            FormSurfaceStage.Active,
            CreatePresentationField("firstName", required: true, isFocused: false, status: new FormSurfaceFieldStatus("firstName", "main", relevant: false, required: true, completed: false, []), displayValue: null),
            "en");

        Assert.Contains($">{expectedStatus}<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(
            $">{Copy.FieldGroups.GetStatusLabel(CreatePresentationFieldGroup(new FormSurfaceFieldGroupStatus("main", relevant: false, completed: false, [])), "en")}<",
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenStageIsSubmittedAndValueMissing_ShowsOwnerTerminalStageAndPlaceholder()
    {
        var state = new SpeechAssistantFormSurfaceStore();
        Publish(state, CreateSnapshot(
            stage: FormSurfaceStage.Submitted,
            focusedFieldId: null,
            firstNameStatus: new FormSurfaceFieldStatus("firstName", "main", relevant: true, required: true, completed: true, []),
            seatClassStatus: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: true, []),
            fieldGroupStatus: new FormSurfaceFieldGroupStatus("main", relevant: true, completed: true, []),
            fieldValuesById: new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)));

        Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        Services.AddSingleton<ISpeechAssistantFormSurfaceState>(state);
        Services.AddSingleton<ISpeechAssistantSilentInputController>(Mock.Of<ISpeechAssistantSilentInputController>());

        var cut = Render<SpeechAssistantFormSurface>();

        Assert.Contains($">{Copy.Stages.GetLabel(FormSurfaceStage.Submitted, "en")}<", cut.Markup, StringComparison.Ordinal);
        Assert.Contains(
            Copy.Fields.GetValue(CreatePresentationField("seatClass", required: true, isFocused: false, status: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: true, []), displayValue: null), "en"),
            cut.Markup,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenEditingInteractionIsPublished_ShowsNativeSilentInput()
    {
        var state = new SpeechAssistantFormSurfaceStore();
        var snapshot = CreateSnapshot(
            stage: FormSurfaceStage.Active,
            focusedFieldId: "firstName",
            firstNameStatus: new FormSurfaceFieldStatus("firstName", "main", relevant: true, required: true, completed: false, []),
            seatClassStatus: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: false, []),
            fieldGroupStatus: new FormSurfaceFieldGroupStatus("main", relevant: true, completed: false, []),
            fieldValuesById: new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal));
        var session = state.BeginSession();
        Assert.True(state.TryPublishActive(
            session,
            new SpeechAssistantFormSurfaceView(
                snapshot,
                new SpeechAssistantFormSurfaceInteractionState(
                    SpeechAssistantSilentInputPhase.Editing,
                    "firstName",
                    SpeechAssistantSilentInputControlKind.Text,
                    OriginSurfaceRevision: 0,
                    RepairKind: null),
                new SpeechAssistantSilentInputSnapshotIdentity(
                    "corr",
                    "run",
                    1,
                    "travel-assistant",
                    "passenger-details",
                    "firstName"))));

        Services.AddSingleton<IFormSurfacePresenter, FormSurfacePresenter>();
        Services.AddSingleton<ISpeechAssistantFormSurfaceState>(state);
        Services.AddSingleton<ISpeechAssistantSilentInputController>(Mock.Of<ISpeechAssistantSilentInputController>());

        var cut = Render<SpeechAssistantFormSurface>();

        Assert.Contains("data-silent-input=\"editing\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("id=\"silent-input-control\"", cut.Markup, StringComparison.Ordinal);
        Assert.Contains("type=\"text\"", cut.Markup, StringComparison.Ordinal);
    }

    private static void Publish(SpeechAssistantFormSurfaceStore store, FormSurfaceSnapshot snapshot)
    {
        var session = store.BeginSession();
        if (snapshot.Stage == FormSurfaceStage.Active)
        {
            Assert.True(store.TryPublishActive(session, SpeechAssistantFormSurfaceView.FromSafeSnapshot(snapshot)));
            return;
        }

        Assert.True(store.TryPublishTerminal(session, SpeechAssistantFormSurfaceView.FromSafeSnapshot(snapshot)));
    }

    private static FormSurfaceSnapshot CreateSnapshot(
        FormSurfaceStage stage,
        string? focusedFieldId,
        FormSurfaceFieldStatus firstNameStatus,
        FormSurfaceFieldStatus seatClassStatus,
        FormSurfaceFieldGroupStatus fieldGroupStatus,
        IReadOnlyDictionary<string, FormSurfaceFieldValue> fieldValuesById)
    {
        var formProjection = new FormSurfaceDefinition(
            entryFieldGroupId: "main",
            fieldGroupsById: new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase)
            {
                ["main"] = new("main", "Passenger details", "Readonly mirror of the active form fields.", null)
            },
            fields:
            [
                new FormSurfaceField("firstName", "main", "First name", "string", false, "text", true, true, null, null, variableId: "firstName"),
                new FormSurfaceField(
                    "seatClass",
                    "main",
                    "Seat class",
                    "enum",
                    false,
                    "select",
                    true,
                    true,
                    null,
                    null,
                    options:
                    [
                        new FormSurfaceOption("economy", "Economy"),
                        new FormSurfaceOption("business", "Business")
                    ],
                    variableId: "seatClass")
            ]);

        return new FormSurfaceSnapshot(
            stage,
            "en",
            "travel-assistant",
            "Travel assistant",
            "passenger-details",
            "Passenger details",
            formProjection,
            fieldValuesById,
            focusedFieldId,
            new Dictionary<string, FormSurfaceFieldStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["firstName"] = firstNameStatus,
                ["seatClass"] = seatClassStatus
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase),
            new Dictionary<string, FormSurfaceFieldGroupStatus>(StringComparer.OrdinalIgnoreCase)
            {
                ["main"] = fieldGroupStatus
            }.ToImmutableDictionary(StringComparer.OrdinalIgnoreCase));
    }

    private static FormFieldPresentation CreatePresentationField(
        string fieldId,
        bool required,
        bool isFocused,
        FormSurfaceFieldStatus status,
        string? displayValue)
    {
        return new FormFieldPresentation(
            new FormSurfaceField(
                fieldId,
                "main",
                fieldId == "seatClass" ? "Seat class" : "First name",
                fieldId == "seatClass" ? "enum" : "string",
                codeStyle: false,
                fieldId == "seatClass" ? "select" : "text",
                required,
                isUserEditable: true,
                helpText: null,
                placeholder: null,
                options: fieldId == "seatClass"
                    ? [new FormSurfaceOption("business", "Business")]
                    : null,
                variableId: fieldId),
            displayValue,
            !string.IsNullOrWhiteSpace(displayValue),
            isFocused,
            status);
    }

    private static FormFieldGroupPresentation CreatePresentationFieldGroup(FormSurfaceFieldGroupStatus status)
    {
        return new FormFieldGroupPresentation(
            new FormSurfaceFieldGroupInfo("main", "Passenger details"),
            IsEntryFieldGroup: true,
            status,
            Fields: ImmutableArray<FormFieldPresentation>.Empty);
    }
}
