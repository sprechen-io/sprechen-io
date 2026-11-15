using System.Collections.Immutable;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergänzt die stille Eingabe um Anwendungssitzung und Ausgangswerte zur Laufzeiterzeugung.
/// </summary>
public sealed class FormSilentInputTransactionRequest
{
    private FormSilentInputTransactionRequest(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind,
        FormSilentInputIntentKind intentKind,
        string? value,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
    {
        LanguageCode = languageCode;
        ApplicationId = applicationId;
        FormId = formId;
        FieldId = fieldId;
        ControlKind = controlKind;
        IntentKind = intentKind;
        Value = value;
        ApplicationSession = applicationSession;
        InitialValuesByVariableId = initialValuesByVariableId;
    }

    public string LanguageCode { get; }

    public string ApplicationId { get; }

    public string FormId { get; }

    public string FieldId { get; }

    public SpeechAssistantSilentInputControlKind ControlKind { get; }

    public FormSilentInputIntentKind IntentKind { get; }

    public string? Value { get; }

    public SpeechAssistantApplicationSessionState ApplicationSession { get; }

    public IReadOnlyDictionary<string, string> InitialValuesByVariableId { get; }

    public static FormSilentInputTransactionRequest Replace(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind,
        string value,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
    {
        Validate(
            languageCode,
            applicationId,
            formId,
            fieldId,
            applicationSession,
            initialValuesByVariableId);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Replace requires a non-blank value.", nameof(value));
        }

        return new FormSilentInputTransactionRequest(
            languageCode.Trim(),
            applicationId.Trim(),
            formId.Trim(),
            fieldId.Trim(),
            controlKind,
            FormSilentInputIntentKind.Replace,
            value,
            applicationSession,
            initialValuesByVariableId);
    }

    public static FormSilentInputTransactionRequest Clear(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantSilentInputControlKind controlKind,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
    {
        Validate(
            languageCode,
            applicationId,
            formId,
            fieldId,
            applicationSession,
            initialValuesByVariableId);
        return new FormSilentInputTransactionRequest(
            languageCode.Trim(),
            applicationId.Trim(),
            formId.Trim(),
            fieldId.Trim(),
            controlKind,
            FormSilentInputIntentKind.Clear,
            value: null,
            applicationSession,
            initialValuesByVariableId);
    }

    public FormSilentInputRequest ToRuntimeRequest()
        => IntentKind == FormSilentInputIntentKind.Clear
            ? FormSilentInputRequest.Clear(LanguageCode, ApplicationId, FormId, FieldId, ControlKind)
            : FormSilentInputRequest.Replace(LanguageCode, ApplicationId, FormId, FieldId, ControlKind, Value!);

    private static void Validate(
        string languageCode,
        string applicationId,
        string formId,
        string fieldId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);
        ArgumentNullException.ThrowIfNull(applicationSession);
        ArgumentNullException.ThrowIfNull(initialValuesByVariableId);
        _ = ImmutableDictionary.CreateRange(
            StringComparer.Ordinal,
            initialValuesByVariableId);
    }
}
