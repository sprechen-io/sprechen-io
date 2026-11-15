namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Befehle mit vorangestelltem Aktivierungswort beim Buchstabieren
/// und Sammeln von Ziffern oder Auswahlwerten. Dafür verwendet er dieselbe
/// Zerlegung, Befehlserkennung und Ausführung wie der übrige Formulardialog.
/// Ein geliefertes Dialogergebnis wird unmittelbar zurückgegeben. Andernfalls
/// wird die Eingabe bei unverändertem Fokusfeld und Eingabemodus fortgesetzt.
/// Bei einer Änderung wird die Feldverarbeitung neu gestartet.
/// </summary>
internal sealed class CharacterAccumulationAnchoredCommandFlow
{
    private readonly FormVoiceCommandTranscriptSourceResolver _sourceResolver;
    private readonly FormVoiceCommandParsedCommandResolver _parsedCommandResolver;
    private readonly FormVoiceCommandGlobalCommandExecutionService _executionService;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public CharacterAccumulationAnchoredCommandFlow(
        FormVoiceCommandTranscriptSourceResolver sourceResolver,
        FormVoiceCommandParsedCommandResolver parsedCommandResolver,
        FormVoiceCommandGlobalCommandExecutionService executionService,
        IFormVoiceCommandSpeechService speechService)
    {
        _sourceResolver = sourceResolver ?? throw new ArgumentNullException(nameof(sourceResolver));
        _parsedCommandResolver = parsedCommandResolver ?? throw new ArgumentNullException(nameof(parsedCommandResolver));
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    /// <summary>
    /// Erkennt ein Aktivierungswort ohne folgenden Befehl. Es darf beim
    /// Buchstabieren nicht als Teil des Feldwerts übernommen werden.
    /// </summary>
    public static bool IsBareAnchor(CharacterAccumulationContext context, string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);
        return FormGlobalCommandParser.IsBareCommandAnchor(transcript, context.Context.AcceptedCommandAnchorForms);
    }

    /// <summary>
    /// Fordert den modellierten Hinweis zur Formulierung eines Befehls mit
    /// Aktivierungswort an.
    /// </summary>
    public Task SpeakAnchorHintAsync(CharacterAccumulationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var voiceContext = context.Context;
        return _speechService.SpeakSafeAsync(
            FormCommandAnchorHintPromptBuilder.BuildPrompt(
                voiceContext.Contract,
                voiceContext.IsoCode,
                voiceContext.VoiceSettings.GetPolitenessLevel()),
            voiceContext.IsoCode,
            voiceContext.VoiceSettings,
            cancellationToken);
    }

    /// <summary>
    /// Trennt Aktivierungswort und Befehlspräfix vom Transkript. Die weitere
    /// Moduserkennung verwendet <see cref="FormVoiceCommandTranscriptSourceResolution.CommandTranscript"/>,
    /// damit das Aktivierungswort vor einem Abschlussbefehl nicht als Inhalt
    /// in den Feldwert gelangt.
    /// </summary>
    public FormVoiceCommandTranscriptSourceResolution ResolveTranscriptSource(CharacterAccumulationContext context, string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);
        return _sourceResolver.Resolve(context.Context, transcript);
    }

    /// <summary>
    /// Führt einen erkannten Befehl mit explizitem Präfix aus und bestimmt,
    /// ob die Eingabe fortgesetzt werden kann. Ohne Präfix oder erkannten Befehl
    /// wird <c>null</c> zurückgegeben.
    /// </summary>
    public async Task<CharacterAccumulationTurnOutcome?> TryExecuteAsync(
        CharacterAccumulationContext context,
        FormVoiceCommandTranscriptSourceResolution sourceResolution,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(sourceResolution);

        if (!sourceResolution.Source.HasExplicitCommandPrefix)
        {
            return null;
        }

        var draft = _parsedCommandResolver.Resolve(context.Context, sourceResolution);
        if (!draft.HasParsedCommand)
        {
            return null;
        }

        var dialogResult = await _executionService.RouteAsync(
                context.Context,
                context.Interpreter,
                context.Session,
                context.StateTracker,
                draft.ParsedCommandId,
                draft.ParsedCommandArgument,
                cancellationToken)
            .ConfigureAwait(false);
        if (dialogResult is not null)
        {
            return CharacterAccumulationTurnOutcome.ReturnDialogResult(dialogResult);
        }

        // Navigation, Moduswechsel und eine abgelehnte Abgabe können trotz geändertem
        // Fokus null liefern. Deshalb Fokusfeld und Eingabemodus vergleichen,
        // bevor der bisherige Puffer weiterverwendet wird.
        var focusedField = await context.Interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
        var staysInField = focusedField is not null
            && string.Equals(focusedField.FieldId, context.Field.FieldId, StringComparison.Ordinal)
            && focusedField.InputMode == context.Field.InputMode;

        return staysInField
            ? CharacterAccumulationTurnOutcome.AwaitNextTurn()
            : CharacterAccumulationTurnOutcome.ExitOnFocusOrModeChange();
    }
}
