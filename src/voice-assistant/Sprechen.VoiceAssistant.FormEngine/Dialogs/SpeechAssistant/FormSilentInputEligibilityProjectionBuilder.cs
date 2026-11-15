using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zulässigkeit stiller Eingabe und wählt ein Steuerelement anhand von
/// Eingaberegeln, Datenschutzklasse und Werttyp.
/// </summary>
internal sealed class FormSilentInputEligibilityProjectionBuilder
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormFieldInputModeAvailabilityPolicy _availabilityPolicy;

    public FormSilentInputEligibilityProjectionBuilder(
        FormDialogDefinitionState definitionState,
        FormFieldInputModeAvailabilityPolicy availabilityPolicy)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _availabilityPolicy = availabilityPolicy ?? throw new ArgumentNullException(nameof(availabilityPolicy));
    }

    public FormSilentInputEligibilityProjection? TryProject(FieldDefinition field, VariableDefinition variable)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variable);

        var availability = _availabilityPolicy.Evaluate(field, variable, InputMode.Keyboard);
        if (availability.Status != FormFieldInputModeAvailabilityStatus.Available)
        {
            return null;
        }

        var policy = _definitionState.RequireEffectivePolicy(variable.VariableId);
        if (policy.PrivacyClass == PrivacyClasses.Secret)
        {
            return null;
        }

        var controlKind = TryMapFirstSliceControl(field, variable);
        if (controlKind is null)
        {
            return null;
        }

        return new FormSilentInputEligibilityProjection(field.FieldId.Value, controlKind.Value);
    }

    internal static SpeechAssistantSilentInputControlKind? TryMapFirstSliceControl(
        FieldDefinition field,
        VariableDefinition variable)
    {
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variable);

        var valueTypeKind = ReadValueTypeProperty(variable, ValueTypePropertyNames.Kind);
        var effectiveType = FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, valueTypeKind);
        if (string.Equals(effectiveType, EffectiveFieldInteractionTypes.Code, StringComparison.Ordinal))
        {
            return SpeechAssistantSilentInputControlKind.CodeText;
        }

        if (!string.Equals(effectiveType, EffectiveFieldInteractionTypes.Text, StringComparison.Ordinal))
        {
            return null;
        }

        if (field.PresentationHints?.MultilinePreferred == true)
        {
            return SpeechAssistantSilentInputControlKind.MultilineText;
        }

        var formatHint = ReadValueTypeProperty(variable, ValueTypePropertyNames.FormatHint);
        if (string.Equals(formatHint, "email", StringComparison.Ordinal))
        {
            return SpeechAssistantSilentInputControlKind.Email;
        }

        if (string.Equals(formatHint, "phone", StringComparison.Ordinal))
        {
            return SpeechAssistantSilentInputControlKind.Telephone;
        }

        return SpeechAssistantSilentInputControlKind.Text;
    }

    private static string? ReadValueTypeProperty(VariableDefinition variable, string propertyName)
        => variable.ValueType.ValueKind == JsonValueKind.Object
            && variable.ValueType.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}
