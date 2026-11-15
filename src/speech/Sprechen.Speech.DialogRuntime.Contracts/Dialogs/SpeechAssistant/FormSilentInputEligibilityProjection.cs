using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet einem für stille Eingabe geeigneten Feld die passende Bedienelementart zu.
/// </summary>
public sealed record FormSilentInputEligibilityProjection(
    string FieldId,
    SpeechAssistantSilentInputControlKind ControlKind);
