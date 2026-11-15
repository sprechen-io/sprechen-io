namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst die sprachbezogene Bezeichnung eines geparsten Befehls für ein Mehrdeutigkeitssignal auf. Ohne Bezeichnung bleibt dessen Kennung erhalten.
/// </summary>
internal sealed class FormVoiceCommandAmbiguityLabelResolver
{
    public string Resolve(
        FormVoiceCommandContext context,
        FormVoiceCommandTranscriptInterpretation interpretation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interpretation);

        return FormGlobalCommandParser.ResolveCommandLabel(
                context.Contract,
                interpretation.ParsedCommandId,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel())
            ?? interpretation.ParsedCommandId;
    }
}
