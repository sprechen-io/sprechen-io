using Sprechen.Speech.LanguageContracts;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die begrenzte Wiederholung eines Sprachangebots bei Fehlern der Frageausgabe.
/// Ein Ausgabeversuch darf wiederholt werden; ein anschließender Aufnahmefehler löst diese Wiederholung nicht aus.
/// Auch nach zwei gescheiterten Ausgabeversuchen bleiben spätere Sprachangebote erreichbar.
/// </summary>
public sealed class SessionEntryUnspokenRungTests
{
    [Fact]
    public async Task EineFrageDieNieGesprochenWurde_WirdEinZweitesMalGestellt()
    {
        // Die italienische Frageausgabe schlägt einmal fehl; im zweiten Versuch wird der Sprachname als Antwort geliefert.
        var harness = SessionEntryUnspokenRungHarness.Create(
            failSpeakingOnce: "it",
            transcripts: new Dictionary<string, string?> { ["it"] = "italiano" });

        var result = await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), result);
        Assert.Equal(2, harness.QuestionAttemptsFor("it"));
    }

    [Fact]
    public async Task EineFrageDieGesprochenWurde_WirdNichtWiederholt()
    {
        // Ein Aufnahmefehler nach erfolgreicher Frageausgabe darf die Frage nicht wiederholen.
        var harness = SessionEntryUnspokenRungHarness.Create(
            failCaptureOnce: "it",
            transcripts: new Dictionary<string, string?> { ["it"] = "italiano" });

        await harness.Turn.DecideLanguageAsync("de", Deadline());

        Assert.Equal(1, harness.QuestionAttemptsFor("it"));
    }

    [Fact]
    public async Task EineFrageDieZweimalNichtGesprochenWerdenKann_BeendetDieLeiterNicht()
    {
        // Auch nach zwei Fehlern bei der englischen Frageausgabe muss Italienisch erreichbar bleiben.
        var harness = SessionEntryUnspokenRungHarness.Create(
            failSpeakingAlways: "en",
            transcripts: new Dictionary<string, string?> { ["it"] = "italiano" });

        Assert.Equal(SessionEntryOutcome.ContinueIn("it"), await harness.Turn.DecideLanguageAsync("de", Deadline()));
        Assert.Equal(2, harness.QuestionAttemptsFor("en"));
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
