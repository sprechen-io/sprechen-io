using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Führt das Sprachangebot am Sitzungseinstieg als Folge einzelsprachiger Fragen aus. Die bereits
/// aufgelöste Sitzungssprache wird zuerst angeboten. Danach folgen weitere Modellsprachen bis zur
/// Annahme oder zum Abbruch. Die anfängliche Sprachauflösung bleibt außerhalb dieses Ablaufs. Aufnahme
/// und Annahmeregeln haben eigene Zuständigkeiten.
/// </summary>
internal sealed class SessionEntryLanguageOfferTurn
{
    private readonly SessionEntryLanguageOfferDependencies _dependencies;
    private readonly SessionEntryQuestion _question;
    private readonly SessionEntryCancelHearing _cancelHearing;
    private readonly SessionEntryLanguageChange _languageChange;
    private readonly SessionEntryFirstContact _firstContact;
    private static readonly ILanguageCatalog Endonyms = VoiceInterfaceLanguageCatalog.CreateDefault();
    private readonly bool _enabled;

    private bool _asked;

    /// <summary>
    /// Baut den Angebotsablauf aus dem Assistentenkontext auf, damit der Sitzungsdienst die
    /// Startreihenfolge bestimmt und die benötigten Sprachdienste nicht selbst zusammensetzt.
    /// </summary>
    public static SessionEntryLanguageOfferTurn Create(SpeechAssistantContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var interaction = context.InteractionDependencies;
        return new SessionEntryLanguageOfferTurn(
            new SessionEntryLanguageOfferDependencies(
                context.SetupDependencies.UserLanguageState,
                context.SetupDependencies.UserLanguageSettingsWorkflow,
                interaction.TextToSpeechPlayer,
                interaction.SpeechCaptureRecordingIndicator,
                interaction.SpeechCaptureClient,
                interaction.WakeWordInteractionDependencies.ModeAwareSpeechCaptureClient,
                interaction.SpeechRecognitionWorker,
                context.SetupDependencies.SessionEntryCancelVocabulary,
                interaction.Logger,
                context.EvaluationSessionTelemetry),
            VoiceInterfaceModelCatalog.SessionLanguageOfferEnabled);
    }

    public SessionEntryLanguageOfferTurn(SessionEntryLanguageOfferDependencies dependencies, bool enabled)
    {
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        _question = new SessionEntryQuestion(dependencies);
        _cancelHearing = new SessionEntryCancelHearing(_question, dependencies.Logger);
        _languageChange = new SessionEntryLanguageChange(dependencies);
        _firstContact = new SessionEntryFirstContact(dependencies);

        // Die Freischaltung stammt aus dem Modell.
        _enabled = enabled;
    }

    /// <summary>
    /// Führt ein noch verfügbares Sprachangebot aus und meldet die Sprache für die
    /// Fortsetzung oder einen Abbruch. Ein deaktiviertes, bereits begonnenes oder nicht
    /// angenommenes Angebot lässt die Sitzungssprache unverändert.
    /// </summary>
    public async ValueTask<SessionEntryOutcome> DecideLanguageAsync(string selectedIsoCode, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedIsoCode);

        if (!_enabled || _asked)
        {
            // Bei deaktiviertem oder bereits begonnenem Angebot keine weitere Sprachfrage stellen.
            // Die Einstellung stammt beim Aufbau aus dem Modell.
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }

        if (SessionEntryLadder.Others(selectedIsoCode).Count == 0)
        {
            // Ohne weitere Modellsprachen entfällt das Korrekturangebot.
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }

        _asked = true;

        try
        {
            var outcome = await RunAsync(selectedIsoCode, cancellationToken).ConfigureAwait(false);
            if (outcome.Cancelled)
            {
                // Ein Abbruch am Einstieg soll den nächsten Startversuch nicht vom Angebot
                // ausschließen. Deshalb das Angebot für den nächsten Startversuch wieder freigeben.
                _asked = false;
            }

            return outcome;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ein unterbrochener Start soll die Sprachentscheidung beim nächsten Versuch erneut
            // anbieten können.
            _asked = false;
            throw;
        }
        catch (VoiceInputPausedException)
        {
            // Das Pausensignal an den Startaufrufer weitergeben, damit dieser es nicht als
            // erfolgreichen Spracheinstieg behandelt.
            _asked = false;
            throw;
        }
        catch (MicrophoneAcquisitionException)
        {
            // Den Mikrofonfehler und die Wiederholbarkeit des Angebots erhalten, statt einen nicht
            // ausführbaren Fragezug als Fortsetzung in der Vorgabesprache auszugeben.
            _asked = false;
            throw;
        }
        catch (Exception ex)
        {
            // Bei einem sonstigen Angebotsfehler bleibt die bereits aufgelöste Sprache der
            // Fortsetzungswert. Die Warnung hält diesen Ersatzweg von einer angenommenen Sprachauswahl
            // unterscheidbar.
            _dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Session-entry language decision could not be put; continuing in {IsoCode}.",
                selectedIsoCode);
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }
    }

    private async ValueTask<SessionEntryOutcome> RunAsync(string selectedIsoCode, CancellationToken cancellationToken)
    {
        var firstContact = await _firstContact.IsFirstContactAsync(selectedIsoCode, cancellationToken).ConfigureAwait(false);
        var politenessLevel = _dependencies.UserLanguageState.SelectedPolitenessLevel ?? PolitenessLevel.Formal;
        var decision = await AskEachLanguageAsync(selectedIsoCode, politenessLevel, firstContact, cancellationToken)
            .ConfigureAwait(false);

        if (decision.Cancelled)
        {
            // Der Abbruchzweig schreibt weder Sprachwechsel noch Angebotsmerker. Sprache der Frage und
            // Erkennungsweg protokollieren, damit direkte und erneute Abbrucherkennung später getrennt
            // ausgewertet werden können.
            _dependencies.Logger.LogInformation(
                OrchestrationLogEvents.SessionEntryCancelled,
                "Session entry cancelled by the participant in {IsoCode} at rung {RungIsoCode} via {ReadShape}; no session starts.",
                selectedIsoCode,
                decision.CancelRungIsoCode,
                decision.CancelReadShape);
            return SessionEntryOutcome.CancelledByTheParticipant(selectedIsoCode);
        }

        if (decision.TargetIsoCode is null)
        {
            if (decision.Answered)
            {
                // Die angenommene Sitzungssprache benötigt keinen Wechsel. Den Angebotsmerker setzen,
                // ohne die gerade angebotene Sprache erneut anzusagen.
                await _firstContact.RememberOfferHeardAsync(selectedIsoCode, cancellationToken).ConfigureAwait(false);
                return SessionEntryOutcome.ContinueIn(selectedIsoCode);
            }

            // Ohne angenommene Sprache das Ende der Fragenfolge und die verbleibende Sprache ansagen.
            // Damit erhält der weitere Einstieg eine hörbare Orientierung, auch wenn keine Frage
            // angenommen wurde.
            await SpeakKeptAsync(selectedIsoCode, selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);

            // Ohne angenommene Sprache den Angebotsmerker nicht fortschreiben. Auch gesprochene
            // Ablehnungen können zu diesem Ergebnis führen.
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }

        // Die angenommene Sprache unmittelbar an den Einstellungsablauf übergeben. Der vorangegangene
        // Fragezug hat die Annahme bereits entschieden. Hier entsteht kein zweites
        // Bestätigungsfenster.
        string continueIn;
        try
        {
            continueIn = await _languageChange
                .ApplyAcceptedLanguageAsync(decision.TargetIsoCode, selectedIsoCode, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VoiceInputPausedException)
        {
            // Wenn der Einstellungsablauf scheitert, die beibehaltene Sprache in der zuvor angebotenen
            // Zielsprache rückmelden. Auswahlziel und Fortsetzungssprache bleiben deshalb getrennte
            // Angaben.
            _dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Session-entry language {IsoCode} was accepted but could not be stored; continuing in "
                + "{SelectedIsoCode}.",
                decision.TargetIsoCode,
                selectedIsoCode);
            await SpeakKeptAsync(selectedIsoCode, decision.TargetIsoCode, cancellationToken)
                .ConfigureAwait(false);
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }

        if (string.Equals(continueIn, selectedIsoCode, StringComparison.OrdinalIgnoreCase))
        {
            // Auch eine Rückgabe der bisherigen Sprache braucht die Rückmeldung, dass der angenommene
            // Wechsel nicht zur anderen Fortsetzungssprache geführt hat.
            await SpeakKeptAsync(selectedIsoCode, decision.TargetIsoCode, cancellationToken)
                .ConfigureAwait(false);
            return SessionEntryOutcome.ContinueIn(selectedIsoCode);
        }

        // Die zurückgegebene neue Sprache vor dem Angebotsmerker ansagen. Diese Rückmeldung verlangt
        // keine weitere Antwort und macht das Ergebnis des Wechsels hörbar.
        try
        {
            await SpeakAcceptedAsync(continueIn, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not VoiceInputPausedException)
        {
            // Ein gewöhnlicher Ansagefehler darf nicht die bisherige Sprache zurückgeben, nachdem der
            // Einstellungsablauf schon eine andere gemeldet hat. Den Ausfall protokollieren und diese
            // Fortsetzungssprache beibehalten.
            _dependencies.Logger.LogWarning(
                OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                ex,
                "Session-entry acceptance of {IsoCode} could not be announced; the language stands.",
                continueIn);
        }

        // Den Merker für die Sprache schreiben, in der der Ablauf fortgesetzt wird.
        await _firstContact.RememberOfferHeardAsync(continueIn, cancellationToken).ConfigureAwait(false);
        return SessionEntryOutcome.ContinueIn(continueIn);
    }

    /// <summary>
    /// Fragt zunächst nach der Sitzungssprache und danach in Katalogreihenfolge nach weiteren Sprachen.
    /// Fehlende Annahme führt zur nächsten Frage, damit auch ein nicht verstandenes Angebot den Weg zu
    /// einer späteren Sprache offenlässt.
    /// </summary>
    private async ValueTask<SessionEntryDecision> AskEachLanguageAsync(
        string selectedIsoCode,
        PolitenessLevel politenessLevel,
        bool firstContact,
        CancellationToken cancellationToken)
    {
        var cancelUtterances = _dependencies.CancelVocabulary
            .GetCancelUtterances(selectedIsoCode, politenessLevel);

        var keepAnswer = await AskOneLanguageAsync(
                SessionLanguageOfferPromptTextResolver.GetKeepQuestionSegment(selectedIsoCode, politenessLevel),
                selectedIsoCode,
                SessionEntryAnswerWindows.ForOffer(firstContact),
                cancellationToken)
            .ConfigureAwait(false);

        if (SessionEntryAnswerReader.AcceptsTheSessionLanguage(keepAnswer.Transcript, selectedIsoCode))
        {
            return SessionEntryDecision.KeptTheSessionLanguage;
        }

        // Den Abbruch erst nach der Annahme prüfen. Bei der ersten Frage stammt das Transkript
        // bereits aus der Sitzungssprache. Hier wird keine zweite Erkennung angefordert.
        if (SessionEntryCancelReader.IsCancelled(keepAnswer.Transcript, cancelUtterances))
        {
            return SessionEntryDecision.CancelledByTheParticipant(selectedIsoCode, "keepRungTranscript");
        }

        foreach (var isoCode in SessionEntryLadder.Others(selectedIsoCode))
        {
            var answer = await AskOneLanguageAsync(
                    SessionLanguageOfferPromptTextResolver.GetLanguageQuestionSegment(isoCode, politenessLevel),
                    isoCode,
                    SessionEntryAnswerWindows.LanguageQuestion,
                    cancellationToken)
                .ConfigureAwait(false);

            if (SessionEntryAnswerReader.AcceptsTheOfferedLanguage(answer.Transcript, isoCode, selectedIsoCode))
            {
                return SessionEntryDecision.Chose(isoCode);
            }

            var cancelReadShape = await _cancelHearing
                .HearAsync(answer, selectedIsoCode, cancelUtterances, cancellationToken)
                .ConfigureAwait(false);
            if (cancelReadShape is not null)
            {
                return SessionEntryDecision.CancelledByTheParticipant(isoCode, cancelReadShape);
            }
        }

        return SessionEntryDecision.Unanswered;
    }


    /// <summary>
    /// Stellt eine einzelne Sprachfrage und liefert das Transkript zusammen mit der Aufnahme zurück.
    /// Frage und primäre Erkennung verwenden dieselbe Sprache. Schlüsselbegriffe werden aus deren
    /// modellierten Antwortwörtern gebildet.
    /// </summary>
    private async ValueTask<SessionEntryAnswer> AskOneLanguageAsync(
        SpokenSegment question,
        string answerIsoCode,
        TimeSpan window,
        CancellationToken cancellationToken)
    {
        // Einmal wiederholen, wenn die Frage vor dem Aufnahmefenster mit dem eigenen Wiedergabefehler
        // abbricht. Ein Fehler nach Beginn der Antwortaufnahme löst diese Wiederholung nicht aus. Der
        // Fehlertyp belegt dabei keine vollständige akustische Stille.
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await _question.AskAsync(
                        [question],
                        answerIsoCode,
                        SessionEntryQuestion.BuildAcceptanceKeyterms(answerIsoCode),
                        window,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (SessionEntryQuestionUnspokenException ex) when (attempt == 1)
            {
                _dependencies.Logger.LogWarning(
                    OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                    ex,
                    "Session-entry question for {IsoCode} was never spoken; asking it once more.",
                    answerIsoCode);
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                and not VoiceInputPausedException
                and not MicrophoneAcquisitionException)
            {
                // Der gewöhnliche Ausfall einer Frage soll die weiteren Sprachangebote nicht
                // verhindern. Den Fehler protokollieren und ein Ergebnis ohne verwendbare Antwort
                // zurückgeben.
                _dependencies.Logger.LogWarning(
                    OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable,
                    ex,
                    "Session-entry question for {IsoCode} could not be put; offering the next language.",
                    answerIsoCode);
                return SessionEntryAnswer.Silence;
            }
        }
    }

    /// <summary>
    /// Sagt die zurückgegebene neue Sprache in dieser Sprache an. Die getrennte Methode hält diese
    /// Wechselmeldung von der Rückmeldung einer beibehaltenen Sprache unterscheidbar.
    /// </summary>
    private ValueTask SpeakAcceptedAsync(string isoCode, CancellationToken cancellationToken)
    {
        var politenessLevel = _dependencies.UserLanguageState.SelectedPolitenessLevel ?? PolitenessLevel.Formal;
        var segment = SessionLanguageOfferPromptTextResolver.GetAcceptedSegment(isoCode, politenessLevel);
        return new ValueTask(_dependencies.TextToSpeechPlayer.SpeakAsync(
            segment.Text,
            segment.IsoCode,
            cancellationToken));
    }

    /// <summary>
    /// Nennt die beibehaltene Sprache in der vorgegebenen Ausgabesprache. Ohne angenommene Frage ist
    /// das die Sitzungssprache. Nach einem fehlgeschlagenen Wechsel die zuvor angebotene Zielsprache.
    /// Der eingesetzte Sprachname bleibt der Eigenname der tatsächlich beibehaltenen Sprache, damit
    /// Satzsprache und benanntes Ergebnis nicht verwechselt werden.
    /// </summary>
    private ValueTask SpeakKeptAsync(
        string keptIsoCode,
        string spokenIsoCode,
        CancellationToken cancellationToken)
    {
        var endonym = Endonyms.GetEndonym(LanguageCode.ParseIso6391(keptIsoCode));
        return new ValueTask(_dependencies.TextToSpeechPlayer.SpeakAsync(
            FormDialogFlowPromptTextResolver.GetKeptPrompt(spokenIsoCode, endonym),
            spokenIsoCode,
            cancellationToken));
    }
}
