namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Trennt Eingabe- und Befehlstranskript anhand des übergebenen Erfassungsergebnisses. Ein erkanntes Präfix bleibt im vollständigen Eingabetext erhalten.
/// </summary>
internal sealed class FormVoiceCommandTranscriptSourceResolver
{
    public FormVoiceCommandTranscriptSourceResolution Resolve(
        FormVoiceCommandContext context,
        string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var trimmedTranscript = transcript.Trim();
        var hasWakeWordCommandSignal = FormVoiceCommandWakeWordActivation.TryExtractActivatedCommandTranscript(
            context,
            trimmedTranscript,
            out var wakeWordCommandTranscript);
        var commandSourceTranscript = hasWakeWordCommandSignal
            ? wakeWordCommandTranscript
            : trimmedTranscript;

        var hasCommandPrefixAfterWakeWord = FormGlobalCommandParser.TryStripCommandPrefix(
            context.Contract,
            commandSourceTranscript,
            context.IsoCode,
            politenessLevel,
            out var prefixedCommandTranscript);
        var hasExplicitCommandPrefix = hasWakeWordCommandSignal || hasCommandPrefixAfterWakeWord;
        var commandTranscript = hasCommandPrefixAfterWakeWord ? prefixedCommandTranscript : commandSourceTranscript;

        return new FormVoiceCommandTranscriptSourceResolution(
            new FormVoiceCommandTranscriptSource(
                trimmedTranscript,
                trimmedTranscript,
                hasExplicitCommandPrefix),
            commandTranscript);
    }
}
