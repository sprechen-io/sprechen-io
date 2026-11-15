using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.App.Runtime.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.TestDoubles;
using Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class WakeWordSetupKeytermTests
{
    [Fact]
    public async Task YesNoTurn_HearsYesAndNo_AndNothingElse()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("ja");
        var interactor = CreateInteractor(speechClient);

        var answer = await interactor.PromptYesNoAsync("Ist das richtig?", "de", 1, CancellationToken.None);

        Assert.True(answer);
        var call = Assert.Single(speechClient.SpeechToTextCalls);
        Assert.NotNull(call.Keyterms);
        Assert.Equal(KeytermComposition.WithoutServerBase, call.Keyterms!.Composition);
        Assert.Contains("ja", call.Keyterms.Terms);
        Assert.Contains("nein", call.Keyterms.Terms);

        // Die Ja/Nein-Frage soll keinen allgemeinen Befehlswortschatz erhalten.
        Assert.DoesNotContain("weiter", call.Keyterms.Terms);
        Assert.DoesNotContain("abbrechen", call.Keyterms.Terms);
    }

    [Fact]
    public async Task DictationTurn_AsksForNoBiasAtAll()
    {
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(Utterance()));
        speechClient.EnqueueTranscript("hallo computer");
        var interactor = CreateInteractor(speechClient);

        await interactor.PromptSpeechToTextAsync("Sagen Sie Ihr Weckwort", "de", CancellationToken.None);

        // Bei frei gewählten Aktivierungswörtern wird kein Wortschatz vorgegeben.
        // WithoutServerBase schließt auch die serverseitigen Grundbegriffe aus;
        // eine leere Begriffsliste allein legt diese Zusammensetzung nicht fest.
        var call = Assert.Single(speechClient.SpeechToTextCalls);
        Assert.NotNull(call.Keyterms);
        Assert.Empty(call.Keyterms!.Terms);
        Assert.Equal(KeytermComposition.WithoutServerBase, call.Keyterms.Composition);
    }

    private static WakeWordSetupSpeechInteractor CreateInteractor(ScriptedSpeechInputClient speechClient)
        => new(
            speechClient,
            new SpeechRecognitionWorker(speechClient),
            new RecordingTextToSpeechPlayer(),
            new NoOpEarconPlayer(),
            RecordingIndicator(),
            NullLogger<WakeWordSetupSpeechInteractor>.Instance);

    private static ISpeechCaptureRecordingIndicator RecordingIndicator()
    {
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>();
        indicator.Setup(i => i.BeginRecordingAsync())
            .Returns(new ValueTask<IAsyncDisposable>(Mock.Of<IAsyncDisposable>()));
        indicator.Setup(i => i.EnsureStoppedAsync()).Returns(ValueTask.CompletedTask);
        indicator.Setup(i => i.PlayInputAcceptedCueAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);
        return indicator.Object;
    }

    private static CapturedSpeechAudio Utterance()
        => new(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
}
