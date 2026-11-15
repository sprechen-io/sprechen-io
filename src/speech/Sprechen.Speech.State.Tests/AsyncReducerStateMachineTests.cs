using Moq;
using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests;

public sealed class AsyncReducerStateMachineTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task DispatchAsync_WhenEventDispatched_UpdatesState()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        reducer
            .Setup(r => r.Reduce(0, "inc"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(1));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DispatchAsync("inc");

        await WaitUntilAsync(() => machine.State == 1);
    }

    [Fact]
    public async Task Faulted_WhenReducerThrows_RaisedOnceWithTheExceptionAndStateBecomesFaulted()
    {
        var failure = new InvalidOperationException("boom");
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        reducer.Setup(r => r.Reduce(0, "boom")).Throws(failure);

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        var faults = new List<Exception>();
        var faulted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        machine.Faulted += ex =>
        {
            faults.Add(ex);
            faulted.TrySetResult(true);
        };

        await machine.DispatchAsync("boom");
        await faulted.Task.WaitAsync(WaitTimeout);

        Assert.Single(faults);
        Assert.Same(failure, faults[0]);

        var observed = Assert.Throws<InvalidOperationException>(() => machine.State);
        Assert.Contains("faulted", observed.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Same(failure, observed.InnerException);
    }

    [Fact]
    public async Task DispatchAsync_WhenInterrupting_CancelsRunningEffects()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        var effect = new Mock<IStateEffect<string, object>>(MockBehavior.Strict);

        var effectStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effectCancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        effect
            .Setup(e => e.ExecuteAsync(
                It.IsAny<object>(),
                It.IsAny<IEventDispatcher<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns((object _, IEventDispatcher<string> _, CancellationToken token) => new ValueTask(RunEffectAsync(token)));

        reducer
            .Setup(r => r.Reduce(0, "start"))
            .Returns(StateUpdate<int, string, object>.WithEffects(1, effect.Object));

        reducer
            .Setup(r => r.Reduce(1, "next"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(2));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DispatchAsync("start");
        await effectStarted.Task.WaitAsync(WaitTimeout);

        await machine.DispatchAsync("next", mode: DispatchMode.Interrupt);
        await effectCancelled.Task.WaitAsync(WaitTimeout);

        await WaitUntilAsync(() => machine.State == 2);

        async Task RunEffectAsync(CancellationToken cancellationToken)
        {
            effectStarted.TrySetResult(true);

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                effectCancelled.TrySetResult(true);
                throw;
            }
        }
    }

    [Fact]
    public async Task DispatchAsync_WhenEffectDispatchesInterruptEvent_DoesNotDropDispatchedEvent()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        var effect = new Mock<IStateEffect<string, object>>(MockBehavior.Strict);

        effect
            .Setup(e => e.ExecuteAsync(
                It.IsAny<object>(),
                It.IsAny<IEventDispatcher<string>>(),
                It.IsAny<CancellationToken>()))
            .Returns((object _, IEventDispatcher<string> dispatcher, CancellationToken token)
                => dispatcher.DispatchAsync("next", DispatchMode.Interrupt, token));

        reducer
            .Setup(r => r.Reduce(0, "start"))
            .Returns(StateUpdate<int, string, object>.WithEffects(1, effect.Object));

        reducer
            .Setup(r => r.Reduce(1, "next"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(2));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DispatchAsync("start");

        await WaitUntilAsync(() => machine.State == 2);
    }

    [Fact]
    public async Task DispatchAsync_WhenReducerThrows_FaultsStateMachine()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        reducer
            .Setup(r => r.Reduce(0, "boom"))
            .Throws(new InvalidOperationException("boom"));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DispatchAsync("boom");

        await WaitUntilAsync(() =>
        {
            try
            {
                _ = machine.State;
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        });
    }

    [Fact]
    public async Task DispatchAsync_WhenEffectThrows_FaultsStateMachine()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        var effect = new Mock<IStateEffect<string, object>>(MockBehavior.Strict);

        effect
            .Setup(e => e.ExecuteAsync(
                It.IsAny<object>(),
                It.IsAny<IEventDispatcher<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        reducer
            .Setup(r => r.Reduce(0, "start"))
            .Returns(StateUpdate<int, string, object>.WithEffects(1, effect.Object));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DispatchAsync("start");

        await WaitUntilAsync(() =>
        {
            try
            {
                _ = machine.State;
                return false;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        });
    }

    [Fact]
    public async Task StateChanged_WhenStateChanges_RaisesEvent()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        reducer
            .Setup(r => r.Reduce(0, "inc"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(1));

        var tcs = new TaskCompletionSource<StateChanged<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());
        machine.StateChanged += change => tcs.TrySetResult(change);

        await machine.DispatchAsync("inc");

        var change = await tcs.Task.WaitAsync(WaitTimeout);
        Assert.Equal(0, change.Previous);
        Assert.Equal(1, change.Current);
    }

    [Fact]
    public async Task StateChanged_WhenSubscriberThrows_FaultsStateMachine()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        reducer
            .Setup(r => r.Reduce(0, "inc"))
            .Returns(StateUpdate<int, string, object>.WithoutEffects(1));

        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());
        machine.StateChanged += _ => throw new InvalidOperationException("observer failed");

        await machine.DispatchAsync("inc");

        await WaitUntilAsync(() =>
        {
            try
            {
                _ = machine.State;
                return false;
            }
            catch (InvalidOperationException ex)
            {
                return ex.InnerException is InvalidOperationException inner &&
                       inner.Message == "observer failed";
            }
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await machine.DispatchAsync("next"));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("observer failed", exception.InnerException!.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalled_PreventsFurtherDispatch()
    {
        var reducer = new Mock<IStateReducer<int, string, object>>(MockBehavior.Strict);
        await using var machine = new AsyncReducerStateMachine<int, string, object>(0, reducer.Object, new object());

        await machine.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await machine.DispatchAsync("inc"));

        await machine.DisposeAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using var cts = new CancellationTokenSource(WaitTimeout);

        while (!predicate())
        {
            cts.Token.ThrowIfCancellationRequested();
            await Task.Delay(10, cts.Token);
        }
    }
}

