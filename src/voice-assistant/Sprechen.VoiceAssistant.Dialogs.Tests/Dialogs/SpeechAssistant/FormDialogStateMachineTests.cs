using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogStateMachineTests
{

    [Fact]
    public void ApplyMacroTransition_WhenTransitionIsUnsupported_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FormDialogStateMachine.ApplyMacroTransition(
                DialogMacroStateIds.Submitted,
                DialogMacroTransitionTriggers.NavigateNext,
                DialogMacroStateIds.FieldGroupIntro));

        Assert.Contains("Unsupported dialog macro transition", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyMicroTransition_WhenTransitionIsUnsupported_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FormDialogStateMachine.ApplyMicroTransition(
                DialogMicroStateIds.Prompting,
                DialogMicroTransitionTriggers.ValidationPassed,
                DialogMicroStateIds.Confirmation));

        Assert.Contains("Unsupported dialog micro transition", exception.Message, StringComparison.Ordinal);
    }
}
