using System.Collections.Immutable;
using Sprechen.Common.Cqrs;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;
using Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus.GetFormSummaryQueryHandlerTestsSupport;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormStatus;

public sealed class GetFormSummaryQueryHandlerTests
{
    #region GetSummary Tests

    [Fact]
    public async Task HandleAsync_WithValues_ReturnsSummaryLines()
    {
        var state = new TestFormState(new Dictionary<string, (string Label, string? Value)>
        {
            ["field1"] = ("Name", "John Doe"),
            ["field2"] = ("Email", "john@example.com")
        });
        var handler = new GetFormSummaryQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormSummaryQuery());

        Assert.Equal(2, result.Lines.Length);
        Assert.Contains(result.Lines, l => l.Label == "Name" && l.Value == "John Doe");
        Assert.Contains(result.Lines, l => l.Label == "Email" && l.Value == "john@example.com");
    }

    [Fact]
    public async Task HandleAsync_EmptyForm_ReturnsEmptyLines()
    {
        var state = new TestFormState(new Dictionary<string, (string Label, string? Value)>());
        var handler = new GetFormSummaryQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormSummaryQuery());

        Assert.Empty(result.Lines);
    }

    [Fact]
    public async Task HandleAsync_SkipsNullValues()
    {
        var state = new TestFormState(new Dictionary<string, (string Label, string? Value)>
        {
            ["field1"] = ("Name", "John"),
            ["field2"] = ("Email", null),
            ["field3"] = ("Phone", "123456")
        });
        var handler = new GetFormSummaryQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormSummaryQuery());

        Assert.Equal(2, result.Lines.Length);
        Assert.DoesNotContain(result.Lines, l => l.Label == "Email");
    }

    [Fact]
    public async Task HandleAsync_SkipsEmptyValues()
    {
        var state = new TestFormState(new Dictionary<string, (string Label, string? Value)>
        {
            ["field1"] = ("Name", "John"),
            ["field2"] = ("Email", ""),
            ["field3"] = ("Phone", "   ")
        });
        var handler = new GetFormSummaryQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormSummaryQuery());

        Assert.Single(result.Lines);
        Assert.Equal("Name", result.Lines[0].Label);
    }

    [Fact]
    public async Task HandleAsync_PreservesFieldOrder()
    {
        var state = new TestFormState(
            new Dictionary<string, (string Label, string? Value)>
            {
                ["field1"] = ("First", "A"),
                ["field2"] = ("Second", "B"),
                ["field3"] = ("Third", "C")
            },
            orderedFieldIds: ["field3", "field1", "field2"]);
        var handler = new GetFormSummaryQueryHandler(state);

        var result = await handler.HandleAsync(new GetFormSummaryQuery());

        Assert.Equal(3, result.Lines.Length);
        Assert.Equal("Third", result.Lines[0].Label);
        Assert.Equal("First", result.Lines[1].Label);
        Assert.Equal("Second", result.Lines[2].Label);
    }

    #endregion

}



