using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class VoiceCommandDialogResultSilentInputTests
{
    [Fact]
    public void SilentInputRequested_RequiresProjectionAndMarksPausedTerminal()
    {
        var projection = new FormSilentInputEligibilityProjection(
            "nameField",
            SpeechAssistantSilentInputControlKind.Email);

        var result = VoiceCommandDialogResult.SilentInputRequested(projection);

        Assert.True(result.IsSilentInputRequested);
        Assert.True(result.WasPaused);
        Assert.Same(projection, result.SilentInputEligibility);
        Assert.Null(VoiceCommandDialogResult.AbandonedWithoutContext(
            System.Collections.Immutable.ImmutableDictionary<string, string>.Empty).SilentInputEligibility);
    }
}
