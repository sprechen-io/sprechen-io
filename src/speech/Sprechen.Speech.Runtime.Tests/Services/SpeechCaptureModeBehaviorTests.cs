using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureModeBehaviorTests
{
    [Theory]
    [InlineData(SpeechCaptureMode.Listening)]
    [InlineData(SpeechCaptureMode.ListeningContinuation)]
    [InlineData(SpeechCaptureMode.ActivationListening)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation)]
    [InlineData(SpeechCaptureMode.ShortConfirmation)]
    [InlineData(SpeechCaptureMode.PromptInterruption)]
    [InlineData(SpeechCaptureMode.PromptInterruptionDetection)]
    public void For_ValidMode_ReturnsBehaviorWithMatchingMode(SpeechCaptureMode mode)
    {
        var behavior = SpeechCaptureModeBehavior.For(mode);

        Assert.Equal(mode, behavior.Mode);
        Assert.NotNull(behavior.SpeechOnsetDescription);
        Assert.NotEmpty(behavior.SpeechOnsetDescription);
    }

    [Fact]
    public void For_UnknownMode_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SpeechCaptureModeBehavior.For((SpeechCaptureMode)999));
    }

    [Fact]
    public void For_PromptInterruption_EnablesBargeIn()
    {
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);

        Assert.True(behavior.BargeInEnabled);
        Assert.True(behavior.RequiresActivePlaybackContext);
        Assert.False(behavior.UsesActivationProfile);
        Assert.False(behavior.IsContinuationMode);
    }

    [Fact]
    public void For_PromptInterruptionDetection_RequiresPlaybackContextWithoutBargeIn()
    {
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruptionDetection);

        Assert.False(behavior.BargeInEnabled);
        Assert.True(behavior.RequiresActivePlaybackContext);
        Assert.False(behavior.UsesActivationProfile);
        Assert.False(behavior.IsContinuationMode);
    }

    [Theory]
    [InlineData(SpeechCaptureMode.ActivationListening, false)]
    [InlineData(SpeechCaptureMode.ActivationListeningContinuation, true)]
    public void For_ActivationModes_UsesActivationProfile(
        SpeechCaptureMode mode,
        bool expectedContinuation)
    {
        var behavior = SpeechCaptureModeBehavior.For(mode);

        Assert.True(behavior.UsesActivationProfile);
        Assert.False(behavior.BargeInEnabled);
        Assert.False(behavior.RequiresActivePlaybackContext);
        Assert.Equal(expectedContinuation, behavior.IsContinuationMode);
    }

    [Theory]
    [InlineData(SpeechCaptureMode.Listening, false)]
    [InlineData(SpeechCaptureMode.ListeningContinuation, true)]
    public void For_StandardListeningModes_HasNoSpecialFlags(SpeechCaptureMode mode, bool expectedContinuation)
    {
        var behavior = SpeechCaptureModeBehavior.For(mode);

        Assert.False(behavior.BargeInEnabled);
        Assert.False(behavior.UsesActivationProfile);
        Assert.False(behavior.RequiresActivePlaybackContext);
        Assert.Equal(expectedContinuation, behavior.IsContinuationMode);
    }

    [Fact]
    public void TheLongerOnsetWindow_IsCarriedByTheModeledProfile_NotByABehaviorFlag()
    {
        // Die unterschiedlichen Stillegrenzen müssen aus den modellierten Profilen hervorgehen.
        var debounceByMode = Enum.GetValues<SpeechCaptureMode>()
            .ToDictionary(mode => mode, SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs);

        var longest = debounceByMode.Values.Max();

        var modesWithTheLongestWindow = debounceByMode
            .Where(entry => entry.Value == longest)
            .Select(entry => entry.Key)
            .ToArray();

        Assert.Equal([SpeechCaptureMode.ActivationListening], modesWithTheLongestWindow);
        Assert.All(debounceByMode.Values, debounce => Assert.True(debounce.GetValueOrDefault() > 0));
    }

    [Fact]
    public void For_PromptInterruption_HasDistinctOnsetDescription()
    {
        var promptInterruption = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruption);
        var detection = SpeechCaptureModeBehavior.For(SpeechCaptureMode.PromptInterruptionDetection);
        var listening = SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening);

        Assert.NotEqual(promptInterruption.SpeechOnsetDescription, detection.SpeechOnsetDescription);
        Assert.NotEqual(promptInterruption.SpeechOnsetDescription, listening.SpeechOnsetDescription);
        Assert.NotEqual(detection.SpeechOnsetDescription, listening.SpeechOnsetDescription);
    }

    [Fact]
    public void For_ShortConfirmation_UsesShortConfirmationProfileWithoutOtherFlags()
    {
        var behavior = SpeechCaptureModeBehavior.For(SpeechCaptureMode.ShortConfirmation);

        Assert.True(behavior.UsesShortConfirmationProfile);
        Assert.False(behavior.BargeInEnabled);
        Assert.False(behavior.UsesActivationProfile);
        Assert.False(behavior.RequiresActivePlaybackContext);
        Assert.False(behavior.IsContinuationMode);
    }

    [Fact]
    public void For_ShortConfirmationProfile_IsRestrictedToShortConfirmationMode()
    {
        var modesWithShortConfirmation = Enum.GetValues<SpeechCaptureMode>()
            .Select(SpeechCaptureModeBehavior.For)
            .Where(behavior => behavior.UsesShortConfirmationProfile)
            .Select(behavior => behavior.Mode)
            .ToArray();

        Assert.Equal([SpeechCaptureMode.ShortConfirmation], modesWithShortConfirmation);
    }

    [Fact]
    public void For_OnlyPromptInterruption_EnablesBargeIn()
    {
        var allModes = Enum.GetValues<SpeechCaptureMode>();
        var modesWithBargeIn = allModes
            .Select(SpeechCaptureModeBehavior.For)
            .Where(b => b.BargeInEnabled)
            .Select(b => b.Mode)
            .ToArray();

        Assert.Equal([SpeechCaptureMode.PromptInterruption], modesWithBargeIn);
    }

    [Fact]
    public void Default_HasListeningMode()
    {
        var defaultBehavior = default(SpeechCaptureModeBehavior);

        Assert.Equal(SpeechCaptureMode.Listening, defaultBehavior.Mode);
        Assert.False(defaultBehavior.BargeInEnabled);
        Assert.Null(defaultBehavior.SpeechOnsetDescription);
    }
}
