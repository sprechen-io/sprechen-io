using Moq;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechAssistantTraceContextTests
{
    [Fact]
    public void RoundTripsTraceAndScope()
    {
        var trace = Mock.Of<ISpeechAssistantTrace>();

        var context = new SpeechAssistantTraceContext(trace, "abc-123", SpeechAssistantTraceScope.FormsCatalogDialog);

        Assert.Same(trace, context.Trace);
        Assert.Equal("abc-123", context.CorrelationId);
        Assert.Equal(SpeechAssistantTraceScope.FormsCatalogDialog, context.Scope);
    }

    [Fact]
    public void AllowsNullTrace()
    {
        var context = new SpeechAssistantTraceContext(null, "id", SpeechAssistantTraceScope.SelectionDialogRuntime);

        Assert.Null(context.Trace);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizesBlankCorrelationIdToNull(string? blank)
    {
        var context = new SpeechAssistantTraceContext(null, blank, SpeechAssistantTraceScope.SelectionDialogRuntime);

        Assert.Null(context.CorrelationId);
    }

    [Fact]
    public void TrimsCorrelationId()
    {
        var context = new SpeechAssistantTraceContext(null, "  id-7  ", SpeechAssistantTraceScope.SelectionDialogRuntime);

        Assert.Equal("id-7", context.CorrelationId);
    }
}
