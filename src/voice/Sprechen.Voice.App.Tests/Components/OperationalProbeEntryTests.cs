using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Voice.App.Tests.TestDoubles;
using Sprechen.VoiceAssistant.EvaluationTelemetry;
using Sprechen.Web.SpeechComponents;
using VoiceAssistantComponent = Sprechen.Web.SpeechComponents.Components.VoiceAssistant;

namespace Sprechen.Voice.App.Tests.Components;

public sealed class OperationalProbeEntryTests : BunitContext
{
    private readonly OperationalProbeSession _probe = new();
    private readonly StudyTelemetryContext _participant = new();
    private readonly Mock<IOperationalProbeBootstrapClient> _bootstrap = new();
    private readonly Mock<ISpeechAssistantSessionController> _controller = new();

    public OperationalProbeEntryTests()
    {
        Services.AddSingleton<IOperationalProbeSession>(_probe);
        Services.AddSingleton<IStudyTelemetryContext>(_participant);
        Services.AddSingleton(_bootstrap.Object);
        Services.AddSingleton(_controller.Object);
        Services.AddSingleton(Mock.Of<ISpeechCaptureSurfacePressController>());
        Services.AddSingleton(Mock.Of<IUserLanguageState>(state => state.SelectedIsoCode == "en"));
        Services.AddSingleton<IWakeWordProvider>(new FixedWakeWordProvider());
        Services.AddSingleton<IDiagnosticOperatingMode>(new RecordingDiagnosticOperatingMode());
        Services.AddSingleton<ILogger<VoiceAssistantComponent>>(NullLogger<VoiceAssistantComponent>.Instance);
        Services.AddSpeechInteractionSignals(new ClientMessageBus(NullLogger<ClientMessageBus>.Instance));
    }

    [Fact]
    public async Task PendingBootstrap_ExposesNoStartControlThenAdmitsWithoutParticipant()
    {
        var pending = new TaskCompletionSource<OperationalProbeContext>();
        _bootstrap.Setup(client => client.BootstrapAsync(It.IsAny<CancellationToken>())).Returns(pending.Task);
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.Single(cut.FindAll("[aria-busy='true']"));
        Assert.Null(_participant.ParticipantId);
        await cut.InvokeAsync(() => pending.SetResult(Context()));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.speech-surface")));
        Assert.Equal(Context(), _probe.Context);
        Assert.Null(_participant.ParticipantId);
        Assert.Empty(_controller.Invocations);
    }

    [Fact]
    public void FailedBootstrap_LeavesCaptureSuppressed()
    {
        _bootstrap.Setup(client => client.BootstrapAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("refused"));
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.True(_probe.IsRequested);
        Assert.Null(_probe.Context);
        Assert.Null(_participant.ParticipantId);
    }

    [Theory]
    [InlineData("/debug/umfrage/probe")]
    [InlineData("/umfrage/probe/")]
    [InlineData("/umfrage/probe/extra")]
    [InlineData("/UMFRAGE/probe")]
    public void ProbeAliases_DoNotBootstrapOrExposeControl(string path)
    {
        Navigate(path);
        var cut = Render<VoiceAssistantComponent>();
        Assert.Empty(cut.FindAll("button.speech-surface"));
        _bootstrap.Verify(client => client.BootstrapAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(_participant.ParticipantId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateBootstrap_AfterNavigationOrDispose_CannotAdmit(bool dispose)
    {
        var pending = new TaskCompletionSource<OperationalProbeContext>();
        _bootstrap.Setup(client => client.BootstrapAsync(It.IsAny<CancellationToken>())).Returns(pending.Task);
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        if (dispose) await cut.Instance.DisposeAsync();
        else await cut.InvokeAsync(() => Navigate("/umfrage/sh0rt"));
        await cut.InvokeAsync(() => pending.SetResult(Context()));
        Assert.Null(_probe.Context);
        Assert.False(_probe.IsRequested);
        Assert.Empty(cut.FindAll("button.speech-surface"));
    }

    [Fact]
    public async Task NavigationAway_RevokesBoundProbeAndStopsAssistant()
    {
        _bootstrap.Setup(client => client.BootstrapAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Context());
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        _probe.BindStudy(_probe.Context!, "study.umfrage", "hintergrund-und-setup");
        await cut.InvokeAsync(() => Navigate("/"));
        Assert.Null(_probe.Context);
        Assert.Null(_participant.ParticipantId);
        _controller.Verify(controller => controller.StopAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ReturningToProbe_PerformsFreshBootstrap()
    {
        _bootstrap.SetupSequence(client => client.BootstrapAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Context())
            .ReturnsAsync(new OperationalProbeContext("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "B"));
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        await cut.InvokeAsync(() => Navigate("/"));
        await cut.InvokeAsync(() => Navigate("/umfrage/probe"));
        cut.WaitForAssertion(() => Assert.Equal("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", _probe.Context?.ProbeId));
        _bootstrap.Verify(client => client.BootstrapAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StopFailure_DoesNotPreventTheNewEntryAfterProbeRevocation()
    {
        _bootstrap.Setup(client => client.BootstrapAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Context());
        _controller.Setup(controller => controller.StopAsync(CancellationToken.None))
            .Returns(() => new ValueTask(Task.FromException(new InvalidOperationException("teardown"))));
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        await cut.InvokeAsync(() => Navigate("/"));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.speech-surface")));
        Assert.Null(_probe.Context);
        Assert.False(_probe.IsRequested);
        Assert.Empty(cut.FindAll("[role='alert']"));
    }

    [Fact]
    public async Task NormalNavigation_DoesNotAcquireProbeLifecycleOrStopTheAssistant()
    {
        Navigate("/");
        var cut = Render<VoiceAssistantComponent>();
        await cut.InvokeAsync(() => Navigate("/?lang=de"));
        _controller.Verify(controller => controller.StopAsync(It.IsAny<CancellationToken>()), Times.Never);
        _bootstrap.Verify(client => client.BootstrapAsync(It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotEmpty(cut.FindAll("button.speech-surface"));
    }

    [Fact]
    public async Task FormerBootstrapCompletion_DoesNotReleaseNewPendingEntry()
    {
        var first = new TaskCompletionSource<OperationalProbeContext>();
        var second = new TaskCompletionSource<OperationalProbeContext>();
        _bootstrap.SetupSequence(client => client.BootstrapAsync(It.IsAny<CancellationToken>()))
            .Returns(first.Task).Returns(second.Task);
        Navigate("/umfrage/probe");
        var cut = Render<VoiceAssistantComponent>();
        await cut.InvokeAsync(() => Navigate("/"));
        await cut.InvokeAsync(() => Navigate("/umfrage/probe"));
        await cut.InvokeAsync(() => first.SetResult(Context()));
        Assert.Empty(cut.FindAll("button.speech-surface"));
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.Single(cut.FindAll("[aria-busy='true']"));
        await cut.InvokeAsync(() => second.SetResult(new OperationalProbeContext("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "B")));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button.speech-surface")));
        Assert.Equal("probe-aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", _probe.Context?.ProbeId);
    }

    private void Navigate(string path) => Services.GetRequiredService<NavigationManager>().NavigateTo(path);

    private static OperationalProbeContext Context() => new("probe-0123456789abcdef0123456789abcdef", "B");
}
