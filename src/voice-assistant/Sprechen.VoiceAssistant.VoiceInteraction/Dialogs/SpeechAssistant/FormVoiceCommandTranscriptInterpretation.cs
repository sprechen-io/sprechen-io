namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Transkriptquelle, Parsergebnis und Interpretationsmodus für die anschließende Feldbehandlung zusammen.
/// </summary>
public sealed record FormVoiceCommandTranscriptInterpretation(
    FormVoiceCommandTranscriptSource Source,
    FormVoiceCommandParsedCommand Command,
    FormVoiceCommandInterpretationMode Mode)
{
    public string TrimmedTranscript => Source.TrimmedTranscript;

    public string InputTranscript => Source.InputTranscript;

    public bool HasExplicitCommandPrefix => Source.HasExplicitCommandPrefix;

    public bool HasParsedCommand => Command.HasParsedCommand;

    public string ParsedCommandId => Command.ParsedCommandId;

    public string? ParsedCommandArgument => Command.ParsedCommandArgument;

    public string CommandSignalClass => Command.CommandSignalClass;

    public bool IsTextField => Mode.IsTextField;

    public bool IsLiteralMode => Mode.IsLiteralMode;

    public bool IsDigitsMode => Mode.IsDigitsMode;

    public bool IsSpellingMode => Mode.IsSpellingMode;

    public bool ShouldExecuteParsedCommand => Mode.ShouldExecuteParsedCommand;

    public bool ShouldExecuteParsedCommandInDigitsMode => Mode.ShouldExecuteParsedCommandInDigitsMode;
}
