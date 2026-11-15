namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, wann freie Texte bei geringerer Konfidenz zur Bestätigung vorgelesen werden dürfen.
/// Buchstabieren, unterdrücktes Vorlesen, Codefelder und die geprüften strukturierten Typen bleiben ausgenommen.
/// </summary>
public sealed class FreeTextConfidenceRelaxationPolicyTests
{
    [Theory]
    [InlineData(InputMode.Voice)]
    [InlineData(InputMode.Digits)]
    [InlineData(InputMode.Literal)]
    public void AllowsAcceptWithReadback_ForAudibleFreeTextDictation_Relaxes(InputMode mode)
    {
        Assert.True(FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            expectedInputMode: mode,
            codeStyle: false,
            valueTypeKind: ValueTypeKinds.String,
            speakValueInConfirmation: true));
    }

    [Fact]
    public void AllowsAcceptWithReadback_ForSpelledTextCapture_StaysStrict()
    {
        // Buchstabierte Texte haben ebenfalls den Typ String. Der Eingabemodus
        // muss sie deshalb von der gelockerten Annahmegrenze ausschließen.
        Assert.False(FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            expectedInputMode: InputMode.Spelling,
            codeStyle: false,
            valueTypeKind: ValueTypeKinds.String,
            speakValueInConfirmation: true));
    }

    [Fact]
    public void AllowsAcceptWithReadback_ForMaskedFreeText_StaysStrict()
    {
        // Ohne vorgelesenen Wert fehlt diese Möglichkeit, eine falsche Erkennung zu bemerken.
        Assert.False(FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            expectedInputMode: InputMode.Voice,
            codeStyle: false,
            valueTypeKind: ValueTypeKinds.String,
            speakValueInConfirmation: false));
    }

    [Fact]
    public void AllowsAcceptWithReadback_ForCodeField_StaysStrict()
    {
        Assert.False(FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            expectedInputMode: InputMode.Voice,
            codeStyle: true,
            valueTypeKind: ValueTypeKinds.String,
            speakValueInConfirmation: true));
    }

    [Theory]
    [InlineData(ValueTypeKinds.Integer)]
    [InlineData(ValueTypeKinds.Boolean)]
    [InlineData(ValueTypeKinds.Date)]
    [InlineData(ValueTypeKinds.Time)]
    [InlineData(ValueTypeKinds.Enum)]
    [InlineData(ValueTypeKinds.Set)]
    public void AllowsAcceptWithReadback_ForConstrainedTypes_StaysStrict(string valueTypeKind)
    {
        Assert.False(FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            expectedInputMode: InputMode.Voice,
            codeStyle: false,
            valueTypeKind: valueTypeKind,
            speakValueInConfirmation: true));
    }
}
