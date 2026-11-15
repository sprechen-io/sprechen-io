namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Navigation zum nächsten offenen Pflichtfeld. Enthält das erreichte
/// oder bereits allein fokussierte Zielfeld und die Zahl noch offener Pflichtfelder.
/// Ohne Ziel ist die Feldkennung null.
/// </summary>
public sealed record FocusNextOpenRequiredFieldResult(
    FocusNextOpenRequiredFieldResultKind Kind,
    string? TargetFieldId,
    int RemainingOpenRequiredCount);
