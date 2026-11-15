using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt Optionen einer Mehrfachauswahl über den gemeinsamen
/// <see cref="CharacterAccumulationWorkflowDriver"/>. Weitere erkannte Optionen
/// werden ergänzt. Befehle werden gesondert aufgelöst und ausgeführt.
/// Der <see cref="SetWorkflowBuffer"/> hält Auswahl und Konfidenzen beitragender
/// Eingabeschritte. Nicht zuordenbare Eingaben lösen eine Rückmeldung aus.
/// Für jeden neuen Feldzug wird eine eigene Strategie mit leerem Puffer erzeugt.
/// </summary>
internal sealed class SetAccumulationStrategy : ICharacterAccumulationStrategy
{
    private readonly SetWorkflowCommandResolver _commandResolver;
    private readonly SetKeytermVocabulary _keyterms = new();
    private readonly SetWorkflowCommandActionFlow _commandActionFlow;
    private readonly SetWorkflowInputRejectionFlow _inputRejectionFlow;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly ImmutableArray<FormChoiceOption> _options;
    private readonly SetWorkflowBuffer _buffer = new();

    // Lokalisierte Optionsbezeichnungen als Schlüsselbegriffe für die Erkennung bereitstellen.
    private readonly FieldChoiceKeytermVocabulary _choiceKeyterms = new();

    // Konfidenz des zuletzt erfassten Schritts zunächst zurückhalten. Nur wenn
    // mindestens eine neue Option hinzukommt, zählt sie zum Minimum der Auswahl.
    // Abgelehnte Eingaben, reine Befehle und doppelte Nennungen verändern dieses Minimum nicht.
    private int? _pendingTurnConfidencePercent;

    public SetAccumulationStrategy(
        SetWorkflowCommandResolver commandResolver,
        SetWorkflowCommandActionFlow commandActionFlow,
        SetWorkflowInputRejectionFlow inputRejectionFlow,
        IFormTranscriptParser transcriptParser,
        IFormVoiceCommandSpeechService speechService,
        ImmutableArray<FormChoiceOption> options,
        int? initialConfidencePercent)
    {
        _commandResolver = commandResolver ?? throw new ArgumentNullException(nameof(commandResolver));
        _commandActionFlow = commandActionFlow ?? throw new ArgumentNullException(nameof(commandActionFlow));
        _inputRejectionFlow = inputRejectionFlow ?? throw new ArgumentNullException(nameof(inputRejectionFlow));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _options = options;
        _pendingTurnConfidencePercent = initialConfidencePercent;
    }

    public bool HasContent => _buffer.HasContent;

    public int? AggregatedConfidencePercent => _buffer.GetSurvivingMinimumConfidencePercent();

    public string GetFinalizedValue() => _buffer.GetFinalizedValue();

    public async Task<CharacterAccumulationTurnOutcome> ProcessTurnAsync(
        CharacterAccumulationContext context,
        string transcript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (CharacterAccumulationAnchoredCommandFlow.IsBareAnchor(context, transcript))
        {
            await context.AnchoredCommands.SpeakAnchorHintAsync(context, cancellationToken).ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        // Das Aktivierungswort vor der Befehlsauflösung entfernen, damit danach
        // ein alleinstehendes Abschlusswort erkannt werden kann.
        var sourceResolution = context.AnchoredCommands.ResolveTranscriptSource(context, transcript);
        var commandTranscript = sourceResolution.Source.HasExplicitCommandPrefix
            ? sourceResolution.CommandTranscript
            : transcript;
        if (string.IsNullOrWhiteSpace(commandTranscript))
        {
            await context.AnchoredCommands.SpeakAnchorHintAsync(context, cancellationToken).ConfigureAwait(false);
            return CharacterAccumulationTurnOutcome.AwaitNextTurn();
        }

        // Befehle des Auswahlmodus zuerst behandeln. Übernehmen und Abschließen
        // beenden die Auswahl dieses Felds, nicht das gesamte Formular.
        // Weitere globale Befehle können anschließend mit Aktivierungswort behandelt werden.
        if (_commandResolver.TryResolve(context.Context, commandTranscript, out var command, out var isIsolatedCommandSignal))
        {
            // Bei einem Abschlussbefehl mit weiterem Text zunächst darin enthaltene
            // Optionen ergänzen. Ein alleinstehender Abschlussbefehl überspringt diese Prüfung.
            if (command is SetWorkflowCommand.Complete or SetWorkflowCommand.AcceptSelection && !isIsolatedCommandSignal)
            {
                // Falls der vollständige Text keine Optionen ergibt, ein erkanntes
                // Abschlusswort am Ende abtrennen und den verbleibenden Text erneut prüfen.
                if (!TryAccumulateOptions(context, commandTranscript)
                    && CharacterAccumulationFinalizeSuffix.TryStrip(context, commandTranscript, ResolveFinalizeCommandId(command), out var remainderBeforeFinalize))
                {
                    TryAccumulateOptions(context, remainderBeforeFinalize);
                }
            }

            // Der gemeinsame Befehlsablauf entscheidet über Abschluss oder Fortsetzung
            // und protokolliert Korrekturen. Für die Auswahlansage verwendet er diese Strategie.
            return await _commandActionFlow
                .HandleAsync(command, HasContent, context, AnnounceContinueOrDoneAsync, cancellationToken)
                .ConfigureAwait(false);
        }

        // Ein mit Aktivierungswort eingeleiteter Text wird anschließend als globaler
        // Befehl behandelt. Schlägt die Zuordnung fehl, bleibt er von der Auswahl ausgeschlossen.
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

        if (!TryAccumulateOptions(context, transcript))
        {
            // Nicht zuordenbare Eingaben an die gemeinsame Fehlerbehandlung weitergeben,
            // damit eine Rückmeldung angefordert wird.
            await _inputRejectionFlow.HandleAsync(
                    context.Context,
                    context.Field,
                    context.Session,
                    context.StateTracker,
                    context.Escalation,
                    transcript,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return CharacterAccumulationTurnOutcome.AwaitNextTurn();
    }

    /// <summary>
    /// Ergänzt die erkannten Optionscodes und setzt bei erfolgreicher Zuordnung
    /// die Zähler für automatische Hilfe und Fehlerrückmeldungen zurück.
    /// Auch eine bereits enthaltene Option gilt dabei als erfolgreich zugeordnet.
    /// </summary>
    private bool TryAccumulateOptions(CharacterAccumulationContext context, string transcript)
    {
        var parseOutcome = _transcriptParser.ParseChoice(
            context.Context.IsoCode,
            transcript.Trim(),
            _options,
            allowMultiple: true);
        if (!parseOutcome.Success || string.IsNullOrWhiteSpace(parseOutcome.CanonicalValue))
        {
            return false;
        }

        // Nur neu hinzugekommene Optionen tragen Konfidenz bei. Eine doppelte Nennung
        // gilt weiterhin als zugeordnet und setzt die Fehlerzähler zurück.
        _buffer.TryAdd(
            parseOutcome.CanonicalValue!.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            _pendingTurnConfidencePercent);
        _pendingTurnConfidencePercent = null;

        context.AutoHelp.Reset();
        context.Escalation.Reset();
        return true;
    }

    // Die passende globale Befehlskennung für die Prüfung eines Abschlussworts
    // am Textende wählen: AcceptValue für Übernahme, sonst Submit.
    private static string ResolveFinalizeCommandId(SetWorkflowCommand command)
        => command is SetWorkflowCommand.AcceptSelection ? GlobalCommandIds.AcceptValue : GlobalCommandIds.Submit;

    public async Task<CharacterAccumulationTurnCapture> CaptureNextTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        _pendingTurnConfidencePercent = null;
        try
        {
            var captureResult = await CaptureSetTurnAsync(context, cancellationToken).ConfigureAwait(false);

            // Die Konfidenz erst bei einer tatsächlich neu hinzugekommenen Option berücksichtigen.
            _pendingTurnConfidencePercent = captureResult.SttConfidencePercent;
            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                // Ein Fehlercode im Ergebnis erreicht keinen catch-Block. Ihn deshalb als
                // Erkennungsstörung behandeln, damit er nicht den Abschluss nach Stille auslöst.
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
            return CharacterAccumulationTurnCapture.RecognizerUnavailable;
        }
    }

    private Task<SpeechCaptureResult> CaptureSetTurnAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        var prompt = BuildLoopPrompt(context);
        var telemetryContext = new SpeechCaptureTelemetryContext(context.Session);
        var recognitionProfile = RecognitionProfilePolicy.Resolve(InputMode.Voice);

        // Optionsbezeichnungen als Erkennungshinweise verwenden. Ohne auflösbare
        // Optionen wird die Erfassung ohne Schlüsselbegriffe angefordert.
        var choiceKeyterms = _choiceKeyterms.GetKeyterms(_options.IsDefaultOrEmpty ? [] : _options);
        return choiceKeyterms.Count == 0
            ? _speechService.CaptureTranscriptSingleAttemptAsync(
                prompt,
                context.Context.IsoCode,
                context.Context.CaptureSettings,
                onStartListening: context.StateTracker.BeginListening,
                telemetryContext,
                recognitionProfile,
                keyterms: null,
                cancellationToken)
            : _speechService.CaptureTranscriptSingleAttemptAsync(
                prompt,
                context.Context.IsoCode,
                context.Context.CaptureSettings,
                onStartListening: context.StateTracker.BeginListening,
                telemetryContext,
                recognitionProfile,
                // Nur Optionsbezeichnungen, Verbindungswörter und unterstützte Auswahlbefehle
                // übergeben. Den allgemeinen Serverwortschatz dabei nicht ergänzen.
                new SpeechKeyterms(
                    _keyterms.GetKeyterms(
                        context.Context.Contract,
                        context.Context.IsoCode,
                        context.Context.VoiceSettings.GetPolitenessLevel(),
                        choiceKeyterms),
                    KeytermComposition.WithoutServerBase),
                cancellationToken);
    }

    public Task AnnounceContinueOrDoneAsync(
        CharacterAccumulationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _speechService.SpeakSafeAsync(
            BuildLoopPrompt(context),
            context.Context.IsoCode,
            context.Context.VoiceSettings,
            cancellationToken);
    }

    // Bei vorhandener Auswahl deren Bestätigung anfordern. Für einen leeren Puffer
    // stattdessen den Hinweis auf die fehlende Auswahl mit einer Beispieloption wählen.
    private string BuildLoopPrompt(CharacterAccumulationContext context)
        => HasContent
            ? FormSetWorkflowPromptBuilder.GetSetSelectionConfirmPrompt(
                context.Context.Contract,
                context.Context.IsoCode,
                context.Field.Label,
                _buffer.GetSpokenSelection(_options),
                context.Context.VoiceSettings.GetPolitenessLevel())
            : FormSetWorkflowPromptBuilder.GetSetNoneSelectedYetPrompt(
                context.Context.Contract,
                context.Context.IsoCode,
                ResolveFirstOptionExample(),
                context.Context.VoiceSettings.GetPolitenessLevel());

    private string ResolveFirstOptionExample()
        => _options.IsDefaultOrEmpty ? string.Empty : _options[0].DisplayName;

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
