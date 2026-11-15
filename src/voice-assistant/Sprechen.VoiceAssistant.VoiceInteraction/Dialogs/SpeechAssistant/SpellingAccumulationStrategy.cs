using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet Buchstabiereingaben über den gemeinsamen
/// <see cref="CharacterAccumulationWorkflowDriver"/>. Der Puffer hält
/// eingegebene Blöcke mit ihren Konfidenzen. Eigene Dienste behandeln
/// Befehle, abgelehnte Eingaben und die datenschutzabhängige Wiedergabe.
/// Jeder Feldzug erhält eine neue Strategie mit eigenem Puffer.
/// </summary>
internal sealed class SpellingAccumulationStrategy : ICharacterAccumulationStrategy
{
    private readonly SpellingWorkflowCommandResolver _commandResolver;
    private readonly SpellingKeytermVocabulary _keyterms = new();
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly SpellingWorkflowCommandActionFlow _commandActionFlow;
    private readonly SpellingWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly SpellingBlockReadback _blockReadback;
    private readonly SpeechInputAcceptanceFeedback _inputAcceptanceFeedback;
    private readonly SpellingWorkflowBuffer _buffer = new();

    // Nach dem Fortsetzungshinweis den nächsten Aufnahmehinweis einmal auslassen,
    // damit das gerade genannte Abschlusswort nicht unmittelbar wiederholt wird.
    private bool _suppressNextCapturePrompt;

    // Konfidenz zunächst zurückhalten und erst mit einem neuen Buchstabierblock
    // speichern. Reine Befehle und abgelehnte Eingaben zählen nicht mit.
    // Beim Löschen eines Blocks entfällt auch dessen Konfidenz aus dem Minimum.
    private int? _pendingTurnConfidencePercent;

    // Eine fehlende Protokollierung durch den leeren Empfänger ersetzen,
    // damit die Aufrufstellen keine Nullprüfung benötigen.
    private readonly IDialogDecisionSink _decisionSink;

    public SpellingAccumulationStrategy(
        SpellingWorkflowCommandResolver commandResolver,
        IFormTranscriptParser transcriptParser,
        IFormVoiceCommandSpeechService speechService,
        SpellingWorkflowCommandActionFlow commandActionFlow,
        SpellingWorkflowInputRejectionFlow inputRejectionFlow,
        SpellingBlockReadback blockReadback,
        SpeechInputAcceptanceFeedback inputAcceptanceFeedback,
        IDialogDecisionSink? decisionSink,
        int? initialConfidencePercent)
    {
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _blockReadback = blockReadback ?? throw new ArgumentNullException(nameof(blockReadback));
        if (inputAcceptanceFeedback is not SpeechInputAcceptanceFeedback.PostInterpretation
            and not SpeechInputAcceptanceFeedback.CueFree)
        {
            throw new ArgumentOutOfRangeException(
                nameof(inputAcceptanceFeedback),
                inputAcceptanceFeedback,
                "Spelling capture feedback must be owned by post-interpretation readback or be cue-free.");
        }

        _inputAcceptanceFeedback = inputAcceptanceFeedback;
        _decisionSink = decisionSink ?? NullDialogDecisionSink.Instance;
        _pendingTurnConfidencePercent = initialConfidencePercent;
    }

    public bool HasContent => _buffer.HasCharacters;

    // Niedrigste gemeldete Konfidenz der noch vorhandenen Blöcke.
    // Ohne gemeldete Konfidenz bleibt sie unbekannt.
    public int? AggregatedConfidencePercent => _buffer.GetSurvivingMinimumConfidencePercent();

    public string GetFinalizedValue() => _buffer.GetValue();

    private void HarvestCharactersRidingWithFinalize(
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

        var parseOutcome = _transcriptParser.ParseSpelling(context.Context.IsoCode, remainderBeforeFinalize);
        if (!parseOutcome.Success || string.IsNullOrWhiteSpace(parseOutcome.CanonicalValue))
        {
            return;
        }

        _buffer.Append(parseOutcome.CanonicalValue, _pendingTurnConfidencePercent);
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

        // Das Aktivierungswort vor der Befehlsauflösung entfernen, damit es
        // weder die Erkennung eines alleinstehenden Befehls noch den Feldwert beeinflusst.
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
            // Bei einem Abschlussbefehl mit weiterem Text zuerst das Abschlusswort am Ende
            // abtrennen und nur den Rest als Buchstaben verarbeiten. Andernfalls könnten
            // Buchstaben des Abschlussworts selbst im Feldwert landen.
            if (command == SpellingWorkflowCommand.Complete
                && !isIsolatedCommandSignal
                && globalCommandId is { } finalizeCommandId)
            {
                HarvestCharactersRidingWithFinalize(context, commandTranscript, finalizeCommandId);
            }

            if (command == SpellingWorkflowCommand.Complete)
            {
                await _decisionSink
                    .RecordAsync(
                        DialogDecisions.SpellingTerminator(
                            globalCommandId ?? "local",
                            isIsolatedCommandSignal,
                            commandTranscript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length),
                        cancellationToken)
                    .ConfigureAwait(false);
                await _decisionSink
                    .RecordAsync(
                        DialogDecisions.SpellingBuffer(_buffer.GetValue().Length, _buffer.BlockCount),
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return await _commandActionFlow.HandleAsync(
                    command,
                    _buffer,
                    _blockReadback,
                    context.Context,
                    context.Field,
                    context.Session,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Mit Aktivierungswort eingeleiteten Text als globalen Befehl behandeln.
        // Bei fehlgeschlagener Zuordnung eine Rückmeldung anfordern und keine Buchstaben ergänzen.
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

        var parseOutcome = _transcriptParser.ParseSpelling(context.Context.IsoCode, transcript);
        if (!parseOutcome.Success || string.IsNullOrWhiteSpace(parseOutcome.CanonicalValue))
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

        _buffer.Append(parseOutcome.CanonicalValue, _pendingTurnConfidencePercent);
        _pendingTurnConfidencePercent = null;
        context.AutoHelp.Reset();
        context.Escalation.Reset();
        await _blockReadback
            .ReadBackBlockAsync(context.Context, context.Field, _buffer.GetLastBlock(), cancellationToken)
            .ConfigureAwait(false);
        if (_inputAcceptanceFeedback == SpeechInputAcceptanceFeedback.CueFree)
        {
            // Im Modus ohne Rückmeldeton eine inhaltsfreie Fortsetzungsaufforderung
            // mit dem Abschlusswort anfordern. Der nächste Aufnahmehinweis entfällt dort.
            await _speechService
                .SpeakSafeAsync(
                    FormSpellingWorkflowPromptBuilder.GetSpellingContinueOrDoneProtectedPrompt(
                        context.Context.Contract,
                        context.Context.IsoCode,
                        context.Context.VoiceSettings.GetPolitenessLevel()),
                    context.Context.IsoCode,
                    context.Context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return CharacterAccumulationTurnOutcome.AwaitNextTurn();
    }

    public async Task<CharacterAccumulationTurnCapture> CaptureNextTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        _pendingTurnConfidencePercent = null;
        try
        {
            var captureSettings = context.Context.CaptureSettings.WithInputAcceptanceFeedback(
                _inputAcceptanceFeedback);
            if (_inputAcceptanceFeedback == SpeechInputAcceptanceFeedback.CueFree || _suppressNextCapturePrompt)
            {
                captureSettings = captureSettings.WithPromptPresentation(
                    SpeechCapturePromptPresentation.ListeningCueOnly);
            }

            _suppressNextCapturePrompt = false;

            var captureResult = await _speechService.CaptureTranscriptSingleAttemptAsync(
                    FormSpellingWorkflowPromptBuilder.GetSpellingWorkflowPrompt(
                        context.Context.Contract,
                        context.Context.IsoCode,
                        _buffer.GetValue(),
                        context.Context.VoiceSettings.GetPolitenessLevel()),
                    context.Context.IsoCode,
                    captureSettings,
                    onStartListening: context.StateTracker.BeginListening,
                    new SpeechCaptureTelemetryContext(context.Session),
                    RecognitionProfilePolicy.Resolve(InputMode.Spelling),
                    // Sprachspezifischen Buchstabierwortschatz und Befehle ohne allgemeine
                    // Serverbegriffe übergeben. Schlüsselbegriffe sind Erkennungshinweise,
                    // keine Akzeptanzprüfung. Erwartete Feldwerte gehören nicht dazu.
                    new SpeechKeyterms(
                        _keyterms.GetKeyterms(
                            context.Context.Contract,
                            context.Context.IsoCode,
                            context.Context.VoiceSettings.GetPolitenessLevel()),
                        KeytermComposition.WithoutServerBase),
                    cancellationToken)
                .ConfigureAwait(false);

            // Konfidenz erst zusammen mit einem neu hinzugefügten Block speichern.
            _pendingTurnConfidencePercent = captureResult.SttConfidencePercent;
            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                // Einen Fehlercode im Ergebnis als Erkennungsstörung behandeln,
                // damit fehlender Text nicht den Abschluss nach Stille auslöst.
                return CharacterAccumulationTurnCapture.RecognizerUnavailable;
            }

            return string.IsNullOrWhiteSpace(captureResult.Transcript)
                ? CharacterAccumulationTurnCapture.Silence
                : CharacterAccumulationTurnCapture.Transcribed(captureResult.Transcript);
        }
        catch (NoSpeechDetectedException)
        {
            return CharacterAccumulationTurnCapture.Silence;
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Behebbare Erfassungsfehler dem gemeinsamen Treiber melden. Sie dürfen
            // nicht als Stille gelten, die den Abschluss eines vorhandenen Puffers auslösen kann.
            return CharacterAccumulationTurnCapture.RecognizerUnavailable;
        }
    }

    public Task AnnounceContinueOrDoneAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        _suppressNextCapturePrompt = true;
        // Im Modus ohne Rückmeldeton auch nach Stille die inhaltsfreie
        // Fortsetzungsaufforderung verwenden.
        var prompt = _inputAcceptanceFeedback == SpeechInputAcceptanceFeedback.CueFree
            ? FormSpellingWorkflowPromptBuilder.GetSpellingContinueOrDoneProtectedPrompt(
                context.Context.Contract,
                context.Context.IsoCode,
                context.Context.VoiceSettings.GetPolitenessLevel())
            : FormSpellingWorkflowPromptBuilder.GetSpellingContinueOrDonePrompt(
                context.Context.Contract,
                context.Context.IsoCode,
                context.Context.VoiceSettings.GetPolitenessLevel());
        return _speechService.SpeakSafeAsync(
            prompt,
            context.Context.IsoCode,
            context.Context.VoiceSettings,
            cancellationToken);
    }

    public Task AnnounceRecognizerUnavailableAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return CharacterAccumulationRecognizerUnavailableAnnouncement.SpeakAsync(
            _speechService,
            context.Context,
            cancellationToken);
    }

}
