using Moq;
using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class DialogFlowExtensionsTests
{
    [Fact]
    public async Task DispatchAsync_WhenNoModeProvided_UsesEnqueueAndNoCancellation()
    {
        var flow = new Mock<IDialogFlow<string>>();
        flow.Setup(f => f.DispatchAsync("evt", DispatchMode.Enqueue, CancellationToken.None)).Returns(ValueTask.CompletedTask);

        await flow.Object.DispatchAsync("evt");

        flow.VerifyAll();
    }

    [Fact]
    public async Task DispatchAsync_WhenModeProvided_UsesModeAndNoCancellation()
    {
        var flow = new Mock<IDialogFlow<string>>();
        flow.Setup(f => f.DispatchAsync("evt", DispatchMode.Interrupt, CancellationToken.None)).Returns(ValueTask.CompletedTask);

        await flow.Object.DispatchAsync("evt", DispatchMode.Interrupt);

        flow.VerifyAll();
    }

    [Fact]
    public async Task DispatchAsync_WhenCancellationProvided_UsesEnqueueAndCancellation()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        var flow = new Mock<IDialogFlow<string>>();
        flow.Setup(f => f.DispatchAsync("evt", DispatchMode.Enqueue, token)).Returns(ValueTask.CompletedTask);

        await flow.Object.DispatchAsync("evt", token);

        flow.VerifyAll();
    }
}


