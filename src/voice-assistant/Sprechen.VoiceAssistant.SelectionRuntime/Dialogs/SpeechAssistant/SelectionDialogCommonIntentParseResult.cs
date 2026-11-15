namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet eine geparste gemeinsame Auswahlabsicht mit optionalem Ziel und konkurrierenden
/// Hinweisen für die anschließende Dialogentscheidung.
/// </summary>
public sealed record SelectionDialogCommonIntentParseResult(
    SelectionDialogCommonIntent Intent,
    string? TargetName = null,
    int? Number = null,
    bool HasCompetingIntentEvidence = false);
