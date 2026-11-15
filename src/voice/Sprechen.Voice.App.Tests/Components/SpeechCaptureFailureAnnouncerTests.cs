using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.App.Contracts.Dialogs.VoiceInterface;
using Sprechen.Web.SpeechComponents;
using Sprechen.Voice.App.Tests.TestDoubles;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class SpeechCaptureFailureAnnouncerTests : TestContext
{
    [Fact]
    public void RendersAssertiveAlertLiveRegion()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();

        var alert = cut.Find("[role=alert]");
        Assert.Equal("assertive", alert.GetAttribute("aria-live"));
    }

    [Fact]
    public async Task OnFailed_WhenPermissionDenied_RendersLocalizedPermissionMessageWithRecoveryHint()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.MicrophonePermissionDenied, "NotAllowedError"));

        var expected = ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied, "de");
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role=alert]");
            Assert.Equal(expected, alert.TextContent.Trim());
        });

        Assert.Contains("Browsereinstellungen", expected, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnFailed_WhenMicrophoneUnavailable_RendersLocalizedUnavailableMessage()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.MicrophoneUnavailable, "NotFoundError"));

        var expected = ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureFailureMicrophoneUnavailable, "de");
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role=alert]");
            Assert.Equal(expected, alert.TextContent.Trim());
            Assert.Contains("versuchen Sie es erneut", alert.TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("automatisch", alert.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task OnFailed_WhenUnspecified_RendersGenericMessageDistinctFromPermission()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.Unspecified, "InvalidOperationException: gRPC unreachable"));

        var expectedGeneric = ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureFailureGeneric, "de");
        var permission = ResolveResourceText(VoiceInterfaceResourceIds.SpeechCaptureFailurePermissionDenied, "de");
        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role=alert]");
            Assert.Equal(expectedGeneric, alert.TextContent.Trim());
        });

        Assert.NotEqual(permission, expectedGeneric);
    }

    [Fact]
    public async Task OnFailed_DoesNotRenderTheNonLocalizedDiagnosticMessage()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.MicrophonePermissionDenied, "NotAllowedError: raw diagnostic"));

        cut.WaitForAssertion(() =>
        {
            var alert = cut.Find("[role=alert]");
            Assert.DoesNotContain("raw diagnostic", alert.TextContent, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NotAllowedError", alert.TextContent, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Dispose_UnsubscribesFromFailures()
    {
        var messageBus = new ClientMessageBus(NullLogger<ClientMessageBus>.Instance);
        Services.AddSpeechInteractionSignals(messageBus);
        RegisterLanguageState("de");

        var cut = Render<SpeechCaptureFailureAnnouncer>();
        cut.Instance.Dispose();

        await messageBus.PublishAsync(new SpeechCaptureFailed(SpeechErrorCode.MicrophonePermissionDenied, "NotAllowedError"));

        var alert = cut.Find("[role=alert]");
        Assert.Equal(string.Empty, alert.TextContent.Trim());
    }

    private void RegisterLanguageState(string isoCode)
    {
        var state = new UserLanguageState();
        state.SetSelectedIsoCode(isoCode);
        Services.AddSingleton<IUserLanguageState>(state);
    }

    private static string ResolveResourceText(string resourceId, string isoCode)
    {
        Assert.True(VoiceInterfaceModelCatalog.Resources.TryGetResourceText(resourceId, isoCode, out var text));
        return text;
    }
}
