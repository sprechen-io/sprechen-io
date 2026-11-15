namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class RecognitionProfilePolicyTests
{
    [Theory]
    [InlineData(InputMode.Spelling, RecognitionProfile.Spelling)]
    [InlineData(InputMode.Digits, RecognitionProfile.Digits)]
    [InlineData(InputMode.Voice, RecognitionProfile.Conversational)]
    [InlineData(InputMode.Keyboard, RecognitionProfile.Conversational)]
    [InlineData(InputMode.Literal, RecognitionProfile.Conversational)]
    public void Resolve_MapsInputModeToExpectedProfile(InputMode mode, RecognitionProfile expected)
    {
        Assert.Equal(expected, RecognitionProfilePolicy.Resolve(mode));
    }

    [Fact]
    public void Resolve_CoversEveryInputModeValue_WithNoSilentDefault()
    {
        foreach (var mode in Enum.GetValues<InputMode>())
        {
            var profile = RecognitionProfilePolicy.Resolve(mode);
            Assert.Contains(profile, Enum.GetValues<RecognitionProfile>());

            var expected = mode switch
            {
                InputMode.Spelling => RecognitionProfile.Spelling,
                InputMode.Digits => RecognitionProfile.Digits,
                _ => RecognitionProfile.Conversational
            };
            Assert.Equal(expected, profile);
        }
    }

    [Fact]
    public void Resolve_WhenInputModeUnknown_FailsSafeToConversational()
    {
        Assert.Equal(RecognitionProfile.Conversational, RecognitionProfilePolicy.Resolve((InputMode)999));
    }
}
