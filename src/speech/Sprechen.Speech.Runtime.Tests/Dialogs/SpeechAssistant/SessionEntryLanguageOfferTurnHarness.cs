using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Einstiegsentscheidung mit Testdoubles für Sprachausgabeaufrufe,
/// Erkennungshinweise, Aufnahmeanzeige und versuchte Einstellungsänderungen.
/// </summary>
internal sealed class SessionEntryLanguageOfferTurnHarness
{
    private readonly Func<SessionEntryLanguageOfferTurn> _newTurn;

    private SessionEntryLanguageOfferTurnHarness(
        Func<SessionEntryLanguageOfferTurn> newTurn,
        SessionEntryLanguageOfferDependencies dependencies,
        QueuedSpeechInputClient speechClient,
        SessionEntrySecondHearingSpeechClient recognition,
        StructuredCapturingLogger<SessionEntryLanguageOfferTurn> logger,
        List<SpokenSegment> spoken,
        List<string> persisted,
        List<string> markedAsHeard,
        List<string> microphoneEvents)
    {
        _newTurn = newTurn;
        Dependencies = dependencies;
        Turn = newTurn();
        SpeechClient = speechClient;
        Recognition = recognition;
        Logged = logger;
        Spoken = spoken;
        Persisted = persisted;
        MarkedAsHeard = markedAsHeard;
        MicrophoneEvents = microphoneEvents;
    }

    /// <summary>
    /// Erzeugt für eine Folgesitzung einen neuen Ablauf mit denselben Testdoubles und demselben Markerstand.
    /// </summary>
    public SessionEntryLanguageOfferTurn NextSession() => _newTurn();

    /// <summary>
    /// Liefert je unterstützter Sprache eine skriptierte Antwort ohne erkannte Sprache.
    /// Die Anzahl folgt dem Katalog, damit zusätzliche Sprachen keine echten Wartefenster erzeugen.
    /// </summary>
    public static IReadOnlyList<string?> SilenceOnEveryRung()
        => [.. Enumerable.Repeat<string?>(null, VoiceInterfaceModelCatalog.SupportedLanguages.Count)];

    public SessionEntryLanguageOfferTurn Turn { get; }

    /// <summary>
    /// Stellt dieselben Testdoubles auch für direkte Prüfungen der beteiligten Dienste bereit.
    /// </summary>
    public SessionEntryLanguageOfferDependencies Dependencies { get; }

    public QueuedSpeechInputClient SpeechClient { get; }

    /// <summary>
    /// Skriptiert die Zweiterkennung getrennt von den Antworten je Sprachangebot.
    /// Ohne vorbereitetes Ergebnis liefert sie kein Abbruchwort.
    /// </summary>
    public SessionEntrySecondHearingSpeechClient Recognition { get; }

    /// <summary>
    /// Aufgezeichnete Protokolleinträge mit Schweregrad und Ereigniskennung für gezielte Prüfungen.
    /// </summary>
    public StructuredCapturingLogger<SessionEntryLanguageOfferTurn> Logged { get; }

    /// <summary>
    /// Sprachausgabeaufrufe in ihrer Reihenfolge mit dem jeweils angeforderten Sprachcode.
    /// </summary>
    public IReadOnlyList<SpokenSegment> Spoken { get; }

    /// <summary>
    /// An den Einstellungsablauf übergebene Sprachcodes, einschließlich fehlgeschlagener Schreibversuche.
    /// </summary>
    public IReadOnlyList<string> Persisted { get; }

    /// <summary>
    /// Versuchte Schreibzugriffe auf den Einstiegsmarker, auch wenn sie scheitern.
    /// Er wird nach angenommener Sprachantwort gesetzt, nicht nach Stille, Abbruch oder Pause.
    /// </summary>
    public IReadOnlyList<string> MarkedAsHeard { get; }

    /// <summary>
    /// Aufrufe der Aufnahmeanzeige in ihrer Reihenfolge: leaseTaken, leaseReleased und acceptedCue.
    /// Die Freigabe bezeichnet einen Aufruf am Double, keinen beobachteten Abschlusston.
    /// </summary>
    public IReadOnlyList<string> MicrophoneEvents { get; }

    public string SpokenText => string.Join(" ", Spoken.Select(segment => segment.Text));

    /// <param name="entryOfferHeardIsoCode">Gespeicherte Sprache des Einstiegsmarkers; null wählt den Erstkontakt.</param>
    /// <param name="storeIsUnreadable">Lässt das Markerlesen fehlschlagen, um das längere Ersatzfenster zu prüfen.</param>
    /// <param name="persistedIsoCodeOverride">Sprachcode in der Antwort eines erfolgreichen Speicherns.</param>
    /// <param name="savingTheLanguageFails">Lässt das Sprachspeichern nach Aufzeichnung des Versuchs fehlschlagen.</param>
    /// <param name="savingTheMarkerFails">Lässt das Markerschreiben ohne Änderung des gespeicherten Markers fehlschlagen.</param>
    /// <param name="recogniserFailsOnRung">Erkennungsfehler auf der angegebenen, ab 1 gezählten Stufe.</param>
    /// <param name="captureSessionFailure">Klassifizierter Mikrofonzugriffsfehler bei der Vorbereitung.</param>
    public static SessionEntryLanguageOfferTurnHarness Create(
        bool enabled,
        IReadOnlyList<string?>? transcripts = null,
        string? entryOfferHeardIsoCode = null,
        bool storeIsUnreadable = false,
        string? persistedIsoCodeOverride = null,
        bool savingTheLanguageFails = false,
        bool savingTheMarkerFails = false,
        int? recogniserFailsOnRung = null,
        MicrophoneAcquisitionFailureCode? captureSessionFailure = null)
    {
        // Je Eintrag werden eine Aufnahme und das Ergebnis der ersten Erkennung vorbereitet.
        // Zweiterkennungen derselben Aufnahme werden separat über Recognition skriptiert.
        var speechClient = new QueuedSpeechInputClient();
        var rung = 0;
        foreach (var transcript in transcripts ?? [])
        {
            rung++;
            // Auch für simulierte Stille wird eine Aufnahme bereitgestellt: null bedeutet
            // NoSpeechDetectedException, keinen fehlgeschlagenen Mikrofonstart. Die vorbereiteten
            // Aufnahmen vermeiden das Abwarten echter Antwortfenster.
            speechClient
                .EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]))
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (rung == recogniserFailsOnRung)
            {
                speechClient.EnqueueSpeechToTextException(new InvalidOperationException("recogniser down"));
                continue;
            }

            if (transcript is null)
            {
                speechClient.EnqueueNoSpeechDetected();
                continue;
            }

            speechClient.EnqueueTranscript(transcript);
        }

        var spoken = new List<SpokenSegment>();
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Loose);
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((text, isoCode, _) =>
            {
                spoken.Add(new SpokenSegment(text, isoCode));
                return Task.CompletedTask;
            });

        var microphoneEvents = new List<string>();
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Loose);
        indicator
            .Setup(i => i.BeginRecordingAsync())
            .Returns(() =>
            {
                microphoneEvents.Add("leaseTaken");
                var release = new Mock<IAsyncDisposable>();
                release
                    .Setup(r => r.DisposeAsync())
                    .Returns(() =>
                    {
                        microphoneEvents.Add("leaseReleased");
                        return ValueTask.CompletedTask;
                    });
                return ValueTask.FromResult(release.Object);
            });
        indicator
            .Setup(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                microphoneEvents.Add("acceptedCue");
                return ValueTask.CompletedTask;
            });

        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var persisted = new List<string>();
        var markedAsHeard = new List<string>();
        var heardIn = entryOfferHeardIsoCode;
        var settingsWorkflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        settingsWorkflow
            .Setup(w => w.SaveSelectedLanguageAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((isoCode, _) =>
            {
                persisted.Add(isoCode);
                return savingTheLanguageFails
                    ? throw new InvalidOperationException("storage unavailable")
                    : ValueTask.FromResult(
                        new UserLanguageSettings(persistedIsoCodeOverride ?? isoCode, PolitenessLevel.Formal, heardIn));
            });
        settingsWorkflow
            .Setup(w => w.ReadEntryOfferHeardIsoCodeAsync(It.IsAny<CancellationToken>()))
            .Returns(() => storeIsUnreadable
                ? throw new InvalidOperationException("storage unavailable")
                : ValueTask.FromResult(heardIn));
        settingsWorkflow
            .Setup(w => w.SaveEntryOfferHeardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>((isoCode, _) =>
            {
                // Erfolgreiches Schreiben verändert den gemeinsamen Markerstand; nur den Versuch
                // aufzuzeichnen würde die Folgesitzung weiterhin als Erstkontakt behandeln.
                markedAsHeard.Add(isoCode);
                if (savingTheMarkerFails)
                {
                    throw new InvalidOperationException("storage unavailable");
                }

                heardIn = isoCode;
                return ValueTask.CompletedTask;
            });

        var recognition = new SessionEntrySecondHearingSpeechClient(speechClient);
        var logger = new StructuredCapturingLogger<SessionEntryLanguageOfferTurn>();
        ISpeechCaptureClient captureClient = speechClient;
        if (captureSessionFailure is { } failureCode)
        {
            var unavailableCapture = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
            unavailableCapture
                .Setup(client => client.EnsureCaptureSessionReadyAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new MicrophoneAcquisitionException(failureCode));
            captureClient = unavailableCapture.Object;
        }

        var dependencies = new SessionEntryLanguageOfferDependencies(
            userLanguageState,
            settingsWorkflow.Object,
            textToSpeech.Object,
            indicator.Object,
            captureClient,
            speechClient,
            new SpeechRecognitionWorker(recognition),
            // Der echte Modelladapter liefert das produktive Abbruchvokabular statt einer separat gepflegten Testliste.
            new Sprechen.VoiceAssistant.Services.VoiceCommandCatalogCancelVocabulary(),
            logger,
            Mock.Of<IEvaluationSessionTelemetry>());

        return new SessionEntryLanguageOfferTurnHarness(
            () => new SessionEntryLanguageOfferTurn(dependencies, enabled),
            dependencies,
            speechClient,
            recognition,
            logger,
            spoken,
            persisted,
            markedAsHeard,
            microphoneEvents);
    }
}
