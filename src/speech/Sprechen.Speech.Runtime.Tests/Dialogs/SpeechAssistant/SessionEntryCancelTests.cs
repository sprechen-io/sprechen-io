using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Logging;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Abbruch beim Sitzungseinstieg, einschließlich einer zweiten Erkennung
/// in der Sitzungssprache während fremdsprachiger Angebote.
/// </summary>
public sealed class SessionEntryCancelTests
{
    // Der strikte Host erlaubt keinen Start. Der Einstellungszugang erlaubt initiales
    // Speichern und Markerlesen, aber keinen Markerschreibzugriff. Die gespeicherten
    // Werte unterscheiden die Initialisierung von zusätzlichen Einstiegsänderungen.
    [Theory]
    [InlineData("de", "abbrechen")]
    [InlineData("en", "cancel")]
    public async Task StartAsync_WhenTheKeepRungHearsTheSessionLanguagesCancelWord_StartsNothingAndPersistsNothing(
        string isoCode,
        string cancelWord)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode(isoCode);
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var persisted = new List<string>();
        var settingsWorkflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        settingsWorkflow
            .Setup(w => w.ReadEntryOfferHeardIsoCodeAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
        settingsWorkflow
            .Setup(w => w.SaveSelectedLanguageAsync(isoCode, It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((saved, _) =>
            {
                persisted.Add(saved);
                return ValueTask.FromResult(new UserLanguageSettings(saved, PolitenessLevel.Formal));
            });

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);

        var spoken = new List<string>();
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Loose);
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((text, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });

        var queued = new QueuedSpeechInputClient();
        await queued.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        queued.EnqueueTranscript(cancelWord);

        // Vorbereitete Stille für weitere Angebote lässt einen verschluckten Abbruch sichtbar werden, ohne echte Antwortfenster abzuwarten.
        foreach (var _ in SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung().Skip(1))
        {
            await queued.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
            queued.EnqueueNoSpeechDetected();
        }

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            queued,
            queued,
            queued,
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);
        await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        // Nur die initiale Sprachspeicherung bleibt erlaubt. Die aufgezeichneten Ansagen
        // müssen Einstiegsfrage und Abbruchbestätigung enthalten, keine anschließende Einrichtung.
        Assert.Equal([isoCode], persisted);
        Assert.Equal(2, spoken.Count);
        Assert.Equal(
            SessionLanguageOfferPromptTextResolver.GetCancelledSegment(isoCode, PolitenessLevel.Formal).Text,
            spoken[1]);
    }

    // Die modellierten Abbruchwörter der Sitzungssprache beenden den Einstieg ohne zusätzliche Sprachwahl oder Einstiegsmarker.
    [Theory]
    [InlineData("de", "abbrechen")]
    [InlineData("de", "beenden")]
    [InlineData("de", "Abbrechen.")]
    public async Task DecideLanguageAsync_WhenTheKeepRungHearsACancelWord_ReportsCancelledAndStoresNothing(
        string isoCode,
        string cancelTranscript)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: [cancelTranscript]);

        var outcome = await harness.Turn.DecideLanguageAsync(isoCode, Deadline());

        Assert.Equal(SessionEntryOutcome.CancelledByTheParticipant(isoCode), outcome);
        Assert.True(outcome.Cancelled);
        Assert.Empty(harness.Persisted);
        Assert.Empty(harness.MarkedAsHeard);
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("quit")]
    public async Task DecideLanguageAsync_WhenAnEnglishSessionHearsItsCancelWord_ReportsCancelled(
        string cancelTranscript)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: [cancelTranscript]);

        var outcome = await harness.Turn.DecideLanguageAsync("en", Deadline());

        Assert.Equal(SessionEntryOutcome.CancelledByTheParticipant("en"), outcome);
        Assert.Empty(harness.Persisted);
        Assert.Empty(harness.MarkedAsHeard);
    }

    // Dieselbe Instanz muss nach Abbruch erneut die Einstiegsfrage stellen. Erst die
    // anschließend bestätigte Antwort darf den Marker schreiben.
    [Fact]
    public async Task DecideLanguageAsync_AfterASpokenCancel_TheNextStartAsksTheLadderAgain()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["abbrechen", "Deutsch"]);

        var cancelled = await harness.Turn.DecideLanguageAsync("de", Deadline());
        Assert.True(cancelled.Cancelled);
        Assert.Empty(harness.MarkedAsHeard);

        var second = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), second);
        Assert.Equal(["de"], harness.MarkedAsHeard);
    }

    // Ein Abbruchwort innerhalb einer längeren Äußerung bleibt eine Nichtantwort und wird nicht als einzelnes Kommando herausgelöst.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheCancelWordIsEmbeddedInASentence_DoesNotCancel()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["ich moechte abbrechen", null, null, null, null]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.False(outcome.Cancelled);
        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), outcome);
    }

    // Ablehnung und Nichtverstehen brechen nicht ab; die übrigen vorbereiteten stillen Sprachangebote folgen.
    [Theory]
    [InlineData("nein")]
    [InlineData("wie bitte")]
    public async Task DecideLanguageAsync_WhenTheAnswerIsNoOrNonComprehension_DoesNotCancel(string answer)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [answer, null, null, null, null]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.False(outcome.Cancelled);
        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), outcome);
    }

    // Auch eine fehlgeschlagene Abbruchbestätigung darf keinen Start oder Marker auslösen.
    // Ihr tatsächlicher Ausgabeversuch wird geprüft, damit ein ganz unterlassener Aufruf nicht genügt.
    [Fact]
    public async Task StartAsync_WhenTheAcknowledgmentCannotBeSpoken_TheCancelStillStandsAndNothingStarts()
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var settingsWorkflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        settingsWorkflow
            .Setup(w => w.ReadEntryOfferHeardIsoCodeAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
        settingsWorkflow
            .Setup(w => w.SaveSelectedLanguageAsync("de", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings("de", PolitenessLevel.Formal)));

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);

        var acknowledgmentText = SessionLanguageOfferPromptTextResolver
            .GetCancelledSegment("de", PolitenessLevel.Formal)
            .Text;
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Loose);
        textToSpeech
            .Setup(p => p.SpeakAsync(acknowledgmentText, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("voice unavailable"));
        textToSpeech
            .Setup(p => p.SpeakAsync(It.Is<string>(text => text != acknowledgmentText), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var queued = new QueuedSpeechInputClient();
        await queued.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));
        queued.EnqueueTranscript("abbrechen");

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            queued,
            queued,
            queued,
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        await using var service = new SpeechAssistantService(context, () => host.Object);
        var outcome = await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(SpeechAssistantStartOutcome.CancelledAtEntry, outcome);
        textToSpeech.Verify(
            p => p.SpeakAsync(acknowledgmentText, It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Nach unbekanntem Ergebnis einer fremdsprachigen Stufe wird dieselbe Aufnahme
    // in der Sitzungssprache auf Abbruch geprüft. Beide Ergebnisse sind vorgegeben;
    // geprüft werden Weiterleitung, Abbruchwort-Hinweise und protokollierte Herkunft, keine Erkennungsqualität.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheFirstTranscriptHidesTheCancelWord_TheSecondRecognitionCancels()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["nein", "kartoffelsalat"]);
        harness.Recognition.EnqueueSecondHearing("abbrechen");

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.CancelledByTheParticipant("de"), outcome);
        Assert.Empty(harness.Persisted);
        Assert.Empty(harness.MarkedAsHeard);

        Assert.Equal(["de"], harness.Recognition.SecondHearingLanguages);
        var secondHearingKeyterms = Assert.Single(harness.Recognition.SecondHearingKeyterms);
        Assert.Equal(["abbrechen", "beenden"], secondHearingKeyterms!.Terms);

        var cancelledEntry = Assert.Single(
            harness.Logged.Entries,
            entry => entry.EventId.Id == OrchestrationLogEvents.SessionEntryCancelled.Id);
        Assert.Equal("en", cancelledEntry.Values["RungIsoCode"]);
        Assert.Equal("foreignRungSecondHearing", cancelledEntry.Values["ReadShape"]);
    }

    // Ein bereits im ersten Transkript enthaltenes vollständiges Abbruchwort erspart den zweiten Erkennungsdurchlauf.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheCancelWordArrivesIntactOnAForeignRung_NoSecondHearingIsNeeded()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["nein", "abbrechen"]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.CancelledByTheParticipant("de"), outcome);
        Assert.Empty(harness.Recognition.SecondHearingLanguages);
    }

    // Stille darf keinen zusätzlichen Erkennungsdurchlauf für Abbruchwörter anfordern.
    [Fact]
    public async Task DecideLanguageAsync_WhenEveryRungIsSilent_NoSecondHearingRuns()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), outcome);
        Assert.Empty(harness.Recognition.SecondHearingLanguages);
    }

    // Eine angenommene Sprachwahl beendet die Entscheidung, bevor ein weiterer Abbruchdurchlauf nötig wäre.
    [Fact]
    public async Task DecideLanguageAsync_WhenAForeignRungIsAccepted_NoSecondHearingRuns()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["nein", "yes"]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("en"), outcome);
        Assert.Empty(harness.Recognition.SecondHearingLanguages);
    }

    // Die erste Frage verwendet schon die Sitzungssprache; ihr Transkript wird daher ohne Zweiterkennung auf Abbruch geprüft.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheKeepRungCancels_NoSecondHearingRuns()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["abbrechen"]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.True(outcome.Cancelled);
        Assert.Empty(harness.Recognition.SecondHearingLanguages);
    }

    // Eine nicht ausführbare Zweiterkennung ist technische Nichtverfügbarkeit, keine
    // Teilnehmerstille. Wiederholte QueueFullException bleiben auf diesen Pfad beschränkt;
    // der Ablauf muss die Warnung melden und weitere Sprachangebote zulassen.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheCancelReadCannotRun_LogsUnavailabilityAndAdvances()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["nein", "kartoffelsalat", null, null, null]);
        for (var i = 0; i < 8; i++)
        {
            harness.Recognition.EnqueueSecondHearingException(new SpeechRecognitionQueueFullException(1));
        }

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), outcome);
        Assert.False(outcome.Cancelled);
        Assert.Contains(
            harness.Logged.Entries,
            entry => entry.Level == LogLevel.Warning
                && entry.EventId.Id == OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable.Id);

        Assert.Equal(5, harness.SpeechClient.RecordCallCount);
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetKeptPrompt("de", "Deutsch"),
            harness.Spoken[^1].Text);
    }

    // Ein wirksamer Abbruch erhält ein Information-Ereignis statt einer Warnung über
    // fehlende Sprachentscheidung. Stufe und Erkennungsweg machen seine Herkunft nachvollziehbar.
    [Fact]
    public async Task DecideLanguageAsync_WhenTheCancelIsHonored_LogsInformationUnderItsOwnEvent()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["abbrechen"]);

        var outcome = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.True(outcome.Cancelled);
        var cancelledEntry = Assert.Single(
            harness.Logged.Entries,
            entry => entry.EventId.Id == OrchestrationLogEvents.SessionEntryCancelled.Id);
        Assert.Equal(LogLevel.Information, cancelledEntry.Level);
        Assert.Equal("de", cancelledEntry.Values["RungIsoCode"]);
        Assert.Equal("keepRungTranscript", cancelledEntry.Values["ReadShape"]);
        Assert.DoesNotContain(
            harness.Logged.Entries,
            entry => entry.EventId.Id == OrchestrationLogEvents.SessionEntryLanguageDecisionUnavailable.Id);
    }

    // Prüft die kanonischen Abbruchwörter der fünf aufgeführten Sprachen in beiden Höflichkeitsformen.
    [Theory]
    [InlineData("de", "abbrechen", "beenden")]
    [InlineData("en", "cancel", "quit")]
    [InlineData("fr", "annuler", "arreter")]
    [InlineData("es", "cancelar", "salir")]
    [InlineData("it", "annulla", "esci")]
    public void TheEntryReadsTheCanonicalCancelVocabulary_InEveryLanguageAndPolitenessForm(
        string isoCode,
        string first,
        string second)
    {
        var vocabulary = new Sprechen.VoiceAssistant.Services.VoiceCommandCatalogCancelVocabulary();

        Assert.Equal([first, second], vocabulary.GetCancelUtterances(isoCode, PolitenessLevel.Formal));
        Assert.Equal([first, second], vocabulary.GetCancelUtterances(isoCode, PolitenessLevel.Informal));
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
