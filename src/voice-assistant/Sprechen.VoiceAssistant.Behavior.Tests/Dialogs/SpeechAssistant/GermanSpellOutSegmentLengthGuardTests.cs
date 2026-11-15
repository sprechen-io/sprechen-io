using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die deutschen Buchstabierausgaben der aufgeführten Testwerte auf Segmente mit mindestens zwei Wörtern.
/// Modellierte technische Zeichennamen sind ausgenommen.
/// </summary>
public sealed class GermanSpellOutSegmentLengthGuardTests
{
    private const string LanguageCode = "de";
    private const int MinimumTokensPerSegment = 2;

    private static readonly char[] SegmentBoundaries = ['.', ':', ';', '!', '?'];

    // Beispielwerte für Kennung, E-Mail, Namen, Wörter und technische Zeichen.
    public static TheoryData<string> SpelledValues =>
    [
        "P007",
        "anna.b@example.de",
        "A1.",
        "A.B@C-_",
        "Marta",
        "BLAU",
        "X_9",
        "DE89370400440532013000"
    ];

    /// <summary>
    /// Modellierte technische Zeichennamen sind von der Zweiwortregel ausgenommen.
    /// </summary>
    private static IReadOnlySet<string> ExemptCharacterNames(PhoneticSpellOutRenderer renderer)
        => renderer.CharacterNamesFor(LanguageCode);

    [Theory]
    [MemberData(nameof(SpelledValues))]
    public void GermanSpellOut_LeavesNoSpokenSegmentWithASingleToken(string value)
    {
        var renderer = new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance);
        var exempt = ExemptCharacterNames(renderer);

        var rendered = renderer.Render(value, LanguageCode);

        var shortSegments = rendered
            .Split(SegmentBoundaries, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => segment.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length < MinimumTokensPerSegment)
            .Where(segment => !exempt.Contains(segment))
            .ToArray();

        Assert.True(
            shortSegments.Length == 0,
            $"Der deutsche Spell-out von '{value}' lässt ein Segment mit weniger als zwei Token stehen. "
            + "Die deutsche Stimme verschluckt ein allein stehendes Wort in 43 % der Renderings. "
            + "Das Element braucht eine Paarung - Buchstaben tragen 'X wie Wort', "
            + "Ziffern tragen 'X wie Zahlwort' über 'digitWords'."
            + Environment.NewLine + $"gerendert: {rendered}"
            + Environment.NewLine + $"betroffen: [{string.Join(" | ", shortSegments)}]");
    }
}
