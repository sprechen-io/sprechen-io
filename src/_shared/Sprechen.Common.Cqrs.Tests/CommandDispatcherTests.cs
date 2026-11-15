using Sprechen.Common.Cqrs;
using Xunit;

namespace Sprechen.Common.Cqrs.Tests;

public sealed class CommandDispatcherTests
{
    #region Dispatch Tests

    [Fact]
    public async Task DispatchAsync_RegisteredHandler_ExecutesHandler()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestCommandHandler();
        dispatcher.Register<TestCommand, string>(handler);

        var result = await dispatcher.DispatchAsync(new TestCommand("test-input"));

        Assert.Equal("handled: test-input", result);
    }

    [Fact]
    public async Task DispatchAsync_UnregisteredHandler_ThrowsInvalidOperation()
    {
        var dispatcher = new CommandDispatcher();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(new TestCommand("test")));
    }

    [Fact]
    public async Task DispatchAsync_VoidCommand_ReturnsUnit()
    {
        var dispatcher = new CommandDispatcher();
        var handler = new TestVoidCommandHandler();
        dispatcher.Register<TestVoidCommand, Unit>(handler);

        var result = await dispatcher.DispatchAsync(new TestVoidCommand());

        Assert.Equal(Unit.Value, result);
        Assert.True(handler.WasCalled);
    }

    [Fact]
    public async Task DispatchAsync_MultipleHandlers_DispatchesToCorrectHandler()
    {
        var dispatcher = new CommandDispatcher();
        var handler1 = new TestCommandHandler();
        var handler2 = new AnotherCommandHandler();
        dispatcher.Register<TestCommand, string>(handler1);
        dispatcher.Register<AnotherCommand, int>(handler2);

        var result1 = await dispatcher.DispatchAsync(new TestCommand("input"));
        var result2 = await dispatcher.DispatchAsync(new AnotherCommand(5));

        Assert.Equal("handled: input", result1);
        Assert.Equal(10, result2);
    }

    #endregion

}



