namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandConfirmationRequestTests
{
    [Fact]
    public void Constructor_WhenTranscriptForFallbackIsNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new FormVoiceCommandConfirmationRequest(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_AcceptsBlankFallback_ForTheExplicitEmptySetCompletion(string transcript)
    {
        // Eine zulässige leere Mehrfachauswahl benötigt auch einen leeren Ersatztext.
        // Der Konstruktor muss diesen Zustand erlauben; die spätere Ansage wird hier nicht geprüft.
        var request = new FormVoiceCommandConfirmationRequest(transcript);

        Assert.Equal(transcript, request.TranscriptForFallback);
    }

    [Fact]
    public void Constructor_WhenValid_PreservesConfirmationFacts()
    {
        var request = new FormVoiceCommandConfirmationRequest("Alice");

        Assert.Equal("Alice", request.TranscriptForFallback);
    }
}
