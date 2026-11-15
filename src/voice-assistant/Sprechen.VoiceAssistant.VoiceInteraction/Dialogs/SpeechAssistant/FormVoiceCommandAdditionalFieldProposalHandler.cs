using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bietet zusätzlich erkannte Feldwerte einzeln zur Übernahme an und lässt
/// bei unklarer Antwort eine Rückfrage zu. Der Formularablauf entscheidet über
/// Zulässigkeit und Anwendung. Die Telemetrie meldet eine Annahme nur bei
/// erfolgreicher Übernahme. Ungenutzte Übernahmeberechtigungen werden freigegeben.
/// </summary>
internal sealed class FormVoiceCommandAdditionalFieldProposalHandler
{
    private readonly IEvaluationCommandTelemetry _commandTelemetry;
    private readonly FormVoiceCommandConfirmationTranscriptCapture _transcriptCapture;
    private readonly IFormTranscriptParser _transcriptParser;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandAdditionalFieldProposalHandler(
        IEvaluationCommandTelemetry commandTelemetry,
        FormVoiceCommandConfirmationTranscriptCapture transcriptCapture,
        IFormTranscriptParser transcriptParser,
        IFormVoiceCommandSpeechService speechService)
    {
        _commandTelemetry = commandTelemetry ?? throw new ArgumentNullException(nameof(commandTelemetry));
        _transcriptCapture = transcriptCapture ?? throw new ArgumentNullException(nameof(transcriptCapture));
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task ProposeAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string transcript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        if (turnContext.Interpreter is not IFormDialogAdditionalFieldProposalRuntime proposalRuntime
            || string.IsNullOrWhiteSpace(transcript))
        {
            return;
        }

        var candidates = await proposalRuntime.ExtractAdditionalFieldCandidatesAsync(transcript).ConfigureAwait(false);
        if (candidates.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (var candidate in candidates)
        {
            var continueOffering = await OfferCandidateAsync(
                    turnContext,
                    proposalRuntime,
                    candidate,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!continueOffering)
            {
                return;
            }
        }
    }

    private async Task<bool> OfferCandidateAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        IFormDialogAdditionalFieldProposalRuntime proposalRuntime,
        FormAdditionalFieldValueCandidate candidate,
        CancellationToken cancellationToken)
    {
        var preparation = await proposalRuntime
            .PrepareAdditionalFieldProposalAsync(candidate)
            .ConfigureAwait(false);

        // Auch eine Berechtigung ohne verwendbaren Angebotstext muss zurückgegeben
        // werden. Sobald ein Token vorliegt, erfolgt die weitere Verarbeitung
        // deshalb im try/finally-Bereich.
        if (preparation.CommitToken is not { } commitToken)
        {
            await SpeakUnavailableAsync(turnContext, candidate, cancellationToken).ConfigureAwait(false);
            RecordOutcome(turnContext, candidate, accepted: false);
            return true;
        }

        try
        {
            if (preparation.Status != FormAdditionalFieldProposalPreparationStatus.Prepared
                || string.IsNullOrWhiteSpace(preparation.SpokenOffer))
            {
                await SpeakUnavailableAsync(turnContext, candidate, cancellationToken).ConfigureAwait(false);
                RecordOutcome(turnContext, candidate, accepted: false);
                return true;
            }

            var (accepted, exitRemaining) = await CaptureAcceptanceAsync(
                    turnContext,
                    candidate,
                    preparation.SpokenOffer,
                    cancellationToken)
                .ConfigureAwait(false);

            if (accepted is null || exitRemaining)
            {
                await SpeakAsync(
                        turnContext,
                        FormAdditionalFieldProposalPromptBuilder.GetSkippedPrompt(
                            turnContext.Context.Contract,
                            turnContext.Context.IsoCode,
                            candidate.FieldLabel,
                            preparation.SpokenOffer,
                            turnContext.Context.VoiceSettings.GetPolitenessLevel()),
                        cancellationToken)
                    .ConfigureAwait(false);
                RecordOutcome(turnContext, candidate, accepted: false);
                return !exitRemaining;
            }

            if (accepted == false)
            {
                RecordOutcome(turnContext, candidate, accepted: false);
                return true;
            }

            var adoption = await proposalRuntime
                .CommitAdditionalFieldProposalAsync(commitToken)
                .ConfigureAwait(false);

            // Die Ergebnisansage vor der Telemetrie anfordern. Scheitert dieser Aufruf
            // mit einer Ausnahme, wird kein Telemetrieergebnis mehr gemeldet.
            await SpeakAsync(
                    turnContext,
                    BuildAdoptionPrompt(turnContext, adoption, candidate, preparation.SpokenOffer),
                    cancellationToken)
                .ConfigureAwait(false);
            RecordOutcome(
                turnContext,
                candidate,
                accepted: adoption.Status == FormAdditionalFieldValueAdoptionStatus.Applied);
            return true;
        }
        finally
        {
            // Jeder Ausgang außer der erfolgreichen Übernahme muss die Berechtigung zurückgeben,
            // auch Abbruch und Fehler. Der Aufruf ist wiederholbar und nach einer Übernahme wirkungslos.
            await proposalRuntime.AbandonAdditionalFieldProposalAsync(commitToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Liefert <c>true</c> für ein klares Ja, <c>false</c> für ein klares Nein und <c>null</c>,
    /// wenn auch die eine Rückfrage keine zuordenbare Antwort ergab. Ein verankerter Abbruch
    /// beendet die restlichen Kandidaten, statt das Wort „Okay" als Zustimmung zu lesen.
    /// </summary>
    private async Task<(bool? Accepted, bool ExitRemaining)> CaptureAcceptanceAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormAdditionalFieldValueCandidate candidate,
        string spokenOfferValue,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        var offerPrompt = FormAdditionalFieldProposalPromptBuilder.GetProposalPrompt(
            context.Contract,
            context.IsoCode,
            candidate.FieldLabel,
            spokenOfferValue,
            politenessLevel);

        var first = await CaptureDecisionAsync(turnContext, offerPrompt, cancellationToken)
            .ConfigureAwait(false);
        if (first.ExitRemaining)
        {
            return (false, true);
        }

        if (first.Decision is not null)
        {
            return (first.Decision, false);
        }

        var reaskPrompt = FormAdditionalFieldProposalPromptBuilder.GetProposalReaskPrompt(
            context.Contract,
            context.IsoCode,
            candidate.FieldLabel,
            spokenOfferValue,
            first.Reason,
            politenessLevel);

        var repeated = await CaptureDecisionAsync(turnContext, reaskPrompt, cancellationToken)
            .ConfigureAwait(false);
        return (repeated.Decision, repeated.ExitRemaining);
    }

    private async Task<(bool? Decision, ConfirmationUndecidedReason Reason, bool ExitRemaining)> CaptureDecisionAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string prompt,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        var decisionTranscript = await _transcriptCapture.CaptureAsync(
                context,
                turnContext.Session,
                turnContext.StateTracker,
                prompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(decisionTranscript))
        {
            return (null, ConfirmationUndecidedReason.Silence, false);
        }

        var normalized = decisionTranscript.Trim();
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();
        if (FormGlobalCommandParser.TryParse(
                context.Contract,
                normalized,
                context.IsoCode,
                politenessLevel,
                out var commandId,
                out _))
        {
            var anchored = FormVoiceCommandCommandSemantics.ShouldExecuteParsedCommand(
                context.Contract,
                context.IsoCode,
                politenessLevel,
                FormGlobalCommandParser.TryStripCommandPrefix(
                    context.Contract,
                    normalized,
                    context.IsoCode,
                    politenessLevel,
                    out _),
                isLiteralMode: false,
                normalized,
                commandId,
                context.AcceptedCommandAnchorForms);
            if (anchored)
            {
                if (string.Equals(commandId, GlobalCommandIds.Cancel, StringComparison.Ordinal))
                {
                    return (false, ConfirmationUndecidedReason.Unrecognized, true);
                }

                return (null, ConfirmationUndecidedReason.Unrecognized, false);
            }
        }

        var yesNoOutcome = _transcriptParser.ParseYesNo(context.IsoCode, normalized);
        if (!yesNoOutcome.Success)
        {
            return (null, ConfirmationUndecidedReason.Unrecognized, false);
        }

        return (
            string.Equals(yesNoOutcome.CanonicalValue, "true", StringComparison.Ordinal),
            ConfirmationUndecidedReason.Unrecognized,
            false);
    }

    private Task SpeakAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string prompt,
        CancellationToken cancellationToken)
        => _speechService.SpeakSafeAsync(
            prompt,
            turnContext.Context.IsoCode,
            turnContext.Context.VoiceSettings,
            cancellationToken);

    /// <summary>
    /// Fordert die Meldung für ein nicht verfügbares Ziel ohne Feldwert an.
    /// Ohne vorbereitetes Angebot fehlt ein zum Vorlesen freigegebener Werttext.
    /// Deshalb wird nur die Feldbezeichnung übergeben.
    /// </summary>
    private Task SpeakUnavailableAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormAdditionalFieldValueCandidate candidate,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        return SpeakAsync(
            turnContext,
            FormAdditionalFieldProposalPromptBuilder.GetUnavailablePrompt(
                context.Contract,
                context.IsoCode,
                candidate.FieldLabel,
                fieldValue: string.Empty,
                context.VoiceSettings.GetPolitenessLevel()),
            cancellationToken);
    }

    private static string BuildAdoptionPrompt(
        FormVoiceCommandFieldTurnContext turnContext,
        FormAdditionalFieldValueAdoptionResult adoption,
        FormAdditionalFieldValueCandidate candidate,
        string spokenValue)
    {
        var context = turnContext.Context;
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        // Bei einer verletzten Bedingung deren modellierte Meldung verwenden,
        // damit die Rückmeldung den konkreten Grund der Ablehnung benennt.
        if (adoption.Status == FormAdditionalFieldValueAdoptionStatus.BlockedByValidation
            && adoption.BlockingViolation is { } violation)
        {
            return FormAdditionalFieldProposalPromptBuilder.GetBlockedByValidationPrompt(
                context.Contract,
                context.IsoCode,
                candidate.FieldLabel,
                spokenValue,
                violation,
                politenessLevel);
        }

        return adoption.Status switch
        {
            FormAdditionalFieldValueAdoptionStatus.Applied => FormAdditionalFieldProposalPromptBuilder.GetAppliedPrompt(
                context.Contract, context.IsoCode, candidate.FieldLabel, spokenValue, politenessLevel),
            FormAdditionalFieldValueAdoptionStatus.Unavailable => FormAdditionalFieldProposalPromptBuilder.GetUnavailablePrompt(
                context.Contract, context.IsoCode, candidate.FieldLabel, spokenValue, politenessLevel),
            _ => FormAdditionalFieldProposalPromptBuilder.GetInvalidPrompt(
                context.Contract, context.IsoCode, candidate.FieldLabel, spokenValue, politenessLevel)
        };
    }

    // Die Telemetrie folgt dem Anwendungsergebnis, nicht der Zustimmung: eine Zusage, deren Wert
    // das Formular nicht übernommen hat, ist keine angenommene Deutung.
    private void RecordOutcome(
        FormVoiceCommandFieldTurnContext turnContext,
        FormAdditionalFieldValueCandidate candidate,
        bool accepted)
        => _commandTelemetry.TrackAmbiguityResolved(
            turnContext.Session,
            TelemetryAmbiguityTypes.FieldNameVsContent,
            TelemetryAmbiguityResolutionStrategies.OptInProposal,
            TelemetryAmbiguitySignalClasses.Clear,
            accepted ? TelemetryAmbiguityResults.Accepted : TelemetryAmbiguityResults.Corrected,
            candidate.FieldId,
            commandId: null);
}
