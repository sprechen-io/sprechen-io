using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormActionKindResolverTests
{
    [Theory]
    [InlineData(" SETVISIBLE ", (int)FormActionKind.SetVisible)]
    [InlineData("setenabled", (int)FormActionKind.SetEnabled)]
    [InlineData("setReadOnly", (int)FormActionKind.SetReadOnly)]
    [InlineData("setValue", (int)FormActionKind.SetValue)]
    [InlineData("clearValue", (int)FormActionKind.ClearValue)]
    [InlineData("copyValue", (int)FormActionKind.CopyValue)]
    [InlineData("speak", (int)FormActionKind.Speak)]
    [InlineData("navigateToFieldGroup", (int)FormActionKind.NavigateToFieldGroup)]
    [InlineData("focusField", (int)FormActionKind.FocusField)]
    public void TryResolve_WithSupportedKind_ResolvesTrimmedCaseInsensitiveValue(string value, int expected)
    {
        var action = Parse($$"""{ "kind": "{{value}}" }""");

        var resolved = FormActionKindResolver.TryResolve(action, out var kind);

        Assert.True(resolved);
        Assert.Equal((FormActionKind)expected, kind);
    }

    [Theory]
    [InlineData("\"not-an-object\"")]
    [InlineData("{ }")]
    [InlineData("{ \"kind\": 42 }")]
    [InlineData("{ \"kind\": \"   \" }")]
    [InlineData("{ \"kind\": \"customAction\" }")]
    [InlineData("{ \"kind\": \"unknownAction\" }")]
    public void TryResolve_WithUnsupportedOrMalformedKind_ReturnsFalse(string json)
    {
        var action = Parse(json);

        var resolved = FormActionKindResolver.TryResolve(action, out _);

        Assert.False(resolved);
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
