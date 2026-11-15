namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Katalogbefehle den gemeinsamen Auswahlbefehlen zu und erhält Hinweise auf
/// konkurrierende Absichten. Katalogspezifische Befehle bleiben hier unbehandelt,
/// damit ihre Verarbeitung außerhalb der gemeinsamen Auswahlsteuerung erfolgt.
/// </summary>
internal static class FormsCatalogSelectionIntentMapper
{
    public static SelectionDialogCommonIntentParseResult Map(FormsCatalogIntentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var mapped = result.Intent switch
        {
            FormsCatalogIntent.Cancel => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Cancel),
            FormsCatalogIntent.Help => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Help),
            FormsCatalogIntent.Repeat => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Repeat),
            FormsCatalogIntent.List => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.List),
            FormsCatalogIntent.SelectByNumber => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByNumber,
                Number: result.Number),
            FormsCatalogIntent.SelectByName => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.SelectByName,
                TargetName: result.TargetName),
            FormsCatalogIntent.OpenCandidate => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.OpenCandidate,
                TargetName: result.TargetName),
            FormsCatalogIntent.Unknown => new SelectionDialogCommonIntentParseResult(
                SelectionDialogCommonIntent.Unknown,
                TargetName: result.TargetName),
            FormsCatalogIntent.ConfirmYes => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.ConfirmYes),
            FormsCatalogIntent.ConfirmNo => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.ConfirmNo),
            _ => new SelectionDialogCommonIntentParseResult(SelectionDialogCommonIntent.Unhandled)
        };

        return mapped with { HasCompetingIntentEvidence = result.HasCompetingIntentEvidence };
    }

    public static SelectionDialogCommonIntent MapConfirmation(FormsCatalogIntent intent)
    {
        return intent switch
        {
            FormsCatalogIntent.ConfirmYes => SelectionDialogCommonIntent.ConfirmYes,
            FormsCatalogIntent.ConfirmNo => SelectionDialogCommonIntent.ConfirmNo,
            FormsCatalogIntent.Cancel => SelectionDialogCommonIntent.Cancel,
            _ => SelectionDialogCommonIntent.Unhandled
        };
    }
}
