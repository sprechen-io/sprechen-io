using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den Sitzungsstart mit Testdoubles aus und protokolliert Aufnahme- und
/// Sprachausgabeaufrufe in derselben Zeitfolge. Sie ermöglicht Reihenfolgeprüfungen,
/// misst jedoch weder tatsächliche Wiedergabe noch physisches Mikrofonende.
/// </summary>
internal sealed class SessionEntryStartHarness
{
    private readonly List<(string Kind, string Detail, TimeSpan At)> _events = [];

    private SessionEntryStartHarness(List<(string, string, TimeSpan)> events)
    {
        _events = events;
    }

    /// <summary>
    /// Aufnahme- und Sprachausgabeaufrufe in aufgezeichneter Reihenfolge.
    /// </summary>
    public IReadOnlyList<(string Kind, string Detail, TimeSpan At)> Events => _events;

    public IReadOnlyList<string> Spoken =>
        [.. _events.Where(entry => entry.Kind == "speak").Select(entry => entry.Detail)];

    /// <summary>
    /// Führt den Erstkontakt mit vorgegebener Annahme der aufgelösten Sitzungssprache aus.
    /// </summary>
    public static Task<SessionEntryStartHarness> StartWithAcceptedEntryAsync(string isoCode)
        => StartCoreAsync(isoCode, studyCatalog: null);

    /// <summary>
    /// Derselbe angenommene Erstkontakt mit gebundenem Studientoken und markierter Studienanwendung.
    /// </summary>
    public static Task<SessionEntryStartHarness> StartWithAcceptedStudyEntryAsync(string isoCode)
        => StartCoreAsync(isoCode, studyCatalog: CreateStudyCatalog());

    private static async Task<SessionEntryStartHarness> StartCoreAsync(
        string isoCode,
        FormsCatalog? studyCatalog)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode(isoCode);
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);

        var settingsWorkflow = new Mock<IUserLanguageSettingsWorkflow>(MockBehavior.Strict);
        settingsWorkflow
            .Setup(w => w.SaveSelectedLanguageAsync(isoCode, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(new UserLanguageSettings(isoCode, PolitenessLevel.Formal)));
        settingsWorkflow
            .Setup(w => w.ReadEntryOfferHeardIsoCodeAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<string?>(null));
        settingsWorkflow
            .Setup(w => w.SaveEntryOfferHeardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var host = new Mock<IDialogHost<SpeechAssistantEvent>>(MockBehavior.Strict);
        host.Setup(h => h.StartAsync(It.IsAny<IDialogFlow<SpeechAssistantEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        if (studyCatalog is not null)
        {
            host.Setup(h => h.DispatchToActiveAsync(
                    It.IsAny<SpeechAssistantStudySessionBoundEvent>(),
                    DispatchMode.Interrupt,
                    It.IsAny<CancellationToken>()))
                .Returns(ValueTask.CompletedTask);
        }

        host.Setup(h => h.DispatchToActiveAsync(
                It.IsAny<SpeechAssistantLanguageSelectedEvent>(),
                DispatchMode.Interrupt,
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var clock = Stopwatch.StartNew();
        var events = new List<(string, string, TimeSpan)>();

        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(p => p.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((text, _, _) =>
            {
                lock (events)
                {
                    events.Add(("speak", text, clock.Elapsed));
                }

                return Task.CompletedTask;
            });

        var queued = new QueuedSpeechInputClient();
        await queued.EnqueueRecordingAsync(new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1, 2 }]));

        // Den Sprachnamen aus dem Katalog lesen, damit die Testantwort dem tatsächlichen Angebot folgt.
        queued.EnqueueTranscript(
            VoiceInterfaceLanguageCatalog.CreateDefault().GetEndonym(LanguageCode.ParseIso6391(isoCode)));
        var speechClient = new SessionEntryLoggingSpeechInputClient(queued, events, clock);

        IStudyTelemetryContext? studyTelemetryContext = null;
        if (studyCatalog is not null)
        {
            var boundStudy = new StudyTelemetryContext();
            boundStudy.SetStudyContext("x9jdln");
            studyTelemetryContext = boundStudy;
        }

        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            settingsWorkflow.Object,
            speechClient,
            speechClient,
            speechClient,
            textToSpeech.Object,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance,
            formsCatalogDataClient: studyCatalog is null
                ? null
                : new FixedFormsCatalogDataClient(studyCatalog),
            studyTelemetryContext: studyTelemetryContext);

        await using var service = new SpeechAssistantService(context, () => host.Object);
        await service.StartAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30));

        return new SessionEntryStartHarness(events);
    }

    private static FormsCatalog CreateStudyCatalog()
    {
        return new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "study.umfrage",
                    DisplayName = new LocalizedText
                    {
                        ["de"] = "Umfrage",
                        ["en"] = "Survey",
                        ["fr"] = "Enquête",
                        ["es"] = "Encuesta",
                        ["it"] = "Sondaggio"
                    },
                    Description = new LocalizedText { ["de"] = "Studie" },
                    SupportedLanguages = ["de", "en", "fr", "es", "it"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("hintergrund-und-setup"),
                            DisplayName = new LocalizedText { ["de"] = "Quizabend" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 1
                        }
                    ]
                }
            ]
        };
    }
}
