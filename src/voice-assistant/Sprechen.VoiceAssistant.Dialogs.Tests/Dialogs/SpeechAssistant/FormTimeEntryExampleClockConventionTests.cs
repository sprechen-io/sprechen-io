using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zeitbeispiele der erzeugten Ansagen: Englisch mit AM/PM, die übrigen
/// vier geprüften Sprachen mit 14: ohne AM/PM. Jeder Beispieltext wird außerdem
/// direkt geparst und muss 14:30 ergeben. Sprachausgabe und Spracherkennung
/// sind nicht Teil dieser Prüfung.
/// </summary>
public sealed class FormTimeEntryExampleClockConventionTests
{
    private const string TwentyFourHourMarker = "14:";

    [Theory]
    [InlineData("en", true)]
    [InlineData("de", false)]
    [InlineData("fr", false)]
    [InlineData("es", false)]
    [InlineData("it", false)]
    public void SpokenTimeExample_MatchesTheLocaleClockConvention(string iso, bool twelveHourCustom)
    {
        var phrase = FormTimeEntryExamplePromptBuilder.GetTimeEntryExamplePhrase(iso);
        Assert.False(string.IsNullOrWhiteSpace(phrase), $"[{iso}] has no spoken time example phrase.");

        if (twelveHourCustom)
        {
            Assert.True(
                HasMeridiem(phrase!),
                $"[{iso}] maps to a 12-hour-clock locale but its spoken time example \"{phrase}\" carries no " +
                "AM/PM meridiem. Use a 12-hour example such as \"2:30 PM\".");
            Assert.DoesNotContain(TwentyFourHourMarker, phrase!, StringComparison.Ordinal);
        }
        else
        {
            Assert.Contains(TwentyFourHourMarker, phrase!, StringComparison.Ordinal);
            Assert.False(
                HasMeridiem(phrase!),
                $"[{iso}] is a 24-hour-clock locale but its spoken time example \"{phrase}\" carries an AM/PM " +
                "meridiem. Use the 24-hour everyday custom such as \"14:30\".");
        }
    }

    // Der Beispieltext muss im Parser derselben Sprache den Wert 14:30 ergeben.
    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void SpokenTimeExample_RoundTripsThroughTheTimeParser(string iso)
    {
        var phrase = FormTimeEntryExamplePromptBuilder.GetTimeEntryExamplePhrase(iso);
        Assert.False(string.IsNullOrWhiteSpace(phrase));

        var parser = new FormTemporalTranscriptParser();
        var outcome = parser.ParseTime(iso, phrase!);

        Assert.True(
            outcome.Success,
            $"[{iso}] spoken time example \"{phrase}\" must parse through the time parser.");
        Assert.Equal("14:30", outcome.CanonicalValue);
    }

    // Sucht die großgeschriebenen Teilstrings AM oder PM, damit etwa ein kleines „am“ nicht zählt.
    private static bool HasMeridiem(string text)
        => text.Contains("AM", StringComparison.Ordinal)
            || text.Contains("PM", StringComparison.Ordinal);
}
