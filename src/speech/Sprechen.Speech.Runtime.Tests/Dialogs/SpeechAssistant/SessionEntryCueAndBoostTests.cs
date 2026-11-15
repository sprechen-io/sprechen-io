using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Freigabe der Aufnahmeanzeige, den Annahmehinweis und
/// frageabhängige Erkennungshinweise beim Sitzungseinstieg.
/// </summary>
public sealed class SessionEntryCueAndBoostTests
{
    [Fact]
    public async Task EveryWindow_TakesAndReleasesTheListeningLeaseExactlyOnce()
    {
        // Die Nutzung der Aufnahmeanzeige muss vor dem nächsten Fenster freigegeben sein;
        // diese Buchführung wird unabhängig von einer akustischen Ausgabe geprüft.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        var starts = harness.MicrophoneEvents.Count(signal => signal == "leaseTaken");
        var stops = harness.MicrophoneEvents.Count(signal => signal == "leaseReleased");

        Assert.Equal(VoiceInterfaceModelCatalog.SupportedLanguages.Count, starts);
        Assert.Equal(starts, stops);
    }

    [Fact]
    public async Task NoSecondWindow_OpensBeforeThePreviousOneIsClosed()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        var open = 0;
        foreach (var signal in harness.MicrophoneEvents)
        {
            if (signal == "leaseTaken")
            {
                open++;
            }
            else if (signal == "leaseReleased")
            {
                open--;
            }

            Assert.InRange(open, 0, 1);
        }

        Assert.Equal(0, open);
    }

    [Fact]
    public async Task AnAnswerThatWasRecorded_IsAcknowledgedBeforeTheRecognitionWait()
    {
        // Der Annahmehinweis folgt nach Freigabe der Aufnahmeanzeige und vor dem
        // Erkennungsergebnis. Er bestätigt erfasstes Audio, noch keinen verstandenen Inhalt.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(enabled: true, transcripts: ["Deutsch"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(["leaseTaken", "leaseReleased", "acceptedCue"], harness.MicrophoneEvents);
    }

    [Fact]
    public async Task TheCueSaysTheAudioWasTaken_EvenWhenNoWordsAreFoundInIt()
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: SessionEntryLanguageOfferTurnHarness.SilenceOnEveryRung());

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(["leaseTaken", "leaseReleased", "acceptedCue"], harness.MicrophoneEvents.Take(3));
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void EachQuestion_BoostsThatLanguagesAnswerWords_AndNothingElse(string isoCode)
    {
        // Je Sprache werden Eigenname sowie Ja und Nein als Erkennungshinweise geprüft;
        // eine ausschließlich deutsche Prüfung würde fehlende Begriffe anderer Sprachen nicht erkennen.
        var keyterms = SessionEntryQuestion.BuildAcceptanceKeyterms(isoCode);

        Assert.Equal(KeytermComposition.WithoutServerBase, keyterms.Composition);
        IReadOnlyList<string> expected =
        [
            YesNo.GetYesWord(isoCode),
            YesNo.GetNoWord(isoCode),
            VoiceInterfaceLanguageCatalog.CreateDefault().GetEndonym(LanguageCode.ParseIso6391(isoCode)),
        ];

        Assert.Equal(
            expected.Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.Ordinal),
            keyterms.Terms.Order(StringComparer.Ordinal));

        Assert.True(keyterms.Terms.Count <= 3, $"Boost-Satz auf {keyterms.Terms.Count} Begriffe gewachsen");
    }

    [Fact]
    public async Task TheBoostFollowsTheQuestion_NotTheSessionLanguage()
    {
        // Die italienische Frage benötigt italienische Antwortbegriffe, auch wenn die Sitzung auf Deutsch begann.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: [null, null, null, null, "italiano"]);

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        var last = harness.SpeechClient.ObservedKeyterms[^1];
        Assert.NotNull(last);
        Assert.Contains(YesNo.GetYesWord("it"), last.Terms);
        Assert.DoesNotContain(YesNo.GetYesWord("de"), last.Terms);
        Assert.Contains(
            VoiceInterfaceLanguageCatalog.CreateDefault().GetEndonym(LanguageCode.ParseIso6391("it")),
            last.Terms);
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
