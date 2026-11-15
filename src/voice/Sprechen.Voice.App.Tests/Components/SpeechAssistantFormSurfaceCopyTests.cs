using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.FormSurfaces;
using Sprechen.Web.SpeechComponents;
using System.Collections.Immutable;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechAssistantFormSurfaceCopyTests
{
    private static readonly VoiceInterfaceLanguageCatalog LanguageCatalog = VoiceInterfaceLanguageCatalog.CreateDefault();
    private static readonly SpeechAssistantFormSurfaceCopy Copy = new();

    public static IEnumerable<object[]> SupportedLanguages()
    {
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            yield return [isoCode];
        }
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void CopyOwners_WhenResolvedForLanguage_UseModeledResources(string isoCode)
    {
        var fieldWithoutValue = CreateFieldPresentation(
            required: true,
            isFocused: false,
            status: null,
            displayValue: null);

        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateKicker, isoCode),
            Copy.EmptyState.GetKicker(isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateTitle, isoCode),
            Copy.EmptyState.GetTitle(isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceEmptyStateDescription, isoCode),
            Copy.EmptyState.GetDescription(isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldValueNotCaptured, isoCode),
            Copy.Fields.GetValue(fieldWithoutValue, isoCode));
        Assert.Equal(
            ResolveTemplate(
                VoiceInterfaceResourceIds.FormSurfaceLanguageSummary,
                isoCode,
                "{languageName}",
                LanguageCatalog.ResolveDisplayName(isoCode, isoCode)),
            Copy.Language.GetSummary(isoCode));
        Assert.Equal(
            ResolveTemplate(
                VoiceInterfaceResourceIds.FormSurfaceFocusedFieldSummary,
                isoCode,
                "{fieldLabel}",
                "Seat class"),
            Copy.Language.GetFocusedFieldSummary("Seat class", isoCode));
    }

    [Theory]
    [MemberData(nameof(SupportedLanguages))]
    public void StatusLabels_WhenResolvedForLanguage_UseModeledResources(string isoCode)
    {
        var focusedField = CreateFieldPresentation(
            required: true,
            isFocused: true,
            status: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: false, []),
            displayValue: "Business");
        var pendingField = CreateFieldPresentation(required: true, isFocused: false, status: null, displayValue: null);
        var optionalField = CreateFieldPresentation(required: false, isFocused: false, status: null, displayValue: null);
        var notRelevantField = CreateFieldPresentation(
            required: true,
            isFocused: false,
            status: new FormSurfaceFieldStatus("seatClass", "main", relevant: false, required: true, completed: false, []),
            displayValue: null);
        var completedField = CreateFieldPresentation(
            required: true,
            isFocused: false,
            status: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: true, []),
            displayValue: "Business");
        var requiredField = CreateFieldPresentation(
            required: true,
            isFocused: false,
            status: new FormSurfaceFieldStatus("seatClass", "main", relevant: true, required: true, completed: false, []),
            displayValue: null);

        var pendingGroup = CreateFieldGroupPresentation(status: null);
        var notRelevantGroup = CreateFieldGroupPresentation(
            new FormSurfaceFieldGroupStatus("main", relevant: false, completed: false, []));
        var incompleteGroup = CreateFieldGroupPresentation(
            new FormSurfaceFieldGroupStatus("main", relevant: true, completed: false, []));
        var completedGroup = CreateFieldGroupPresentation(
            new FormSurfaceFieldGroupStatus("main", relevant: true, completed: true, []));

        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusListening, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, focusedField, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusPending, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, pendingField, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusOptional, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, optionalField, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusNotRelevant, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, notRelevantField, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusCompleted, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, completedField, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldStatusRequired, isoCode),
            Copy.Fields.GetStatusLabel(FormSurfaceStage.Active, requiredField, isoCode));

        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusPending, isoCode),
            Copy.FieldGroups.GetStatusLabel(pendingGroup, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusNotRelevant, isoCode),
            Copy.FieldGroups.GetStatusLabel(notRelevantGroup, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusInProgress, isoCode),
            Copy.FieldGroups.GetStatusLabel(incompleteGroup, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceFieldGroupStatusCompleted, isoCode),
            Copy.FieldGroups.GetStatusLabel(completedGroup, isoCode));

        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceStageLive, isoCode),
            Copy.Stages.GetLabel(FormSurfaceStage.Active, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceStageSubmitted, isoCode),
            Copy.Stages.GetLabel(FormSurfaceStage.Submitted, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceStageCancelled, isoCode),
            Copy.Stages.GetLabel(FormSurfaceStage.Cancelled, isoCode));
        Assert.Equal(
            ResolveResourceText(VoiceInterfaceResourceIds.FormSurfaceStageAbandoned, isoCode),
            Copy.Stages.GetLabel(FormSurfaceStage.Abandoned, isoCode));
    }

    private static FormFieldPresentation CreateFieldPresentation(
        bool required,
        bool isFocused,
        FormSurfaceFieldStatus? status,
        string? displayValue)
    {
        return new FormFieldPresentation(
            new FormSurfaceField(
                "seatClass",
                "main",
                "Seat class",
                "enum",
                codeStyle: false,
                "select",
                required,
                isUserEditable: true,
                helpText: null,
                placeholder: null,
                options:
                [
                    new FormSurfaceOption("economy", "Economy")
                ],
                variableId: "seatClass"),
            displayValue,
            !string.IsNullOrWhiteSpace(displayValue),
            isFocused,
            status);
    }

    private static FormFieldGroupPresentation CreateFieldGroupPresentation(FormSurfaceFieldGroupStatus? status)
    {
        return new FormFieldGroupPresentation(
            new FormSurfaceFieldGroupInfo("main", "Passenger details"),
            IsEntryFieldGroup: true,
            status,
            Fields: ImmutableArray<FormFieldPresentation>.Empty);
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(
            VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text),
            $"Expected modeled voice-interface resource '{resourceId}' for '{isoCode}'.");
        return text;
    }

    private static string ResolveTemplate(string resourceId, string isoCode, string placeholder, string replacement)
    {
        return ResolveResourceText(resourceId, isoCode).Replace(placeholder, replacement, StringComparison.Ordinal);
    }
}
