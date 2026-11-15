namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SpellingWorkflowBufferTests
{
    [Fact]
    public void Append_AccumulatesCharactersAcrossSeveralTurns()
    {
        var buffer = new SpellingWorkflowBuffer();

        buffer.Append("m", confidencePercent: null);
        buffer.Append("a", confidencePercent: null);
        buffer.Append("r", confidencePercent: null);
        buffer.Append("kus", confidencePercent: null);

        Assert.True(buffer.HasCharacters);
        Assert.Equal("markus", buffer.GetValue());
    }

    [Fact]
    public void GetSpokenCharacters_SpacesEachCharacterForLetterByLetterReadback()
    {
        var buffer = new SpellingWorkflowBuffer();
        buffer.Append("ab", confidencePercent: null);
        buffer.Append("c", confidencePercent: null);

        Assert.Equal("a b c", buffer.GetSpokenCharacters());
    }

    [Fact]
    public void TryRemoveLastCharacter_RemovesOnlyTheLastCharacter()
    {
        var buffer = new SpellingWorkflowBuffer();
        buffer.Append("ma", confidencePercent: null);
        buffer.Append("x", confidencePercent: null);

        var removed = buffer.TryRemoveLastCharacter();

        Assert.True(removed);
        Assert.Equal("ma", buffer.GetValue());
    }

    [Fact]
    public void TryRemoveLastCharacter_WhenEmpty_ReturnsFalse()
    {
        var buffer = new SpellingWorkflowBuffer();

        Assert.False(buffer.TryRemoveLastCharacter());
        Assert.False(buffer.HasCharacters);
    }
}
