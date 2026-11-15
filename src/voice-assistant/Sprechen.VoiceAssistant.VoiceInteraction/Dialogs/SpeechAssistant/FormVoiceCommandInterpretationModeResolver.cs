using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Interpretationsmodi und Befehlsignalklasse aus Feld, erwarteter Eingabeart und Parserergebnis ab. Der Ziffernmodus behält seine gesonderte Befehlsfreigabe.
/// </summary>
internal sealed class FormVoiceCommandInterpretationModeResolver
{
    public FormVoiceCommandInterpretationModeResolution Resolve(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        InputMode expectedInputMode,
        FormVoiceCommandTranscriptSource source,
        FormVoiceCommandParsedCommandDraft parsedCommand)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(parsedCommand);

        var isTextField = FieldInteractionSemantics.IsTextField(field.CodeStyle, field.ValueTypeKind);
        var isLiteralMode = expectedInputMode == InputMode.Literal;
        var isDigitsMode = expectedInputMode == InputMode.Digits;
        var isSpellingMode = expectedInputMode == InputMode.Spelling;
        var shouldExecuteParsedCommand = parsedCommand.HasParsedCommand
            && FormVoiceCommandCommandSemantics.ShouldExecuteParsedCommand(
                context.Contract,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel(),
                source.HasExplicitCommandPrefix,
                isLiteralMode,
                source.TrimmedTranscript,
                parsedCommand.ParsedCommandId,
                context.AcceptedCommandAnchorForms);

        // Im Ziffernmodus globale Befehle nur mit explizitem Präfix oder
        // akzeptiertem führendem Aktivierungswort freigeben. Lokale
        // Ziffernkorrekturen bleiben beim eigenen Eingabeablauf.
        var shouldExecuteParsedCommandInDigitsMode = isDigitsMode
            && parsedCommand.HasParsedCommand
            && (source.HasExplicitCommandPrefix
                || FormGlobalCommandParser.HasCommandLeadIn(source.TrimmedTranscript, context.AcceptedCommandAnchorForms));

        return new FormVoiceCommandInterpretationModeResolution(
            new FormVoiceCommandInterpretationMode(
                isTextField,
                isLiteralMode,
                isDigitsMode,
                shouldExecuteParsedCommand,
                shouldExecuteParsedCommandInDigitsMode,
                isSpellingMode),
            DetermineCommandSignalClass(parsedCommand, shouldExecuteParsedCommand));
    }

    private static string DetermineCommandSignalClass(
        FormVoiceCommandParsedCommandDraft parsedCommand,
        bool shouldExecuteParsedCommand)
    {
        if (!parsedCommand.HasParsedCommand || parsedCommand.RepeatShouldRemainInput)
        {
            return CommandSignalClass.NoCommand;
        }

        // Die Befehlskennzeichnung aus der Ausführungsentscheidung übernehmen.
        // Sie berücksichtigt auch die kontextbezogenen Ausnahmen für Moduswechsel.
        return shouldExecuteParsedCommand ? CommandSignalClass.Clear : CommandSignalClass.NoCommand;
    }
}
