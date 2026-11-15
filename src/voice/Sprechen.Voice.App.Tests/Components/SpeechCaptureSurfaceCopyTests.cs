using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Web.SpeechComponents;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureSurfaceCopyTests
{
    [Theory]
    [InlineData(SpeechAssistantActivationState.NotStarted, false, "Start voice assistant", "Voice assistant not started.")]
    [InlineData(SpeechAssistantActivationState.Starting, false, "Voice assistant is starting", "Voice assistant is starting.")]
    [InlineData(SpeechAssistantActivationState.Ready, false, "Pause voice assistant", "Voice assistant is ready.")]
    [InlineData(SpeechAssistantActivationState.Paused, false, "Resume voice input", "Voice input is paused.")]
    [InlineData(SpeechAssistantActivationState.Failed, false, "Start voice assistant again", "Voice assistant failed to start. Press again to retry.")]
    [InlineData(SpeechAssistantActivationState.Ready, true, "Pause voice assistant", "Voice assistant is listening.")]
    public void ResolveCopy_ForEnglishDisplayLanguage_ReturnsExpectedAccessibleText(
        SpeechAssistantActivationState activationState,
        bool isRecording,
        string expectedAriaLabel,
        string expectedStatusAnnouncement)
    {
        var snapshot = new SpeechCaptureSurfaceStateSnapshot(activationState, isRecording, null);

        Assert.Equal(expectedAriaLabel, SpeechCaptureSurfaceCopy.GetAriaLabel(snapshot, "en"));
        Assert.Equal(expectedStatusAnnouncement, SpeechCaptureSurfaceCopy.GetStatusAnnouncement(snapshot, "en"));
    }

    [Fact]
    public void ResolveCopy_WhenLexicalFallbackIsActive_UsesFallbackAnnouncement()
    {
        var snapshot = new SpeechCaptureSurfaceStateSnapshot(
            SpeechAssistantActivationState.Ready,
            false,
            null,
            new SpeechWakeListeningFallbackState(
                true,
                "WAKE_DETECTOR_UNAVAILABLE",
                LexicalWakeFallbackReason.DetectorUnavailable));

        Assert.Equal("Pause voice assistant (wake-word detection may respond slower)", SpeechCaptureSurfaceCopy.GetAriaLabel(snapshot, "en"));
        Assert.Equal(
            "Voice assistant is ready. Wake-word detection may respond a little slower right now.",
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(snapshot, "en"));
    }

    [Theory]
    [InlineData(
        LexicalWakeFallbackReason.OverrideIneligible,
        "Voice assistant is ready. Because custom wake words are set, wake-word detection may respond a little slower.")]
    [InlineData(
        LexicalWakeFallbackReason.DetectorUnavailable,
        "Voice assistant is ready. Wake-word detection may respond a little slower right now.")]
    [InlineData(
        LexicalWakeFallbackReason.DetectorWarmup,
        "Voice assistant is ready. Wake-word detection is still getting ready and responds a little slower for a moment.")]
    [InlineData(
        LexicalWakeFallbackReason.DetectorFailure,
        "Voice assistant is ready. Wake-word detection is responding a little slower at the moment.")]
    [InlineData(
        LexicalWakeFallbackReason.RecognitionOnlyActivation,
        "Voice assistant is ready. Wake-word detection may respond a little slower.")]
    [InlineData(
        LexicalWakeFallbackReason.CatalogFallbackOnly,
        "Voice assistant is ready. Wake-word detection may respond a little slower for now.")]
    [InlineData(
        LexicalWakeFallbackReason.None,
        "Voice assistant is ready. Wake-word detection may respond a little slower for now.")]
    public void ResolveCopy_WhenLexicalFallbackIsActive_MapsReasonToAnnouncement(
        LexicalWakeFallbackReason reason,
        string expectedAnnouncement)
    {
        var snapshot = new SpeechCaptureSurfaceStateSnapshot(
            SpeechAssistantActivationState.Ready,
            false,
            null,
            new SpeechWakeListeningFallbackState(true, string.Empty, reason));

        Assert.Equal(expectedAnnouncement, SpeechCaptureSurfaceCopy.GetStatusAnnouncement(snapshot, "en"));
    }

    [Fact]
    public void ResolveCopy_ForGermanDisplayLanguage_ReturnsGermanTextNotEnglish()
    {
        var notStarted = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.NotStarted, false, null);
        var listening = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Ready, true, null);



        Assert.Equal("Sprachassistent starten", SpeechCaptureSurfaceCopy.GetAriaLabel(notStarted, "de"));
        Assert.Equal("Sprachassistent nicht gestartet.", SpeechCaptureSurfaceCopy.GetStatusAnnouncement(notStarted, "de"));
        Assert.NotEqual(
            SpeechCaptureSurfaceCopy.GetAriaLabel(listening, "en"),
            SpeechCaptureSurfaceCopy.GetAriaLabel(listening, "de"));
        Assert.NotEqual(
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(listening, "en"),
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(listening, "de"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    public void ResolveCopy_ForUnsupportedOrNullDisplayLanguage_ResolvesToDefaultLanguageNonEmpty(string? isoCode)
    {
        var notStarted = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.NotStarted, false, null);


        Assert.False(string.IsNullOrWhiteSpace(SpeechCaptureSurfaceCopy.GetAriaLabel(notStarted, isoCode)));
        Assert.False(string.IsNullOrWhiteSpace(SpeechCaptureSurfaceCopy.GetStatusAnnouncement(notStarted, isoCode)));
    }

    [Fact]
    public void ResolveStatus_WhenReadyWithWakeWords_AnnouncesWakeWordsAndModeledHelpPhrase()
    {
        var ready = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Ready, false, null);

        var english = SpeechCaptureSurfaceCopy.GetStatusAnnouncement(ready, "en", "Hey Sprechen, Sprechen");

        Assert.Equal(
            "Voice assistant is ready. Say Hey Sprechen, Sprechen to begin. After the wake word you can say help at any time.",
            english);

        var german = SpeechCaptureSurfaceCopy.GetStatusAnnouncement(ready, "de", "Hey Sprechen");
        Assert.Equal(
            "Sprachassistent ist bereit. Sagen Sie Hey Sprechen, um zu beginnen. Nach dem Weckwort können Sie jederzeit hilfe sagen.",
            german);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveStatus_WhenReadyWithoutWakeWords_FallsBackToPlainReadyText(string? wakeWords)
    {
        var ready = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Ready, false, null);


        var announcement = SpeechCaptureSurfaceCopy.GetStatusAnnouncement(ready, "en", wakeWords);

        Assert.Equal("Voice assistant is ready.", announcement);
        Assert.DoesNotContain("{wakeWords}", announcement, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveStatus_WhenWakeWordsSuppliedButStateNotPlainReady_IgnoresWakeWords()
    {
        var starting = new SpeechCaptureSurfaceStateSnapshot(SpeechAssistantActivationState.Starting, false, null);

        Assert.Equal(
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(starting, "en"),
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(starting, "en", "Hey Sprechen"));

        var readyFallback = new SpeechCaptureSurfaceStateSnapshot(
            SpeechAssistantActivationState.Ready,
            false,
            null,
            new SpeechWakeListeningFallbackState(true, string.Empty, LexicalWakeFallbackReason.DetectorUnavailable));
        Assert.Equal(
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(readyFallback, "en"),
            SpeechCaptureSurfaceCopy.GetStatusAnnouncement(readyFallback, "en", "Hey Sprechen"));
    }
}
