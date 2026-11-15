namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Sammelt Tag, Monat und Jahr in einzeln angekündigten Zügen und übergibt ein
/// zusammengesetztes Datum an die gemeinsame Wertanwendung. Der Dialog erlaubt den
/// Abbruch und ein vollständig gelesenes Datum als Ausstieg aus der Teilsammlung.
/// Bei fehlendem oder ungültigem Teil wird dessen Erfassung erneut angefordert.
/// </summary>
internal sealed class DateComponentEntryWorkflowDriver
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormDateComponentParser _componentParser;

    public DateComponentEntryWorkflowDriver(
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser transcriptParser,
        IFormDateComponentParser componentParser)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _componentParser = componentParser ?? throw new ArgumentNullException(nameof(componentParser));
    }

    public async Task<DateComponentEntryResult> RunAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        string initialTranscript,
        int? initialConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(stateTracker);
        ArgumentNullException.ThrowIfNull(autoHelp);

        // Die anfängliche Äußerung nur als vollständiges Datum übernehmen. Ein einzelner
        // Bestandteil wird erst nach seiner Ansage gesammelt. Nur ein übernommenes Datum
        // trägt hier seine Erkennungssicherheit zum Ergebnis bei.
        if (!string.IsNullOrWhiteSpace(initialTranscript)
            && TryFullDateEscape(context.IsoCode, initialTranscript, out var escapedIso))
        {
            return DateComponentEntryResult.Captured(escapedIso, initialConfidencePercent);
        }

        var buffer = new DateComponentWorkflowBuffer(context.IsoCode);
        DateComponentReaskReason? dayFirstPromptReason = null;

        while (true)
        {
            foreach (var slot in buffer.Order)
            {
                if (buffer.IsCollected(slot))
                {
                    continue;
                }

                var firstReason = slot == DateComponentSlot.Day ? dayFirstPromptReason : null;
                var collected = await CollectSlotAsync(
                        context, field, session, stateTracker, autoHelp, slot, firstReason, cancellationToken)
                    .ConfigureAwait(false);

                switch (collected.Kind)
                {
                    case DateComponentSlotOutcomeKind.Aborted:
                        return DateComponentEntryResult.AbortedToFieldEntry();
                    case DateComponentSlotOutcomeKind.Escaped:
                        // Ein vollständiges Datum ersetzt die bisher gesammelten Teile. Seine
                        // Erkennungssicherheit stammt ausschließlich aus diesem Aufnahmezug.
                        return DateComponentEntryResult.Captured(collected.EscapedIso!, collected.SttConfidencePercent);
                }

                buffer.Record(slot, collected.Value, collected.SttConfidencePercent);
                if (slot == DateComponentSlot.Day)
                {
                    dayFirstPromptReason = null;
                }
            }

            if (buffer.TryAssembleIsoDate(out var isoDate))
            {
                return DateComponentEntryResult.Captured(isoDate, buffer.GetSurvivingMinimumConfidencePercent());
            }

            // Ergeben die Teile kein gültiges Kalenderdatum, den Tag verwerfen und erneut
            // erfragen. Monat und Jahr bleiben im Puffer.
            buffer.DiscardDay();
            dayFirstPromptReason = DateComponentReaskReason.ImpossibleDate;
        }
    }

    private async Task<DateComponentSlotCollectionResult> CollectSlotAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        DateComponentSlot slot,
        DateComponentReaskReason? firstPromptReason,
        CancellationToken cancellationToken)
    {
        var politeness = context.VoiceSettings.GetPolitenessLevel();
        var reason = firstPromptReason;

        while (true)
        {
            var prompt = reason is null
                ? FormDateComponentPromptBuilder.GetSlotPrompt(context.Contract, context.IsoCode, politeness, slot)
                : FormDateComponentPromptBuilder.GetReaskPrompt(context.Contract, context.IsoCode, politeness, slot, reason.Value);

            var (transcript, sttConfidencePercent) = await CaptureAsync(context, session, stateTracker, prompt, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(transcript))
            {
                // Bei fehlender verwertbarer Eingabe denselben Bestandteil erneut anfordern
                // und die automatische Hilfe fortschreiben.
                await autoHelp.RegisterFailureAsync(field.FieldId, cancellationToken).ConfigureAwait(false);
                reason = null;
                continue;
            }

            if (IsAbortCommand(context, transcript))
            {
                return DateComponentSlotCollectionResult.Aborted();
            }

            if (TryFullDateEscape(context.IsoCode, transcript, out var escapedIso))
            {
                return DateComponentSlotCollectionResult.Escaped(escapedIso, sttConfidencePercent);
            }

            var outcome = _componentParser.ParseDateComponent(context.IsoCode, slot, transcript);
            switch (outcome.Status)
            {
                case DateComponentParseStatus.Parsed:
                    autoHelp.Reset();
                    return DateComponentSlotCollectionResult.Collected(outcome.Value, sttConfidencePercent);
                case DateComponentParseStatus.OutOfRange:
                    reason = DateComponentReaskReason.OutOfRange;
                    break;
                case DateComponentParseStatus.YearNotFourDigits:
                    reason = DateComponentReaskReason.YearNotFourDigits;
                    break;
                case DateComponentParseStatus.YearUncertain:
                    reason = DateComponentReaskReason.YearUncertain;
                    break;
                default:
                    reason = DateComponentReaskReason.NotRecognized;
                    break;
            }
        }
    }

    private async Task<(string? Transcript, int? SttConfidencePercent)> CaptureAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string prompt,
        CancellationToken cancellationToken)
    {
        try
        {
            // Für die Aufnahme jedes Bestandteils das Ziffernprofil anfordern.
            // Wie der Erkenner die Aufnahme tatsächlich transkribiert, entscheidet dieser Aufruf nicht.
            var captureResult = await _speechService.CaptureTranscriptAsync(
                    prompt,
                    context.IsoCode,
                    context.CaptureSettings,
                    onStartListening: stateTracker.BeginListening,
                    new SpeechCaptureTelemetryContext(session),
                    RecognitionProfilePolicy.Resolve(InputMode.Digits),
                    keyterms: null,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(captureResult.FailureCode))
            {
                // Ein gemeldeter Aufnahmefehler liefert hier weder Transkript noch
                // Erkennungssicherheit. Der Sammelablauf behandelt das als fehlende Eingabe.
                return (null, null);
            }

            return (captureResult.Transcript, captureResult.SttConfidencePercent);
        }
        catch (NoSpeechDetectedException)
        {
            return (null, null);
        }
        catch (Exception ex) when (RecoverableCaptureFault.IsTransientCaptureFault(ex))
        {
            // Einen als vorübergehend eingeordneten Aufnahmefehler ohne Transkript
            // zurückgeben, damit der Sammelablauf denselben Bestandteil erneut erfragt.
            return (null, null);
        }
    }

    private bool TryFullDateEscape(string isoCode, string transcript, out string isoDate)
    {
        isoDate = string.Empty;
        var trimmed = transcript.Trim();

        // Eine einzelne reine Ziffernfolge nicht als vollständiges Datum übernehmen.
        // Andere Texte dürfen aus der Teilsammlung aussteigen, wenn der Datumsparser sie akzeptiert.
        if (!trimmed.Contains(' ', StringComparison.Ordinal) && trimmed.Length > 0 && trimmed.All(char.IsDigit))
        {
            return false;
        }

        var outcome = _transcriptParser.ParseDate(isoCode, trimmed);
        if (outcome.Success)
        {
            isoDate = outcome.CanonicalValue!;
            return true;
        }

        return false;
    }

    private static bool IsAbortCommand(FormVoiceCommandContext context, string transcript)
        => FormGlobalCommandParser.TryParse(
               context.Contract,
               transcript.Trim(),
               context.IsoCode,
               context.VoiceSettings.GetPolitenessLevel(),
               out var commandId,
               out _)
           && string.Equals(commandId, GlobalCommandIds.Cancel, StringComparison.Ordinal);
}
