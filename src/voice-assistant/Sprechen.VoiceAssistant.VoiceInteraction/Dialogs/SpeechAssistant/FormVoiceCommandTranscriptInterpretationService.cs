namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Transkriptquelle, Interpretationsmodus und Befehlsauflösung zu einem Ergebnis für die weitere Feldinteraktion.
/// </summary>
internal sealed class FormVoiceCommandTranscriptInterpretationService
{
    private readonly FormVoiceCommandTranscriptSourceResolver _sourceResolver;
    private readonly FormVoiceCommandParsedCommandResolver _parsedCommandResolver;
    private readonly FormVoiceCommandInterpretationModeResolver _modeResolver;

    internal FormVoiceCommandTranscriptInterpretationService()
        : this(
            new FormVoiceCommandTranscriptSourceResolver(),
            new FormVoiceCommandParsedCommandResolver(),
            new FormVoiceCommandInterpretationModeResolver())
    {
    }

    public FormVoiceCommandTranscriptInterpretationService(
        FormVoiceCommandTranscriptSourceResolver sourceResolver,
        FormVoiceCommandParsedCommandResolver parsedCommandResolver,
        FormVoiceCommandInterpretationModeResolver modeResolver)
    {
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        _parsedCommandResolver = parsedCommandResolver ?? throw new ArgumentNullException(nameof(parsedCommandResolver));
        _modeResolver = modeResolver ?? throw new ArgumentNullException(nameof(modeResolver));
    }

    public FormVoiceCommandTranscriptInterpretation Interpret(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        InputMode expectedInputMode,
        string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var sourceResolution = _sourceResolver.Resolve(context, transcript);
        var parsedCommand = _parsedCommandResolver.Resolve(context, sourceResolution);
        var modeResolution = _modeResolver.Resolve(
            context,
            field,
            expectedInputMode,
            sourceResolution.Source,
            parsedCommand);

        return new FormVoiceCommandTranscriptInterpretation(
            sourceResolution.Source,
            new FormVoiceCommandParsedCommand(
                parsedCommand.HasParsedCommand,
                parsedCommand.ParsedCommandId,
                parsedCommand.ParsedCommandArgument,
                modeResolution.CommandSignalClass),
            modeResolution.Mode);
    }
}
