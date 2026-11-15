using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.Speech.State;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantSessionOnboardingAnnouncerTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task AnnounceOnceAsync_WhenCallsOverlap_WaitsForSingleAnnouncement()
    {
        var firstCallStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    firstCallStarted.TrySetResult();
                    await releaseFirstCall.Task.WaitAsync(cancellationToken);
                }
            });
        var announcer = CreateAnnouncer(textToSpeech.Object);

        var first = announcer.AnnounceOnceAsync("de", CancellationToken.None).AsTask();
        await firstCallStarted.Task.WaitAsync(TestTimeout);
        var second = announcer.AnnounceOnceAsync("de", CancellationToken.None).AsTask();

        await Task.Yield();
        Assert.False(second.IsCompleted, "A concurrent caller may not pass onboarding while the owner is still speaking.");

        releaseFirstCall.TrySetResult();
        await Task.WhenAll(first, second).WaitAsync(TestTimeout);

        Assert.Equal(2, Volatile.Read(ref callCount));
        VerifySuccessfulAnnouncement(textToSpeech);
    }

    [Fact]
    public async Task AnnounceOnceAsync_WhenWaitingCallerCancels_PropagatesWithoutCancelingActiveAnnouncement()
    {
        var firstCallStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirstCall = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var callCount = 0;
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken cancellationToken) =>
            {
                if (Interlocked.Increment(ref callCount) == 1)
                {
                    firstCallStarted.TrySetResult();
                    await releaseFirstCall.Task.WaitAsync(cancellationToken);
                }
            });
        var announcer = CreateAnnouncer(textToSpeech.Object);

        var activeAnnouncement = announcer.AnnounceOnceAsync("de", CancellationToken.None).AsTask();
        await firstCallStarted.Task.WaitAsync(TestTimeout);
        using var waitingCallerCancellation = new CancellationTokenSource();
        var waitingCaller = announcer
            .AnnounceOnceAsync("de", waitingCallerCancellation.Token)
            .AsTask();

        waitingCallerCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waitingCaller);
        Assert.False(activeAnnouncement.IsCompleted, "Canceling a waiter may not cancel the active announcement.");

        releaseFirstCall.TrySetResult();
        await activeAnnouncement.WaitAsync(TestTimeout);

        Assert.Equal(2, Volatile.Read(ref callCount));
        VerifySuccessfulAnnouncement(textToSpeech);
    }

    [Fact]
    public async Task AnnounceOnceAsync_WhenIntroFails_RetriesWholeAnnouncement()
    {
        var failure = new InvalidOperationException("intro failed");
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .SetupSequence(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(failure)
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask);
        var announcer = CreateAnnouncer(textToSpeech.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => announcer.AnnounceOnceAsync("de", CancellationToken.None).AsTask());
        await announcer.AnnounceOnceAsync("de", CancellationToken.None);

        Assert.Same(failure, exception);
        VerifySuccessfulAnnouncement(textToSpeech, expectedIntroCalls: 2);
    }

    [Fact]
    public async Task AnnounceOnceAsync_WhenRepeatHintFails_RetriesWholeAnnouncement()
    {
        var failure = new InvalidOperationException("repeat hint failed");
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .SetupSequence(player => player.SpeakAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .ThrowsAsync(failure)
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask);
        var announcer = CreateAnnouncer(textToSpeech.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => announcer.AnnounceOnceAsync("de", CancellationToken.None).AsTask());
        await announcer.AnnounceOnceAsync("de", CancellationToken.None);

        Assert.Same(failure, exception);
        VerifySuccessfulAnnouncement(textToSpeech, expectedIntroCalls: 2, expectedRepeatHintCalls: 2);
    }

    private static SpeechAssistantSessionOnboardingAnnouncer CreateAnnouncer(ITextToSpeechPlayer textToSpeechPlayer)
    {
        var userLanguageState = new UserLanguageState();
        userLanguageState.SetSelectedIsoCode("de");
        userLanguageState.SetSelectedPolitenessLevel(PolitenessLevel.Formal);
        var context = SpeechAssistantContextTestFactory.Create(
            SpeechInteractionSignalsTestFactory.Create(Mock.Of<IClientMessageBus>()),
            userLanguageState,
            Mock.Of<IUserLanguageSettingsWorkflow>(),
            Mock.Of<ISpeechInputClient>(),
            null,
            Mock.Of<ISpeechInputClient>(),
            textToSpeechPlayer,
            Mock.Of<IEarconPlayer>(),
            Mock.Of<IWakeWordProvider>(),
            new NoOpFormsCatalogDialog(),
            new NoOpFormSelectionDialog(),
            Mock.Of<IVoiceCommandDialog>(),
            NullLogger<SpeechAssistantService>.Instance);

        return new SpeechAssistantSessionOnboardingAnnouncer(
            context.SetupDependencies,
            context.InteractionDependencies);
    }

    private static void VerifySuccessfulAnnouncement(
        Mock<ITextToSpeechPlayer> textToSpeech,
        int expectedIntroCalls = 1,
        int expectedRepeatHintCalls = 1)
    {
        var intro = FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt("de", PolitenessLevel.Formal);
        var repeatHint = FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt("de", PolitenessLevel.Formal);

        textToSpeech.Verify(
            player => player.SpeakAsync(intro, "de", It.IsAny<CancellationToken>()),
            Times.Exactly(expectedIntroCalls));
        textToSpeech.Verify(
            player => player.SpeakAsync(repeatHint, "de", It.IsAny<CancellationToken>()),
            Times.Exactly(expectedRepeatHintCalls));
        textToSpeech.VerifyNoOtherCalls();
    }
}
