using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Datenschutzfreigabe, Sprachübergabe und angeforderte Texte beim Vorlesen buchstabierter Werte.
/// Der phonetische Renderer und die Sprachausgabe sind dabei simuliert.
/// </summary>
public sealed class SpellingBlockReadbackTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task ReadBackBlockAsync_WhenSpellOutAllowed_SpeaksPhoneticForLanguage(string isoCode)
    {
        var spoken = new List<string>();
        var readback = CreateReadback(spoken, spellOutAllowed: true, out var capturedIso);

        await readback.ReadBackBlockAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            SpellingWorkflowTestFactory.CreateField(),
            "ab",
            CancellationToken.None);

        Assert.Single(spoken);
        Assert.Equal($"PHON[ab|{isoCode}]", spoken[0]);
        Assert.Equal(isoCode, capturedIso());
    }

    [Fact]
    public async Task ReadBackBlockAsync_WhenForbidden_SpeaksNothing()
    {
        var spoken = new List<string>();
        var readback = CreateReadback(spoken, spellOutAllowed: false, out _);

        await readback.ReadBackBlockAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            "ab",
            CancellationToken.None);

        Assert.Empty(spoken);
    }

    [Fact]
    public async Task ReadBackBlockAsync_WhitespaceBlock_IsNoOp()
    {
        var spoken = new List<string>();
        var readback = CreateReadback(spoken, spellOutAllowed: true, out _);

        await readback.ReadBackBlockAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            "   ",
            CancellationToken.None);

        Assert.Empty(spoken);
    }

    [Fact]
    public async Task ReadBackWholeValueAsync_WhenAllowed_SpeaksReadbackPromptWithPhonetic()
    {
        var spoken = new List<string>();
        var readback = CreateReadback(spoken, spellOutAllowed: true, out _);

        await readback.ReadBackWholeValueAsync(
            SpellingWorkflowTestFactory.CreateContext("de"),
            SpellingWorkflowTestFactory.CreateField(),
            "abc",
            "a b c",
            CancellationToken.None);

        Assert.Single(spoken);
        Assert.Contains("PHON[abc|de]", spoken[0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de", "Aus Datenschutzgr\u00fcnden kann ich diesen Wert nicht vorlesen. Sagen Sie die n\u00e4chsten Buchstaben, oder fertig.")]
    [InlineData("en", "For privacy reasons, I cannot read this value aloud. Say more letters, or done.")]
    [InlineData("fr", "Pour des raisons de confidentialit\u00e9, je ne peux pas lire cette valeur \u00e0 voix haute. Dites d\'autres lettres, ou terminer.")]
    [InlineData("es", "Por motivos de privacidad, no puedo leer este valor en voz alta. Diga m\u00e1s letras, o terminar.")]
    [InlineData("it", "Per motivi di privacy, non posso leggere questo valore ad alta voce. Dica altre lettere, o fine.")]
    public async Task ReadBackWholeValueAsync_WhenForbidden_SpeaksLocalizedActionablePrivacyGuidance(
        string isoCode,
        string expected)
    {
        var spoken = new List<string>();
        var readback = CreateReadback(spoken, spellOutAllowed: false, out _);

        await readback.ReadBackWholeValueAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            SpellingWorkflowTestFactory.CreateField(),
            "abc",
            "a b c",
            CancellationToken.None);

        Assert.Equal(expected, Assert.Single(spoken));
    }

    [Theory]
    [InlineData("de", "Aus Datenschutzgr\u00fcnden kann ich diesen Wert nicht vorlesen. Sag die n\u00e4chsten Buchstaben, oder fertig.")]
    [InlineData("en", "For privacy reasons, I cannot read this value aloud. Say more letters, or done.")]
    [InlineData("fr", "Pour des raisons de confidentialit\u00e9, je ne peux pas lire cette valeur \u00e0 voix haute. Dis d\'autres lettres, ou terminer.")]
    [InlineData("es", "Por motivos de privacidad, no puedo leer este valor en voz alta. Di m\u00e1s letras, o terminar.")]
    [InlineData("it", "Per motivi di privacy, non posso leggere questo valore ad alta voce. Di' altre lettere, o fine.")]
    public void GetSpellingReadbackUnavailablePrompt_Informal_UsesLocalizedGuidance(
        string isoCode,
        string expected)
    {
        var prompt = FormSpellingWorkflowPromptBuilder.GetSpellingReadbackUnavailablePrompt(
            SpellingWorkflowTestFactory.CreateContext(isoCode).Contract,
            isoCode,
            PolitenessLevel.Informal);

        Assert.Equal(expected, prompt);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task ReadBackBlockAsync_SpeaksAtReducedReadbackRate(string isoCode)
    {
        var rates = new List<double>();
        var readback = CreateRateCapturingReadback(rates);

        await readback.ReadBackBlockAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            SpellingWorkflowTestFactory.CreateField(),
            "ab",
            CancellationToken.None);

        // Die Ausgabe wird mit reduzierter Sprechgeschwindigkeit angefordert.
        Assert.Equal(0.85d, Assert.Single(rates), precision: 3);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task ReadBackWholeValueAsync_SpeaksAtReducedReadbackRate(string isoCode)
    {
        var rates = new List<double>();
        var readback = CreateRateCapturingReadback(rates);

        await readback.ReadBackWholeValueAsync(
            SpellingWorkflowTestFactory.CreateContext(isoCode),
            SpellingWorkflowTestFactory.CreateField(),
            "abc",
            "a b c",
            CancellationToken.None);

        Assert.Equal(0.85d, Assert.Single(rates), precision: 3);
    }

    private static SpellingBlockReadback CreateRateCapturingReadback(List<double> rates)
    {
        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((_, _, voiceSettings, _) =>
            {
                rates.Add(voiceSettings.GetSpeechRate());
                return Task.CompletedTask;
            });

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy
            .Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(true);

        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, iso) => $"PHON[{value}|{iso}]");

        return new SpellingBlockReadback(
            speech.Object,
            privacy.Object,
            renderer.Object);
    }

    private static SpellingBlockReadback CreateReadback(
        List<string> spoken,
        bool spellOutAllowed,
        out Func<string?> capturedIso)
    {
        string? lastIso = null;
        capturedIso = () => lastIso;

        var speech = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speech
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });

        var privacy = new Mock<IFormDialogPrivacyReadbackPolicy>(MockBehavior.Loose);
        privacy
            .Setup(p => p.IsSpellOutAllowedInConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync(spellOutAllowed);

        var renderer = new Mock<IPhoneticSpellOutRenderer>(MockBehavior.Loose);
        renderer
            .Setup(r => r.Render(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((value, iso) =>
            {
                lastIso = iso;
                return $"PHON[{value}|{iso}]";
            });

        return new SpellingBlockReadback(
            speech.Object,
            privacy.Object,
            renderer.Object);
    }
}
