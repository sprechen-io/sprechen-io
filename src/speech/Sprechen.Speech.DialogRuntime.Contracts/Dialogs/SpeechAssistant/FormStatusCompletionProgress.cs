namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Hält getrennte Fortschrittsdimensionen für die Statusansage nach der konfigurierten Berechnungsgrundlage bereit.
/// </summary>
public sealed record FormStatusCompletionProgress(
    FormStatusProgressDimension AllFields,
    FormStatusProgressDimension RequiredFieldsOnly,
    FormStatusProgressDimension FieldGroupBased);
