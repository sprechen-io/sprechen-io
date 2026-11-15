using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.PromptTemplates;

/// <summary>
/// Prüft die gewählte Anredeform bei mehreren Ansageauflösern und den formalen Standard
/// der Überladungen ohne Anredeparameter.
/// </summary>
public sealed class ClassAPolitenessResolverTests
{
    [Fact]
    public void CatalogOverload_Informal_PrefersTheInformalVariant_ThenFallsBackToBase()
    {
        // Fehlt eine informelle Variante, bleibt die allgemeine Vorlage das Rückfallziel.
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
            "dialog.capture.noInput", "de", PolitenessLevel.Informal, out var informal));
        Assert.Equal("Ich habe nichts gehört. Bitte sprich noch einmal.", informal);

        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
            "dialog.capture.noInput", "de", PolitenessLevel.Formal, out var formal));
        Assert.Equal("Ich habe nichts gehört. Bitte sprechen Sie noch einmal.", formal);

        // replayLeadIn besitzt keine eigene Anredevariante und verwendet die allgemeine Vorlage.
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
            "dialog.capture.replayLeadIn", "de", PolitenessLevel.Informal, out var noVariant));
        Assert.Equal("Noch einmal.", noVariant);
    }

    [Theory]
    [InlineData("de", "Ich konnte dich gerade nicht verstehen. Bitte sag es noch einmal.")]
    [InlineData("fr", "Je n'ai pas réussi à te comprendre. Répète, s'il te plaît.")]
    public void RecoverableCapture_Informal_SpeaksTheInformalRegister(string isoCode, string expected)
        => Assert.Equal(expected, RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt(isoCode, PolitenessLevel.Informal));

    [Fact]
    public void RecoverableCapture_Formal_KeepsTheFormalRegister()
        => Assert.Equal(
            "Ich konnte Sie gerade nicht verstehen. Bitte sagen Sie es noch einmal.",
            RecoverableCapturePromptTextResolver.GetRecoverableFailurePrompt("de", PolitenessLevel.Formal));

    [Fact]
    public void NoInputCapture_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Ich habe nichts gehört. Bitte sprich noch einmal.",
            NoInputCapturePromptTextResolver.GetNoInputPrompt("de", PolitenessLevel.Informal));
        Assert.Equal(
            "Ich habe nichts gehört. Bitte sprechen Sie noch einmal.",
            NoInputCapturePromptTextResolver.GetNoInputPrompt("de", PolitenessLevel.Formal));
    }

    [Fact]
    public void AllowBargeInEnabled_HonoursTheSelectedRegister()
    {
        Assert.Equal(
            "Unterbrechung ist jetzt aktiv. Du kannst mich beim Sprechen unterbrechen.",
            AllowBargeInPromptTextResolver.GetEnabledPrompt("de", PolitenessLevel.Informal));
        Assert.Equal(
            "Unterbrechung ist jetzt aktiv. Sie können mich beim Sprechen unterbrechen.",
            AllowBargeInPromptTextResolver.GetEnabledPrompt("de", PolitenessLevel.Formal));
    }

    [Theory]
    [InlineData("de", "Bitte sag eine unterstützte Sprache, zum Beispiel Deutsch.")]
    [InlineData("it", "Di' una lingua supportata, per esempio Deutsch.")]
    public void ChangeLanguageMissingArgument_Informal_SpeaksTheInformalRegister(string isoCode, string expected)
        => Assert.Equal(expected, FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt(isoCode, "Deutsch", PolitenessLevel.Informal));

    [Fact]
    public void ChangeLanguageMissingArgument_Formal_KeepsTheFormalRegister()
        => Assert.Equal(
            "Bitte sagen Sie eine unterstützte Sprache, zum Beispiel Deutsch.",
            FormDialogFlowPromptTextResolver.GetMissingArgumentPrompt("de", "Deutsch", PolitenessLevel.Formal));

    [Fact]
    public void ChangeLanguageWakeWordHint_HonoursTheSelectedRegister()
    {
        Assert.Contains("Du kannst", FormDialogFlowPromptTextResolver.GetWakeWordHintPrompt("de", PolitenessLevel.Informal), StringComparison.Ordinal);
        Assert.Contains("Sie k", FormDialogFlowPromptTextResolver.GetWakeWordHintPrompt("de", PolitenessLevel.Formal), StringComparison.Ordinal);
    }

    [Fact]
    public void AutoAdvanceManualContinuation_HonoursTheSelectedRegister()
    {
        Assert.Contains("Sag weiter", FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de", PolitenessLevel.Informal), StringComparison.Ordinal);
        Assert.Contains("Sagen Sie weiter", FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de", PolitenessLevel.Formal), StringComparison.Ordinal);
    }

    [Fact]
    public void SingleArgumentResolvers_DefaultToTheFormalRegister()
    {
        // Überladungen ohne Anredeparameter sollen dieselbe formale Vorlage verwenden.
        Assert.Equal(
            AllowBargeInPromptTextResolver.GetEnabledPrompt("de", PolitenessLevel.Formal),
            AllowBargeInPromptTextResolver.GetEnabledPrompt("de"));
        Assert.Equal(
            NoInputCapturePromptTextResolver.GetNoInputPrompt("de", PolitenessLevel.Formal),
            NoInputCapturePromptTextResolver.GetNoInputPrompt("de"));
        Assert.Equal(
            FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de", PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetAutoAdvanceManualContinuationPrompt("de"));
    }
}
