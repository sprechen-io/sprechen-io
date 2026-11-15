using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSilentInputEligibilityProjectionBuilderTests
{
    [Fact]
    public void TryMapFirstSliceControl_MapsTextEmailTelephoneMultilineAndCode()
    {
        Assert.Equal(
            SpeechAssistantSilentInputControlKind.Text,
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("name"),
                Variable("string")));
        Assert.Equal(
            SpeechAssistantSilentInputControlKind.Email,
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("mail"),
                Variable("string", "email")));
        Assert.Equal(
            SpeechAssistantSilentInputControlKind.Telephone,
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("phone"),
                Variable("string", "phone")));
        Assert.Equal(
            SpeechAssistantSilentInputControlKind.MultilineText,
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("notes", multiline: true),
                Variable("string")));
        Assert.Equal(
            SpeechAssistantSilentInputControlKind.CodeText,
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("iban", codeStyle: true),
                Variable("string")));
    }

    [Fact]
    public void TryMapFirstSliceControl_LeavesComplexKindsUnavailable()
    {
        Assert.Null(
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("age"),
                Variable("integer")));
        Assert.Null(
            FormSilentInputEligibilityProjectionBuilder.TryMapFirstSliceControl(
                Field("when"),
                Variable("date")));
    }

    private static FieldDefinition Field(string fieldId, bool multiline = false, bool codeStyle = false)
        => new()
        {
            FieldId = FieldId.From(fieldId),
            CodeStyle = codeStyle,
            PresentationHints = multiline ? new FieldPresentationHints { MultilinePreferred = true } : null
        };

    private static VariableDefinition Variable(string kind, string? formatHint = null)
    {
        var json = formatHint is null
            ? $$"""{"kind":"{{kind}}"}"""
            : $$"""{"kind":"{{kind}}","formatHint":"{{formatHint}}"}""";
        return new VariableDefinition
        {
            VariableId = VariableId.From("v"),
            ValueType = JsonDocument.Parse(json).RootElement.Clone()
        };
    }
}
