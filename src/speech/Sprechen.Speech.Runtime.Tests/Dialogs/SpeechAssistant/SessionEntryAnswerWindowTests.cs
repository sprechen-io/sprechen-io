using Sprechen.Mda.Contracts.V1.Preflight;
using Sprechen.Mda.Contracts.V1.SpeechCapture;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft modellierte Mindestzeiten für Antworten, das kürzere Wiederholungsfenster
/// und die Weitergabe der Fenster samt zusätzlicher Zeit zum Abklingen der Wiedergabe.
/// </summary>
public sealed class SessionEntryAnswerWindowTests
{
    [Fact]
    public void TheEntryWindows_AreModeled_NotHardCoded()
    {
        // Der erforderliche Modellblock muss vorhanden sein; ein lokaler Ersatzwert würde die Modellvorgabe umgehen.
        Assert.NotNull(SpeechCaptureProfileCatalogReader.GetSessionEntryProfile());
    }

    [Theory]
    // Der erste Kontakt mit einer angebotenen Sprache erhält ein längeres Antwortfenster.
    [InlineData("firstContact", 6000)]
    // Das Wiederholungsfenster gilt, wenn das Einstiegsangebot in dieser Sprache bereits beantwortet wurde.
    [InlineData("repeat", 3000)]
    // Weitere Sprachangebote behalten mindestens 4000 ms Antwortzeit.
    [InlineData("languageQuestion", 4000)]
    public void EachWindow_IsAtLeastWhatItWasMeasuredTo(string window, int floorMs)
    {
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);

        var actual = window switch
        {
            "firstContact" => profile.FirstContactInitialSilenceTimeoutMs,
            "repeat" => profile.RepeatInitialSilenceTimeoutMs,
            "languageQuestion" => profile.LanguageQuestionInitialSilenceTimeoutMs,
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, "Unknown entry window."),
        };

        Assert.True(
            actual >= floorMs,
            $"sessionEntry.{window} was turned down to {actual}ms; it must stay at or above {floorMs}ms");
    }

    [Fact]
    public void TheRepeatWindow_StaysShorterThanTheFirstContactWindow()
    {
        // Eine bekannte Einstiegsfrage erhält ein kürzeres Stillefenster. Getrennte Bereichsprüfungen würden gleich lange Fenster nicht erkennen.
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);

        Assert.True(
            profile.RepeatInitialSilenceTimeoutMs < profile.FirstContactInitialSilenceTimeoutMs,
            "the every-session window must stay shorter than the once-only one");
    }

    [Fact]
    public void APreflight_RefusesAWindowTurnedBelowTheFloor()
    {
        // Die Modellprüfung muss eine zu kurze Vorgabe wie 200 ms für den Erstkontakt ablehnen.
        var catalog = CatalogWith(new SpeechCaptureSessionEntryProfile
        {
            FirstContactInitialSilenceTimeoutMs = 200,
            RepeatInitialSilenceTimeoutMs = 3000,
            LanguageQuestionInitialSilenceTimeoutMs = 4000,
            PlaybackSettleAllowanceMs = 650,
        });

        var result = SpeechCaptureProfileCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error => error.Path == "$.sessionEntry.firstContactInitialSilenceTimeoutMs");
    }

    [Fact]
    public void APreflight_RefusesARepeatWindowThatIsNotShorter()
    {
        var catalog = CatalogWith(new SpeechCaptureSessionEntryProfile
        {
            FirstContactInitialSilenceTimeoutMs = 6000,
            RepeatInitialSilenceTimeoutMs = 6000,
            LanguageQuestionInitialSilenceTimeoutMs = 4000,
            PlaybackSettleAllowanceMs = 650,
        });

        var result = SpeechCaptureProfileCatalogPreflight.Validate(catalog);

        Assert.False(result.Ok);
        Assert.Contains(result.Errors, error => error.Path == "$.sessionEntry.repeatInitialSilenceTimeoutMs");
    }

    [Fact]
    public async Task EveryWindowOfTheWholeLadder_IsTheOneItsQuestionAsksFor()
    {
        // Die vollständige Folge schützt vor vertauschter Zuordnung von Erstkontakt- und Folgefenstern.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung(),
            entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);

        Assert.Equal(
            [
                (TimeSpan?)Requested(profile.FirstContactInitialSilenceTimeoutMs),
                .. Enumerable
                    .Repeat(
                        (TimeSpan?)Requested(profile.LanguageQuestionInitialSilenceTimeoutMs),
                        VoiceInterfaceModelCatalog.SupportedLanguages.Count - 1),
            ],
            harness.SpeechClient.AnswerWindows);
    }

    [Fact]
    public async Task NoEntryPath_OpensMoreWindowsThanThereAreLanguages()
    {
        // Auch bei ausschließlich unbekannten Antworten bleibt der Einstieg auf einen Versuch je unterstützter Sprache begrenzt.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: ["Kartoffelsalat", "Kartoffelsalat", "Kartoffelsalat", "Kartoffelsalat", "Kartoffelsalat"],
            entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(
            VoiceInterfaceModelCatalog.SupportedLanguages.Count,
            harness.SpeechClient.RecordCallCount);
    }

    [Fact]
    public async Task EveryWindow_IsAShortConfirmation()
    {
        // ShortConfirmation berücksichtigt kurze Antworten nach einer Ansage.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung(),
            entryOfferHeardIsoCode: null);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.All(harness.SpeechClient.RecordModes, mode => Assert.Equal(SpeechCaptureMode.ShortConfirmation, mode));
    }

    [Fact]
    public void TheSettleAllowance_IsModeled_AndCoversTheMeasuredLoss()
    {
        // Mindestens 620 ms zusätzliche Abklingzeit kommen zum modellierten Antwortfenster hinzu; der Test prüft die angeforderte Dauer, keine reale Audiolatenz.
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);

        Assert.True(
            profile.PlaybackSettleAllowanceMs >= 620,
            $"the settle allowance is {profile.PlaybackSettleAllowanceMs}ms; the measured settle wait "
            + "is 610-620ms, so anything below it hands the participant less time than was decided");
    }

    /// <summary>
    /// Angeforderte Aufnahmedauer aus gewählter Antwortzeit und modellierter Zusatzzeit.
    /// </summary>
    private static TimeSpan Requested(int decidedMs)
    {
        var profile = SpeechCaptureProfileCatalogReader.GetSessionEntryProfile();
        Assert.NotNull(profile);
        return TimeSpan.FromMilliseconds(decidedMs + profile.PlaybackSettleAllowanceMs);
    }

    private static SpeechCaptureProfileCatalog CatalogWith(SpeechCaptureSessionEntryProfile sessionEntry)
        => new()
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            CaptureProfilesByMode =
            [
                new SpeechCaptureProfile
                {
                    Mode = nameof(SpeechCaptureMode.ShortConfirmation),
                    SilenceDebounceMs = 500,
                    MaxUtteranceDurationMs = 5000,
                },
            ],
            SessionEntry = sessionEntry,
        };

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
