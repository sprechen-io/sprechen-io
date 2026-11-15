using Bunit;
using Moq;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Voice.App.Pages;

namespace Sprechen.Voice.App.Tests.Pages;

public sealed class LanguageTesterSpeechToTextTabTests : TestContext
{
    [Fact]
    public void ToggleAsync_WhenStartedAndStopped_StreamsTranscriptsAndSendsLanguageCodeOnce()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(
            new SupportedLanguage("de", "Deutsch"),
            new SupportedLanguage("en", "English"));

        var session = new Mock<ISpeechToTextStreamingSession>(MockBehavior.Strict);
        session
            .SetupGet(service => service.Events)
            .Returns(CreateEvents(
                new SpeechConversationTranscriptUpdateEvent("first", null, null),
                new SpeechConversationTranscriptFinalizedEvent("second", null, null)));
        session
            .Setup(service => service.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        session
            .Setup(service => service.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Stop", cut.Find("button.btn").TextContent, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Start", cut.Find("button.btn").TextContent, StringComparison.Ordinal));
        cut.WaitForAssertion(() => Assert.Contains("second", cut.Markup, StringComparison.Ordinal));

        client.Verify(service => service.StartAsync("de", It.IsAny<CancellationToken>()), Times.Once);
        session.Verify(service => service.StopAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ToggleAsync_WhenStartFails_ShowsError()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(new SupportedLanguage("de", "Deutsch"));

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() => Assert.Contains("boom", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void OnInitializedAsync_WhenLanguageLoadTimesOut_ShowsErrorAndFallbackLanguage()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("timeout"));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Loading supported languages timed out.", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Using: de", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void OnInitializedAsync_WhenLanguageLoadFails_ShowsErrorAndFallbackLanguage()
    {
        var supportedLanguagesClient = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        supportedLanguagesClient
            .Setup(client => client.GetAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(Mock.Of<ISpeechToTextStreamingClient>());

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Loading supported languages failed: boom", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Using: de", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ToggleAsync_WhenStopIsCancelled_DoesNotShowError()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(new SupportedLanguage("de", "Deutsch"));

        var session = new Mock<ISpeechToTextStreamingSession>(MockBehavior.Strict);
        session
            .SetupGet(service => service.Events)
            .Returns(CreateEvents());
        session
            .Setup(service => service.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("cancelled"));
        session
            .Setup(service => service.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Stop", cut.Find("button.btn").TextContent, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Start", cut.Find("button.btn").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("alert-danger", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ToggleAsync_WhenStopFails_ShowsError()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(new SupportedLanguage("de", "Deutsch"));

        var session = new Mock<ISpeechToTextStreamingSession>(MockBehavior.Strict);
        session
            .SetupGet(service => service.Events)
            .Returns(CreateEvents());
        session
            .Setup(service => service.StopAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("send failed"));
        session
            .Setup(service => service.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Stop", cut.Find("button.btn").TextContent, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("send failed", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public void ToggleAsync_WhenServerReturnsNoTranscript_CompletesWithoutError()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(new SupportedLanguage("de", "Deutsch"));

        var session = new Mock<ISpeechToTextStreamingSession>(MockBehavior.Strict);
        session
            .SetupGet(service => service.Events)
            .Returns(CreateEvents());
        session
            .Setup(service => service.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        session
            .Setup(service => service.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Stop", cut.Find("button.btn").TextContent, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Start", cut.Find("button.btn").TextContent, StringComparison.Ordinal);
            Assert.DoesNotContain("alert-danger", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("no speech", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void ToggleAsync_WhenMultipleTurnsArrive_AccumulatesTranscriptAcrossTurns()
    {
        var supportedLanguagesClient = CreateSupportedLanguagesClient(new SupportedLanguage("de", "Deutsch"));

        var session = new Mock<ISpeechToTextStreamingSession>(MockBehavior.Strict);
        session
            .SetupGet(service => service.Events)
            .Returns(CreateEvents(
                new SpeechConversationTranscriptUpdateEvent("hallo", null, 0),
                new SpeechConversationTranscriptFinalizedEvent("hallo welt", null, 0),
                new SpeechConversationEndOfTurnEvent(0, "hallo welt"),
                new SpeechConversationTranscriptUpdateEvent("wie", null, 1),
                new SpeechConversationTranscriptFinalizedEvent("wie geht es dir", null, 1),
                new SpeechConversationEndOfTurnEvent(1, "wie geht es dir")));
        session
            .Setup(service => service.StopAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        session
            .Setup(service => service.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        var client = new Mock<ISpeechToTextStreamingClient>(MockBehavior.Strict);
        client
            .Setup(service => service.StartAsync("de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session.Object);

        Services.AddSingleton(supportedLanguagesClient.Object);
        Services.AddSingleton(client.Object);

        var cut = Render<LanguageTesterSpeechToTextTab>();

        cut.WaitForAssertion(() => Assert.Contains("Using:", cut.Markup, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Stop", cut.Find("button.btn").TextContent, StringComparison.Ordinal));

        cut.Find("button.btn").Click();
        cut.WaitForAssertion(() => Assert.Contains("Start", cut.Find("button.btn").TextContent, StringComparison.Ordinal));
        cut.WaitForAssertion(() => Assert.Contains("hallo welt wie geht es dir", cut.Markup, StringComparison.Ordinal));
    }

    private static Mock<ISupportedLanguagesClient> CreateSupportedLanguagesClient(params SupportedLanguage[] languages)
    {
        var client = new Mock<ISupportedLanguagesClient>(MockBehavior.Strict);
        client
            .Setup(service => service.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupportedLanguages("de", languages));

        return client;
    }

    private static async IAsyncEnumerable<SpeechConversationEvent> CreateEvents(params SpeechConversationEvent[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}
