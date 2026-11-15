using Sprechen.Common.Cqrs;
using Xunit;

namespace Sprechen.Common.Cqrs.Tests;

public sealed class QueryDispatcherTests
{
    #region Dispatch Tests

    [Fact]
    public async Task DispatchAsync_RegisteredHandler_ExecutesHandler()
    {
        var dispatcher = new QueryDispatcher();
        var handler = new TestQueryHandler();
        dispatcher.Register<TestQuery, string>(handler);

        var result = await dispatcher.DispatchAsync(new TestQuery("test-input"));

        Assert.Equal("query result: test-input", result);
    }

    [Fact]
    public async Task DispatchAsync_UnregisteredHandler_ThrowsInvalidOperation()
    {
        var dispatcher = new QueryDispatcher();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(new TestQuery("test")));
    }

    [Fact]
    public async Task DispatchAsync_MultipleHandlers_DispatchesToCorrectHandler()
    {
        var dispatcher = new QueryDispatcher();
        var handler1 = new TestQueryHandler();
        var handler2 = new AnotherQueryHandler();
        dispatcher.Register<TestQuery, string>(handler1);
        dispatcher.Register<AnotherQuery, int>(handler2);

        var result1 = await dispatcher.DispatchAsync(new TestQuery("input"));
        var result2 = await dispatcher.DispatchAsync(new AnotherQuery(7));

        Assert.Equal("query result: input", result1);
        Assert.Equal(49, result2);
    }

    [Fact]
    public async Task DispatchAsync_ComplexResult_ReturnsCorrectType()
    {
        var dispatcher = new QueryDispatcher();
        var handler = new ComplexQueryHandler();
        dispatcher.Register<ComplexQuery, ComplexResult>(handler);

        var result = await dispatcher.DispatchAsync(new ComplexQuery("test"));

        Assert.Equal("test", result.Name);
        Assert.Equal(42, result.Value);
    }

    #endregion

}


