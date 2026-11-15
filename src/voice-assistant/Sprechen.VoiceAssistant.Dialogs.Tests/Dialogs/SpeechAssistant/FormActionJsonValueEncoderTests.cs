using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormActionJsonValueEncoderTests
{
    [Theory]
    [InlineData("\"hello\"", "hello")]
    [InlineData("42", "42")]
    [InlineData("4.25", "4.25")]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("null", "")]
    public void TryEncodeJsonValue_WithScalarValue_EncodesCanonicalString(string json, string expected)
    {
        var value = Parse(json);

        var encoded = FormActionJsonValueEncoder.TryEncodeJsonValue(value, out var result);

        Assert.True(encoded);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryEncodeJsonValue_WithStringArray_EncodesSortedNonBlankStringItems()
    {
        var value = Parse("""["z", " ", "a", 3, "m", " a "]""");

        var encoded = FormActionJsonValueEncoder.TryEncodeJsonValue(value, out var result);

        Assert.True(encoded);
        Assert.Equal("a|a|m|z", result);
    }

    [Fact]
    public void TryEncodeJsonValue_WithObject_ReturnsFalse()
    {
        var value = Parse("""{"nested":true}""");

        var encoded = FormActionJsonValueEncoder.TryEncodeJsonValue(value, out var result);

        Assert.False(encoded);
        Assert.Equal(string.Empty, result);
    }

    private static JsonElement Parse(string json)
        => JsonDocument.Parse(json).RootElement;
}
