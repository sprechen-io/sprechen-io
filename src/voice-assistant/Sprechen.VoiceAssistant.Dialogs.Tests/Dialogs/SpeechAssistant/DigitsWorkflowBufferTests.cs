namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DigitsWorkflowBufferTests
{
    [Fact]
    public void GetSpokenDigits_WithMultipleBlocks_SpacesDigitsAndPausesBetweenBlocks()
    {
        var buffer = CreateBuffer("123", "456");

        Assert.Equal("1 2 3, 4 5 6", buffer.GetSpokenDigits());
        Assert.Equal("123456", buffer.GetDigits());
    }

    [Fact]
    public void GetSpokenDigits_WhenEmpty_ReturnsEmptyString()
    {
        var buffer = new DigitsWorkflowBuffer();

        Assert.Equal(string.Empty, buffer.GetSpokenDigits());
        Assert.Equal(string.Empty, buffer.GetDigits());
    }

    [Fact]
    public void GetSpokenDigits_WithSingleBlock_SpacesEveryDigit()
    {
        var buffer = CreateBuffer("123");

        Assert.Equal("1 2 3", buffer.GetSpokenDigits());
        Assert.Equal("123", buffer.GetDigits());
    }

    [Fact]
    public void GetSpokenDigits_WithSingleDigit_ReturnsThatDigit()
    {
        var buffer = CreateBuffer("1");

        Assert.Equal("1", buffer.GetSpokenDigits());
        Assert.Equal("1", buffer.GetDigits());
    }

    [Fact]
    public void GetSpokenDigits_WhenBlockHasSurroundingWhitespace_RendersTrimmedDigits()
    {
        var buffer = CreateBuffer(" 12 ", "3");

        Assert.Equal("1 2, 3", buffer.GetSpokenDigits());
        Assert.Equal("123", buffer.GetDigits());
    }

    private static DigitsWorkflowBuffer CreateBuffer(params string[] blocks)
    {
        var buffer = new DigitsWorkflowBuffer();
        foreach (var block in blocks)
        {
            buffer.Append(block, confidencePercent: null);
        }

        return buffer;
    }
}
