using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Textformatierung bei ausgeschaltetem Buchstabieren. Passende Codes werden
/// in Vierergruppen getrennt; die übrigen Beispiele bleiben nach dem Trimmen unverändert.
/// Die Gliederung soll lange Codes beim Vorlesen leichter überprüfbar machen.
/// Eine Freigabe zum Vorlesen, Audioausgabe und gespeicherte Formularwerte werden hier nicht geprüft.
/// </summary>
public sealed class FormConfirmationReadbackValueFormatterTests
{
    [Fact]
    public void Format_SensitiveOptInIban_IsChunkedIntoFourCharacterBlocks()
    {

        const string iban = "DE44500105175407324931";

        var result = Format(iban, InputMode.Voice);

        Assert.Equal("DE44 5001 0517 5407 3249 31", result);
    }

    [Theory]
    [InlineData("AB12CD34", "AB12 CD34")]
    [InlineData("64289", "6428 9")]
    [InlineData("A1B2C", "A1B2 C")]
    [InlineData("XKCD1234567890AB", "XKCD 1234 5678 90AB")]
    [InlineData("De44xB12", "De44 xB12")] // Groß- und Kleinschreibung ändern die Gruppierung nicht.
    public void Format_SensitiveOptInCodeRun_IsChunkedLeftAligned(string value, string expected)
    {
        // Passende Codes über vier Zeichen werden von links in Vierergruppen aufgeteilt, einschließlich einer Restgruppe.
        Assert.Equal(expected, Format(value, InputMode.Voice));
    }

    [Fact]
    public void Format_PaddedCodeRun_IsTrimmedFirstThenChunked()
    {
        // Äußere Leerzeichen werden vor der Prüfung entfernt.
        const string paddedIban = "  DE44500105175407324931  ";

        Assert.Equal("DE44 5001 0517 5407 3249 31", Format(paddedIban, InputMode.Voice));
    }

    [Fact]
    public void Format_SensitiveDigitsCapture_IsChunkedNotReadDigitByDigit()
    {
        // Bei ausgeschaltetem Buchstabieren wird auch eine Zifferneingabe in Vierergruppen formatiert.
        var result = Format("12345678", InputMode.Digits);

        Assert.Equal("1234 5678", result);
    }

    [Theory]
    [InlineData("DE44 5001 0517")] // Bereits getrennte Werte bleiben so erhalten.
    [InlineData("DE44-5001")] // Der Bindestrich verhindert die Codegruppierung.
    [InlineData("DE-AD-BEEF")] // Auch innerhalb dieses Codes verhindern Bindestriche die Gruppierung.
    [InlineData("Müller")] // Ein Wort ohne Ziffern wird nicht gruppiert.
    [InlineData("XKCDAB")] // Ein reiner Buchstabencode ohne Ziffern wird ebenfalls nicht gruppiert.
    [InlineData("AB12")] // Vier Zeichen überschreiten die Mindestlänge nicht.
    [InlineData("A1")] // Kurzer Wert ohne Gruppierung.
    public void Format_NonCodeShapes_AreNotChunked(string value)
    {

        Assert.Equal(value, Format(value, InputMode.Voice));
    }

    [Fact]
    public void Format_EmptyAndWhitespaceOnly_StaysEmpty()
    {
        Assert.Equal(string.Empty, Format(string.Empty, InputMode.Voice));
        Assert.Equal(string.Empty, Format("   ", InputMode.Voice));
    }

    [Fact]
    public void Format_SensitiveOptInCodeRun_NeverAltersContentOnlyAddsSpaces()
    {
        const string value = "DE44500105175407324931";

        var result = Format(value, InputMode.Voice);


        Assert.Equal(value, result.Replace(" ", string.Empty, StringComparison.Ordinal));
    }

    private static string Format(string value, InputMode inputMode) =>
        new FormConfirmationReadbackValueFormatter(
                new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance))
            .Format(
                value,
                inputMode,
                allowSpellOut: false,
                isoCode: "de");
}
