using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSpeakActionExecutorTests
{
    [Fact]
    public void ApplySpeak_WithPromptId_TrimsPromptAndReturnsNoStateChange()
    {
        var action = Parse("""{ "promptId": " welcome-prompt " }""");
        var prompts = new List<string>();

        var changed = FormSpeakActionExecutor.ApplySpeak(action, prompts);

        Assert.False(changed);
        Assert.Equal(["welcome-prompt"], prompts);
    }

    [Theory]
    [InlineData("{ }")]
    [InlineData("{ \"promptId\": 42 }")]
    [InlineData("{ \"promptId\": \"   \" }")]
    public void ApplySpeak_WithMissingOrMalformedPromptId_ReturnsFalseWithoutPrompt(string json)
    {
        var action = Parse(json);
        var prompts = new List<string>();

        var changed = FormSpeakActionExecutor.ApplySpeak(action, prompts);

        Assert.False(changed);
        Assert.Empty(prompts);
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
