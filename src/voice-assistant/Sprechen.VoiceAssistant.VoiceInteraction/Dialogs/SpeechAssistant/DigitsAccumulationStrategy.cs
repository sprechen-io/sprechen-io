namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Zifferneingaben mit dem gemeinsamen
/// <see cref="CharacterAccumulationWorkflowDriver"/>. Sie hält den Puffer aus
/// Ziffernblöcken und verwendet die bestehenden Funktionen für Befehle,
/// Ablehnungen und Aufnahmen. Die Schleife und der Abschluss nach fehlender
/// Sprache bleiben beim gemeinsamen Ablauf. Pro Feldeingabe entsteht eine
/// neue Instanz mit eigenem Puffer.
/// </summary>
internal sealed class DigitsAccumulationStrategy : ICharacterAccumulationStrategy
{
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly DigitsWorkflowCommandResolver _commandResolver;
    private readonly DigitsWorkflowCommandActionFlow _commandActionFlow;
    private readonly DigitsWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly DigitsWorkflowTranscriptCapture _transcriptCapture;
    private readonly DigitsWorkflowBuffer _buffer = new();

    // Den Vertrauenswert einer Aufnahme erst zusammen mit einem übernommenen
    // Ziffernblock speichern. Abgelehnte Eingaben und reine Befehle tragen
    // nichts zum Minimum bei. Gelöschte Blöcke verlieren auch ihren Vertrauenswert.
    // Das gilt ebenso für die anfängliche Aufnahme.
    private int? _pendingTurnConfidencePercent;

    // Nach der Nachfrage bei fehlender Sprache die nächste Aufnahme ohne
    // zusätzliche Eingabeaufforderung beginnen. Das Kennzeichen gilt einmalig.
    private bool _suppressNextCapturePrompt;

    public DigitsAccumulationStrategy(
        IFormTranscriptParser transcriptParser,
        DigitsWorkflowCommandResolver commandResolver,
        DigitsWorkflowCommandActionFlow commandActionFlow,
        DigitsWorkflowInputRejectionFlow inputRejectionFlow,
        DigitsWorkflowTranscriptCapture transcriptCapture,
        int? initialConfidencePercent)
    {
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _transcriptCapture = transcriptCapture ?? throw new ArgumentNullException(nameof(transcriptCapture));
        _pendingTurnConfidencePercent = initialConfidencePercent;
    }

    public bool HasContent => _buffer.HasDigits;

    // Minimum der vorhandenen Vertrauenswerte aller verbleibenden Blöcke.
    // Fehlen sämtliche Angaben, bleibt es unbekannt.
    public int? AggregatedConfidencePercent => _buffer.GetSurvivingMinimumConfidencePercent();

    public string GetFinalizedValue() => _buffer.GetDigits();

    private void HarvestDigitsRidingWithFinalize(
        CharacterAccumulationContext context,
        string transcript,
        string finalizeCommandId)
    {
        if (!CharacterAccumulationFinalizeSuffix.TryStrip(
                context,
                transcript,
                finalizeCommandId,
                out var remainderBeforeFinalize))
        {
            return;
        }

        var parseOutcome = _transcriptParser.ParseDigits(context.Context.IsoCode, remainderBeforeFinalize);
        if (!parseOutcome.Success || string.IsNullOrWhiteSpace(parseOutcome.CanonicalValue))
        {
            return;
        }

        _buffer.Append(parseOutcome.CanonicalValue!, _pendingTurnConfidencePercent);
        _pendingTurnConfidencePercent = null;
    }

    public async Task<CharacterAccumulationTurnOutcome> ProcessTurnAsync(
        CharacterAccumulationContext context,
        string transcript,
        CancellationToken cancellationToken)
    {
        if (CharacterAccumulationAnchoredCommandFlow.IsBareAnchor(context, transcript))
        {
            await context.AnchoredCommands.SpeakAnchorHintAsync(context, cancellationToken).ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        // Aktivierungswort und Befehlspräfix vor der Moduserkennung entfernen,
        // damit sie bei einem Abschlussbefehl nicht als Zifferninhalt übernommen werden.
        var sourceResolution = context.AnchoredCommands.ResolveTranscriptSource(context, transcript);
        var commandTranscript = sourceResolution.Source.HasExplicitCommandPrefix
            ? sourceResolution.CommandTranscript
            : transcript;
        if (string.IsNullOrWhiteSpace(commandTranscript))
        {
            await context.AnchoredCommands.SpeakAnchorHintAsync(context, cancellationToken).ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        if (_commandResolver.TryResolve(
                context.Context,
                commandTranscript,
                out var command,
                out var isIsolatedCommandSignal,
                out var globalCommandId))
        {
            // Wurde ein Abschlusswort gemeinsam mit Ziffern erkannt, zuerst den
            // Abschlussbefehl abtrennen und die verbleibenden Ziffern übernehmen.
            // Continue schließt anschließend den nichtleeren Puffer ab.
            if (command == DigitsWorkflowCommand.Continue
                && !isIsolatedCommandSignal
                && globalCommandId is { } finalizeCommandId)
            {
                HarvestDigitsRidingWithFinalize(context, commandTranscript, finalizeCommandId);
            }

            return await _commandActionFlow.HandleAsync(
                    command,
                    _buffer,
                    context.Context,
                    context.Field,
                    context.Session,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Andere Befehle mit explizitem Präfix über die gemeinsame Feldsteuerung
        // ausführen. Wird kein Befehl erkannt, die Eingabe ablehnen und nicht
        // als Zifferninhalt übernehmen.
        if (sourceResolution.Source.HasExplicitCommandPrefix)
        {
            var anchoredOutcome = await context.AnchoredCommands
                .TryExecuteAsync(context, sourceResolution, cancellationToken)
                .ConfigureAwait(false);
            if (anchoredOutcome is { } outcome)
            {
                return outcome;
            }

            await _inputRejectionFlow.HandleAsync(
                    context.Context,
                    context.Field,
                    context.Session,
                    context.StateTracker,
                    context.Escalation,
                    transcript,
                    cancellationToken)
                .ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        var parseOutcome = _transcriptParser.ParseDigits(context.Context.IsoCode, transcript);
        if (!parseOutcome.Success)
        {
            await _inputRejectionFlow.HandleAsync(
                    context.Context,
                    context.Field,
                    context.Session,
                    context.StateTracker,
                    context.Escalation,
                    transcript,
                    cancellationToken)
                .ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        _buffer.Append(parseOutcome.CanonicalValue!, _pendingTurnConfidencePercent);
        _pendingTurnConfidencePercent = null;
        context.AutoHelp.Reset();
        context.Escalation.Reset();
        return CharacterAccumulationTurnOutcome.AwaitNextTurn();
    }

    public async Task<CharacterAccumulationTurnCapture> CaptureNextTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        _pendingTurnConfidencePercent = null;
        var listeningCueOnly = _suppressNextCapturePrompt;
        _suppressNextCapturePrompt = false;
        var turnCapture = await _transcriptCapture.CaptureWorkflowTranscriptAsync(
                context.Context,
                context.Session,
                context.StateTracker,
                _buffer.GetSpokenDigits(),
                listeningCueOnly,
                cancellationToken)
            .ConfigureAwait(false);

        // Den Vertrauenswert vorläufig halten. Er wird erst beim Übernehmen
        // eines Ziffernblocks dessen Beitrag zum Gesamtwert zugeordnet.
        _pendingTurnConfidencePercent = turnCapture.SttConfidencePercent;
        return turnCapture;
    }

    public Task AnnounceContinueOrDoneAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _suppressNextCapturePrompt = true;
        return _transcriptCapture.SpeakContinueOrDoneAsync(context.Context, cancellationToken);
    }

    public Task AnnounceRecognizerUnavailableAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _transcriptCapture.SpeakRecognizerUnavailableAsync(context.Context, cancellationToken);
    }
}
