using Sprechen.Service.Abstractions;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramRecognitionFormattingFlagsTests
{
    [Fact]
    public void FromProfile_Conversational_KeepsSmartFormattingAndPunctuationWithNumeralsUnset()
    {
        var flags = DeepgramRecognitionFormattingFlags.FromProfile(RecognitionProfile.Conversational);

        Assert.True(flags.SmartFormat);
        Assert.True(flags.Punctuate);
        Assert.Null(flags.Numerals);
    }

    [Fact]
    public void FromProfile_Spelling_DisablesAllThreeFlags()
    {
        var flags = DeepgramRecognitionFormattingFlags.FromProfile(RecognitionProfile.Spelling);

        Assert.False(flags.SmartFormat);
        Assert.False(flags.Punctuate);
        Assert.False(flags.Numerals);
    }

    [Fact]
    public void FromProfile_Digits_DisablesFormattingButForcesNumeralsOn()
    {
        var flags = DeepgramRecognitionFormattingFlags.FromProfile(RecognitionProfile.Digits);

        Assert.False(flags.SmartFormat);
        Assert.False(flags.Punctuate);
        Assert.True(flags.Numerals);
    }

    [Fact]
    public void FromProfile_DefaultEnumValue_ResolvesToConversational()
    {
        var flags = DeepgramRecognitionFormattingFlags.FromProfile(default);

        Assert.True(flags.SmartFormat);
        Assert.True(flags.Punctuate);
        Assert.Null(flags.Numerals);
    }

    [Fact]
    public void FromProfile_UnknownCastFromIntValue_FallsBackToConversational()
    {
        var flags = DeepgramRecognitionFormattingFlags.FromProfile((RecognitionProfile)999);

        Assert.True(flags.SmartFormat);
        Assert.True(flags.Punctuate);
        Assert.Null(flags.Numerals);
    }
}
