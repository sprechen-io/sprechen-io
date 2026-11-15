using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class LanguageConfirmationConversationStateTests
{
    [Fact]
    public void BuildResult_WhenLanguageLocked_UsesLockedLanguageAndTranscript()
    {
        var state = CreateState(requestedLanguageCode: "en");
        state.Apply(new SpeechConversationTranscriptFinalizedEvent("Hallo", null, null));
        state.Apply(new SpeechConversationLanguageLockedEvent("de", 1));

        state.EnsureTranscriptPresent();
        var result = state.BuildResult();

        Assert.Equal("Hallo", result.Transcript);
        Assert.Equal("de", result.Language.IsoCode);
        Assert.Equal("Deutsch", result.Language.DisplayName);
        var candidate = Assert.Single(result.Language.Candidates);
        Assert.Equal("de", candidate.IsoCode);
        Assert.Equal(1.0f, candidate.Probability);
    }

    [Fact]
    public void BuildResult_WhenOnlyDetectedLanguagesExist_UsesPrimaryCandidate()
    {
        var state = CreateState(requestedLanguageCode: null);
        state.Apply(new SpeechConversationEndOfTurnEvent(1, "bonjour"));
        state.Apply(new SpeechConversationLanguageDetectedEvent(new[] { "fr", "en" }, 1));

        state.EnsureTranscriptPresent();
        var result = state.BuildResult();

        Assert.Equal("bonjour", result.Transcript);
        Assert.Equal("fr", result.Language.IsoCode);
        Assert.Equal("Francais", result.Language.DisplayName);
        Assert.Collection(
            result.Language.Candidates,
            first =>
            {
                Assert.Equal("fr", first.IsoCode);
                Assert.Equal(0.0f, first.Probability);
            },
            second =>
            {
                Assert.Equal("en", second.IsoCode);
                Assert.Equal(0.0f, second.Probability);
            });
    }

    [Fact]
    public void BuildResult_WhenOnlyRequestedLanguageExists_UsesRequestedLanguageFallback()
    {
        var state = CreateState(requestedLanguageCode: "en");
        state.Apply(new SpeechConversationTranscriptUpdateEvent("hello", null, null));

        state.EnsureTranscriptPresent();
        var result = state.BuildResult();

        Assert.Equal("en", result.Language.IsoCode);
        Assert.Equal("English", result.Language.DisplayName);
        var candidate = Assert.Single(result.Language.Candidates);
        Assert.Equal("en", candidate.IsoCode);
        Assert.Equal(1.0f, candidate.Probability);
    }

    [Fact]
    public void EnsureTranscriptPresent_WhenTranscriptIsMissing_ThrowsNoSpeechDetectedException()
    {
        var state = CreateState(requestedLanguageCode: "de");

        Assert.Throws<NoSpeechDetectedException>(state.EnsureTranscriptPresent);
    }

    private static LanguageConfirmationConversationState CreateState(string? requestedLanguageCode)
    {
        return new LanguageConfirmationConversationState(
            new SupportedLanguages("de", new[]
            {
                new SupportedLanguage("de", "Deutsch"),
                new SupportedLanguage("en", "English"),
                new SupportedLanguage("fr", "Francais")
            }),
            requestedLanguageCode);
    }
}
