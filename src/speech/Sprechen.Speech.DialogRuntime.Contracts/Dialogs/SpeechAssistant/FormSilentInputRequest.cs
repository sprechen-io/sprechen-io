using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Feldzuordnung und Ersetzungs- oder Leerungsabsicht für die stille Eingabe.
/// </summary>
public sealed class FormSilentInputRequest
{
    private FormSilentInputRequest(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind,
        FormSilentInputIntentKind intentKind,
        string? value)
    {
        LanguageCode = languageCode;
        ApplicationId = applicationId;
        FormId = formId;
        FieldId = fieldId;
        ControlKind = controlKind;
        IntentKind = intentKind;
        Value = value;
    }

    public string LanguageCode { get; }

    public string ApplicationId { get; }

    public string FormId { get; }

    public string FieldId { get; }

    public SpeechAssistantSilentInputControlKind ControlKind { get; }

    public FormSilentInputIntentKind IntentKind { get; }

    public string? Value { get; }

    public static FormSilentInputRequest Replace(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind,
        string value)
    {
        ValidateIdentity(languageCode, applicationId, formId, fieldId);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Replace requires a non-blank value.", nameof(value));
        }

        return new FormSilentInputRequest(
            languageCode.Trim(),
            applicationId.Trim(),
            formId.Trim(),
            fieldId.Trim(),
            controlKind,
            FormSilentInputIntentKind.Replace,
            value);
    }

    public static FormSilentInputRequest Clear(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind)
    {
        ValidateIdentity(languageCode, applicationId, formId, fieldId);
        return new FormSilentInputRequest(
            languageCode.Trim(),
            applicationId.Trim(),
            formId.Trim(),
            fieldId.Trim(),
            controlKind,
            FormSilentInputIntentKind.Clear,
            value: null);
    }

    private static void ValidateIdentity(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
    }
}
