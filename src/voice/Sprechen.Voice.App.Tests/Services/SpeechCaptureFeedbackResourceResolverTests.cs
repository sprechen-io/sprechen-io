using Sprechen.Voice.App.Services;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;

namespace Sprechen.Voice.App.Tests.Services;

public sealed class SpeechCaptureFeedbackResourceResolverTests
{
    private readonly SpeechCaptureFeedbackResourceResolver _resolver = new();

    [Theory]
    [InlineData(SpeechErrorCode.AuthMissingApiKey, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed)]
    [InlineData(SpeechErrorCode.AuthInvalidApiKey, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed)]
    [InlineData(SpeechErrorCode.VoiceConfigurationInvalidRequest, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed)]
    [InlineData(SpeechErrorCode.VoiceConfigurationUnsupportedVersion, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed)]
    [InlineData(SpeechErrorCode.VoiceConfigurationInvalidConfiguration, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingConfigurationFailed)]
    [InlineData(SpeechErrorCode.LanguageCodeRequired, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingLanguageUnavailable)]
    [InlineData(SpeechErrorCode.LanguageCodeInvalid, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingLanguageUnavailable)]
    [InlineData(SpeechErrorCode.LanguageCodeUnsupported, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingLanguageUnavailable)]
    [InlineData(SpeechErrorCode.AudioOutputMimeTypeRequired, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingAudioOutputUnsupported)]
    [InlineData(SpeechErrorCode.AudioOutputMimeTypeUnsupported, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingAudioOutputUnsupported)]
    [InlineData(SpeechErrorCode.ProviderRateLimited, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingRateLimited)]
    [InlineData(SpeechErrorCode.Unspecified, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed)]
    [InlineData(SpeechErrorCode.ProviderFailure, VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed)]
    public void ResolveTranscriptSpeakingFailureResourceId_WhenErrorCodeIsKnown_ReturnsModeledResourceId(
        SpeechErrorCode errorCode,
        string expectedResourceId)
    {
        var resourceId = _resolver.ResolveTranscriptSpeakingFailureResourceId(errorCode);

        Assert.Equal(expectedResourceId, resourceId);
    }

    [Theory]
    [MemberData(nameof(AllKnownErrorCodes))]
    public void ResolveTranscriptSpeakingFailureResourceId_WhenErrorCodeIsKnown_ReturnsCatalogResource(
        SpeechErrorCode errorCode)
    {
        var resourceId = _resolver.ResolveTranscriptSpeakingFailureResourceId(errorCode);

        Assert.True(
            VoiceInterfaceModelCatalog.Resources.HasResource(resourceId),
            $"Resource '{resourceId}' for error code '{errorCode}' is missing from the voice resource catalog.");
    }

    [Fact]
    public void ResolveTranscriptSpeakingFailureResourceId_WhenErrorCodeIsUnknown_ReturnsModeledDefaultResourceId()
    {
        var resourceId = _resolver.ResolveTranscriptSpeakingFailureResourceId((SpeechErrorCode)int.MaxValue);

        Assert.Equal(VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed, resourceId);
    }

    [Theory]
    [InlineData(SpeechErrorCode.VoiceCommandDialogFailed, VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed)]
    [InlineData(SpeechErrorCode.MicrophonePermissionDenied, VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied)]
    [InlineData(SpeechErrorCode.MicrophoneUnavailable, VoiceInterfaceResourceIds.SpeechCaptureFailureMicrophoneUnavailable)]
    [InlineData(SpeechErrorCode.Unspecified, VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric)]
    [InlineData(SpeechErrorCode.FormRuleLoopGuardExceeded, VoiceInterfaceResourceIds.SpeechCaptureFailureRuleLoopGuardExceeded)]
    [InlineData(SpeechErrorCode.ProviderFailure, VoiceInterfaceResourceIds.SpeechCaptureFailureDefault)]
    public void ResolveFailureAnnouncementResourceId_WhenErrorCodeIsKnown_ReturnsModeledResourceId(
        SpeechErrorCode errorCode,
        string expectedResourceId)
    {
        var resourceId = _resolver.ResolveFailureAnnouncementResourceId(errorCode);

        Assert.Equal(expectedResourceId, resourceId);
    }

    [Theory]
    [MemberData(nameof(AllKnownErrorCodes))]
    public void ResolveFailureAnnouncementResourceId_WhenErrorCodeIsKnown_ReturnsCatalogResource(
        SpeechErrorCode errorCode)
    {
        var resourceId = _resolver.ResolveFailureAnnouncementResourceId(errorCode);

        Assert.True(
            VoiceInterfaceModelCatalog.Resources.HasResource(resourceId),
            $"Resource '{resourceId}' for error code '{errorCode}' is missing from the voice resource catalog.");
    }

    [Fact]
    public void ResolveFailureAnnouncementMessage_WhenDisplayLanguageIsSupported_ReturnsModeledText()
    {
        var expected = ResolveResourceText(
            VoiceInterfaceResourceIds.SpeechCaptureFailureVoiceCommandDialogFailed,
            "de");

        var message = _resolver.ResolveFailureAnnouncementMessage(SpeechErrorCode.VoiceCommandDialogFailed, "de");

        Assert.Equal(expected, message);
    }

    [Fact]
    public void ResolveTranscriptSpeakingFailureMessage_WhenDisplayLanguageIsSupported_ReturnsModeledText()
    {
        var expected = ResolveResourceText(
            VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingRateLimited,
            "de");

        var message = _resolver.ResolveTranscriptSpeakingFailureMessage(SpeechErrorCode.ProviderRateLimited, "de");

        Assert.Equal(expected, message);
    }

    [Fact]
    public void ResolveTranscriptSpeakingFailureMessage_WhenDisplayLanguageIsUnsupported_ReturnsModeledDefaultLanguageText()
    {
        var expected = ResolveResourceText(
            VoiceInterfaceResourceIds.SpeechCaptureTranscriptSpeakingFailed,
            "en");

        var message = _resolver.ResolveTranscriptSpeakingFailureMessage((SpeechErrorCode)int.MaxValue, "pt");

        Assert.Equal(expected, message);
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }

    public static IEnumerable<object[]> AllKnownErrorCodes() =>
        Enum.GetValues<SpeechErrorCode>()
            .Select(errorCode => new object[] { errorCode });
}
