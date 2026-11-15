using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormSilentInputFallbackLabelPromptIdResolverTests
{
    [Fact]
    public void Resolve_KeyboardFallback_ReturnsKeyboardLabelPromptId()
    {
        var resolved = new FormSilentInputFallbackLabelPromptIdResolver()
            .Resolve(SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard);

        Assert.Equal(FormDialogPromptIds.SilentInputLabelKeyboard, resolved);
    }

    [Fact]
    public void Resolve_BrailleFallback_ReturnsBrailleLabelPromptId()
    {
        var resolved = new FormSilentInputFallbackLabelPromptIdResolver()
            .Resolve(SensitiveDataPolicyDefaults.SilentInputFallbackBraille);

        Assert.Equal(FormDialogPromptIds.SilentInputLabelBraille, resolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("any")]
    [InlineData("Keyboard")] // Nur die exakt kleingeschriebene Konstante wird erkannt.
    [InlineData("unknown")]
    public void Resolve_UnknownFallback_ReturnsNull(string fallback)
    {
        Assert.Null(new FormSilentInputFallbackLabelPromptIdResolver().Resolve(fallback));
    }
}
