namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Erkennt einen Befehl und bildet daraus ein vorläufiges Ergebnis für
/// die spätere Ausführungsentscheidung. Prüft außerdem, ob ein erkanntes
/// Wiederholungswort als Feldinhalt behandelt werden soll.
/// </summary>
internal sealed class FormVoiceCommandParsedCommandResolver
{
    public FormVoiceCommandParsedCommandDraft Resolve(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptSourceResolution sourceResolution)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sourceResolution);

        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var parsedCommandId = string.Empty;
        string? parsedCommandArgument = null;
        var hasParsedCommand = FormGlobalCommandParser.TryParse(
                context.Contract,
                sourceResolution.CommandTranscript,
                context.IsoCode,
                politenessLevel,
                out parsedCommandId,
                out parsedCommandArgument);

        var repeatShouldRemainInput = hasParsedCommand
            && FormVoiceCommandCommandSemantics.ShouldTreatRepeatAsInput(
                context.Contract,
                sourceResolution.Source.TrimmedTranscript,
                parsedCommandId,
                context.IsoCode,
                politenessLevel,
                context.AcceptedCommandAnchorForms);

        return new FormVoiceCommandParsedCommandDraft(
            hasParsedCommand,
            parsedCommandId,
            parsedCommandArgument,
            repeatShouldRemainInput);
    }
}
