using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SessionEntryNoSpeechTelemetryTests
{
    [Fact]
    public async Task CaptureSilence_RecordsNoSpeechDetected()
    {
        var telemetry = new Mock<IEvaluationSessionTelemetry>(MockBehavior.Strict);
        telemetry.Setup(t => t.TrackNoSpeechDetected());
        var capture = new Mock<IModeAwareSpeechCaptureClient>(MockBehavior.Strict);
        capture
            .Setup(client => client.RecordAsync(
                SpeechCaptureMode.ShortConfirmation,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());
        var tts = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        tts.Setup(player => player.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        indicator.Setup(i => i.BeginRecordingAsync())
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());
        var languageState = new UserLanguageState();
        await using var recognition = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());
        var question = new SessionEntryQuestion(
            new SessionEntryLanguageOfferDependencies(
                languageState,
                Mock.Of<IUserLanguageSettingsWorkflow>(),
                tts.Object,
                indicator.Object,
                Mock.Of<ISpeechCaptureClient>(),
                capture.Object,
                recognition,
                Mock.Of<ISessionEntryCancelVocabulary>(),
                NullLogger.Instance,
                telemetry.Object));

        var answer = await question.AskAsync(
            [new SpokenSegment("Welche Sprache?", "de")],
            "de",
            SessionEntryQuestion.BuildAcceptanceKeyterms("de"),
            TimeSpan.FromSeconds(3),
            CancellationToken.None);

        Assert.Equal(SessionEntryAnswer.Silence, answer);
        telemetry.Verify(t => t.TrackNoSpeechDetected(), Times.Once);
    }
}
