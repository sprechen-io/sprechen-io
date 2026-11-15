using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpeechAssistantVoiceInputPauseTerminalTests
{
    [Fact]
    public async Task CompleteExpectedAsync_WhenOperationThrowsVoiceInputPausedException_Completes()
    {
        await SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(
            _ => throw new VoiceInputPausedException(),
            CancellationToken.None);
    }

    [Fact]
    public async Task CompleteExpectedAsync_WhenOperationThrowsOtherException_Propagates()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(
                _ => throw new InvalidOperationException("boom"),
                CancellationToken.None));
    }

    [Fact]
    public async Task CompleteExpectedAsync_WhenOperationIsNull_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteExpectedAsync_WhenTokenIsCanceled_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => SpeechAssistantVoiceInputPauseTerminal.CompleteExpectedAsync(
                _ => Task.CompletedTask,
                cts.Token));
    }
}
