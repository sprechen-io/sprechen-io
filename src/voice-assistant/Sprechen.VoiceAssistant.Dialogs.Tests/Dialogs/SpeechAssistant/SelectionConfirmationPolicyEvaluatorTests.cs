using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SelectionConfirmationPolicyEvaluatorTests
{
    [Theory]
    [InlineData(ConfirmationPolicy.Always, false, true)]
    [InlineData(ConfirmationPolicy.Always, true, true)]
    [InlineData(ConfirmationPolicy.Never, false, false)]
    [InlineData(ConfirmationPolicy.Never, true, true)]
    [InlineData(ConfirmationPolicy.Ambiguous, false, false)]
    [InlineData(ConfirmationPolicy.Ambiguous, true, true)]
    public void RequiresConfirmation_ResolvesAccordingToPolicy(
        ConfirmationPolicy confirmationPolicy,
        bool isAmbiguous,
        bool expected)
    {
        var actual = new SelectionConfirmationPolicyEvaluator()
            .RequiresConfirmation(confirmationPolicy, isAmbiguous);

        Assert.Equal(expected, actual);
    }
}
