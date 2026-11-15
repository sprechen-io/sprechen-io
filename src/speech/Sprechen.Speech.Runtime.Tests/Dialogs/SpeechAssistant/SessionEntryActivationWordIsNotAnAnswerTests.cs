using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass eine Aktivierungsphrase allein keine Einstiegsfrage zur Sprache beantwortet.
/// Die allgemeine Ja/Nein-Klassifikation akzeptiert diese Phrase; der Einstieg muss
/// sie deshalb entfernen und eine eventuell folgende Sprachantwort erhalten.
/// </summary>
public sealed class SessionEntryActivationWordIsNotAnAnswerTests
{
    [Fact]
    public void TheActivationWord_IsAYesToThePlainClassifier()
    {
        // Die allgemeine Akzeptanz der Phrase wird eigens geprüft, damit Lexikonänderungen sichtbar werden.
        var activationForm = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de")[0];

        Assert.True(
            YesNo.TryParseYesNo(activationForm, VoiceInterfaceModelCatalog.SupportedLanguages, out var isYes));
        Assert.True(isYes, $"'{activationForm}' is expected to read as yes to the plain classifier");
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    public async Task SayingTheActivationWord_DoesNotAcceptTheLanguage(string isoCode)
    {
        var activationForm = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode)[0];
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [activationForm, null, null, null, null]);

        var continued = await harness.Turn
            .DecideLanguageAsync(isoCode, Deadline())
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(SessionEntryOutcome.ContinueIn(isoCode), continued);

        // Die Aktivierungsphrase enthält keine Sprachpräferenz; weitere Sprachangebote müssen folgen, ohne eine Auswahl zu speichern.
        Assert.Equal(VoiceInterfaceModelCatalog.SupportedLanguages.Count, harness.SpeechClient.RecordCallCount);

        Assert.Empty(harness.Persisted);
    }

    /// <summary>
    /// Auch unterschiedliche Satzzeichen und Großschreibung machen eine reine Aktivierungsphrase nicht zur Antwort.
    /// </summary>
    [Theory]
    [InlineData("Okay, Alex.")]
    [InlineData("Okay alex")]
    [InlineData("okay alex.")]
    [InlineData("OKAY, ALEX!")]
    public async Task TheShapesTheRecogniserActuallyReturns_AreAllRefused(string transcript)
    {
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true,
            transcripts: [transcript, null, null, null, null]);

        var continued = await harness.Turn
            .DecideLanguageAsync("de", Deadline())
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), continued);
        Assert.Equal(VoiceInterfaceModelCatalog.SupportedLanguages.Count, harness.SpeechClient.RecordCallCount);
        Assert.Empty(harness.Persisted);
    }

    [Fact]
    public async Task TheActivationWordFollowedByAnAnswer_KeepsTheAnswer()
    {
        // Das Entfernen des Ankers muss die folgende echte Antwort erhalten, etwa Deutsch in Okay, Alex. Deutsch.
        var harness = SessionEntryLanguageOfferTurnHarness.Create(
            enabled: true, transcripts: ["Okay, Alex. Deutsch"]);

        var continued = await harness.Turn
            .DecideLanguageAsync("de", Deadline())
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(30));

        Assert.Equal(SessionEntryOutcome.ContinueIn("de"), continued);
        Assert.Equal(1, harness.SpeechClient.RecordCallCount);
    }

    private static CancellationToken Deadline()
        => new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
}
