namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Wertet Bestätigungsantworten aus. Übernehmen, Bearbeiten und Buchstabieren
/// werden direkt zugeordnet. Weitere Befehle durchlaufen die gemeinsame
/// Befehlsprüfung. Ein Abschlussbefehl ohne Freigabe als globaler Befehl
/// gilt als Zustimmung. Übrige Texte werden als Ja oder Nein geprüft.
/// </summary>
internal sealed class FormVoiceCommandConfirmationTranscriptInterpreter
{
    private readonly FormVoiceCommandConfirmationCommandDecisionMapper _commandDecisionMapper;
    private readonly IFormTranscriptParser _transcriptParser;

    public FormVoiceCommandConfirmationTranscriptInterpreter(
        FormVoiceCommandConfirmationCommandDecisionMapper commandDecisionMapper,
        IFormTranscriptParser transcriptParser)
    {
        _commandDecisionMapper = commandDecisionMapper ?? throw new ArgumentNullException(nameof(commandDecisionMapper));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
    }

    public ConfirmationDecision? TryInterpret(
        FormVoiceCommandContext context,
        string transcript)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var normalizedTranscript = transcript.Trim();
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        if (FormGlobalCommandParser.TryParse(
                context.Contract,
                normalizedTranscript,
                context.IsoCode,
                politenessLevel,
                out var commandId,
                out var commandArgument))
        {
            var decision = _commandDecisionMapper.Map(commandId, commandArgument);

            // Die drei direkt zugeordneten Bestätigungsbefehle benötigen kein
            // Aktivierungswort. Andere Befehle werden anschließend über die
            // gemeinsame Befehlsprüfung mit deren Modusausnahmen bewertet.
            if (decision.Kind != ConfirmationDecisionKind.Execution)
            {
                return decision;
            }

            var anchored = FormVoiceCommandCommandSemantics.ShouldExecuteParsedCommand(
                context.Contract,
                context.IsoCode,
                politenessLevel,
                FormGlobalCommandParser.TryStripCommandPrefix(
                    context.Contract,
                    normalizedTranscript,
                    context.IsoCode,
                    politenessLevel,
                    out _),
                isLiteralMode: false,
                normalizedTranscript,
                commandId,
                context.AcceptedCommandAnchorForms);
            if (anchored)
            {
                return decision;
            }

            // Wird Submit nicht als globaler Befehl ausgeführt, gilt es hier als
            // Zustimmung zum Feldwert. Ein ausdrücklich freigegebener globaler
            // Abgabebefehl wurde bereits oben zurückgegeben.
            if (string.Equals(commandId, GlobalCommandIds.Submit, StringComparison.Ordinal))
            {
                return ConfirmationDecision.Continue();
            }
        }

        var yesNoOutcome = _transcriptParser.ParseYesNo(context.IsoCode, normalizedTranscript);
        if (yesNoOutcome.Success)
        {
            return string.Equals(yesNoOutcome.CanonicalValue, "true", StringComparison.Ordinal)
                ? ConfirmationDecision.Continue()
                : ConfirmationDecision.Edit();
        }

        return null;
    }
}
