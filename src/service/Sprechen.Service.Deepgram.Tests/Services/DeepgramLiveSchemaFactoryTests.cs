using System.Security.Cryptography;
using System.Text;
using Deepgram.Models.Listen.v2.WebSocket;
using Sprechen.Service.Abstractions;
using Sprechen.Service.Deepgram.Options;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramLiveSchemaFactoryTests
{
    [Fact]
    public void CreateLowLatency_WhenLanguageCodeIsMissing_EnablesDetectLanguageExtra()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: null, RecognitionProfile.Conversational);

        Assert.Equal("nova-3-general", schema.Model);
        Assert.True(schema.SmartFormat);
        Assert.True(schema.Punctuate);
        Assert.Null(schema.Numerals);
        Assert.True(schema.InterimResults);
        Assert.True(schema.NoDelay);
        Assert.Equal("linear16", schema.Encoding);
        Assert.Equal(16_000, schema.SampleRate);
        Assert.Equal(1, schema.Channels);
        Assert.NotNull(schema.Extra);
        Assert.Equal("true", schema.Extra!["detect_language"]);
        Assert.True(string.IsNullOrWhiteSpace(schema.Language));
    }

    [Fact]
    public void CreateLowLatency_WhenLanguageCodeIsProvided_DisablesDetectLanguageExtra()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: " de ", RecognitionProfile.Conversational);

        Assert.Equal("de", schema.Language);
        Assert.Null(schema.Extra);
    }

    [Fact]
    public void CreateLowLatency_WhenSpellingProfile_DisablesSmartFormatPunctuateAndNumerals()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Spelling);

        Assert.False(schema.SmartFormat);
        Assert.False(schema.Punctuate);
        Assert.False(schema.Numerals);
    }

    [Fact]
    public void CreateLowLatency_WhenDigitsProfile_DisablesSmartFormatPunctuateButForcesNumerals()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Digits);

        Assert.False(schema.SmartFormat);
        Assert.False(schema.Punctuate);
        Assert.True(schema.Numerals);
    }

    [Fact]
    public void CreateLowLatency_WhenConversationalProfile_KeepsConversationalFormattingFlags()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational);

        Assert.True(schema.SmartFormat);
        Assert.True(schema.Punctuate);
        Assert.Null(schema.Numerals);
    }

    [Fact]
    public void CreateLowLatency_WhenProfileIsUnknownValue_FallsBackToConversationalFlags()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", (RecognitionProfile)999);

        Assert.True(schema.SmartFormat);
        Assert.True(schema.Punctuate);
        Assert.Null(schema.Numerals);
    }

    [Fact]
    public void CreateLowLatency_WhenSpellingProfile_AttachesDerivedAlphabetKeyterms()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Spelling);

        Assert.NotNull(schema.Keyterm);
        Assert.Equal(SpellingKeytermVocabulary.Default.Keyterms, schema.Keyterm);
        Assert.Equal(88, schema.Keyterm!.Count);
        Assert.Equal(
            "C0455F3A8C91576252BD0131A3DBD864C3816CBB8BE4BD4EE41B1BAE5D920473",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", schema.Keyterm)))));
        Assert.Contains("anton", schema.Keyterm!);
        Assert.Contains("berta", schema.Keyterm!);
        Assert.Contains("alpha", schema.Keyterm!);
    }

    [Fact]
    public void CreateLowLatency_WhenConversationalProfile_AttachesCommandAndWakeWordKeyterms()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational);

        Assert.NotNull(schema.Keyterm);
        var expected = ConversationalKeytermSet.Compose(
            new DeepgramOptions { EnableCommandKeyterms = true, EnableWakeWordKeyterms = true, EnableConfirmationKeyterms = true },
            CommandKeytermVocabulary.Default,
            WakeWordKeytermVocabulary.Default,
            ConfirmationKeytermVocabulary.Default,
            "de");
        Assert.Equal(expected, schema.Keyterm);
        Assert.Contains("buchstabieren", schema.Keyterm!);
        Assert.Contains("alex", schema.Keyterm!);
        Assert.Contains("okay", schema.Keyterm!);
        Assert.Contains("hey", schema.Keyterm!);
        Assert.Contains("ja", schema.Keyterm!);
        Assert.Contains("nein", schema.Keyterm!);
    }

    [Fact]
    public void CreateLowLatency_WhenCommandKeytermsDisabledButWakeWordEnabled_AttachesOnlyWakeWordKeyterms()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: true, enableConfirmationKeyterms: false);

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational);

        // Aktivierungsbegriffe bleiben enthalten, obwohl Befehlsbegriffe deaktiviert sind.
        Assert.NotNull(schema.Keyterm);
        Assert.Equal(WakeWordKeytermVocabulary.Default.GetKeyterms("de"), schema.Keyterm);
        Assert.Contains("alex", schema.Keyterm!);
        Assert.DoesNotContain("buchstabieren", schema.Keyterm!);
    }

    [Fact]
    public void CreateLowLatency_WhenConversationalProfileNoLanguage_HasNoKeyterm()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: null, RecognitionProfile.Conversational);

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenAllConversationalKeytermsDisabled_HasNoKeyterm()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational);

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenDigitsProfile_HasNoKeyterm()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Digits);

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenSpellingProfileAndEmptyVocabulary_HasNoKeyterm()
    {
        var factory = CreateFactory(new SpellingKeytermVocabulary([]));

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Spelling);

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenKeytermsSupplied_UnionsThemWithConversationalKeyterms()
    {
        var factory = CreateFactory();
        var keyterms = new[] { "Einfaches Beispiel", "Komplexes Beispiel" };

        LiveSchema schema = factory.CreateLowLatency(
            AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational, new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));

        Assert.NotNull(schema.Keyterm);
        Assert.Contains("Einfaches Beispiel", schema.Keyterm!);
        Assert.Contains("Komplexes Beispiel", schema.Keyterm!);
        // Die zusätzlichen Begriffe ersetzen die aktivierten Serverbegriffe in diesem Modus nicht.
        Assert.Contains("ja", schema.Keyterm!);
        Assert.Contains("alex", schema.Keyterm!);
    }

    [Fact]
    public void CreateLowLatency_WhenKeytermsSuppliedWithoutConversationalBoost_AttachesOnlyKeyterms()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);
        var keyterms = new[] { "Einfaches Beispiel", "Komplexes Beispiel" };

        LiveSchema schema = factory.CreateLowLatency(
            AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational, new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));

        Assert.Equal(keyterms, schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenKeytermsEmpty_LeavesSchemaUnchanged()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);

        LiveSchema schema = factory.CreateLowLatency(
            AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational, keyterms: new SpeechKeyterms([], KeytermComposition.WithServerBase));

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_WhenKeytermDuplicatesAConversationalKeyterm_IsNotDuplicated()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(
            AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational, keyterms: new SpeechKeyterms(["ja"], KeytermComposition.WithServerBase));

        Assert.NotNull(schema.Keyterm);
        Assert.Equal(1, schema.Keyterm!.Count(term => term == "ja"));
    }

    [Fact]
    public void CreateLowLatency_WhenContextTermHasBoundaryWhitespace_SendsNormalizedTerm()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);

        var schema = factory.CreateLowLatency(
            AudioMimeType.Pcm,
            languageCode: "de",
            RecognitionProfile.Conversational,
            keyterms: new SpeechKeyterms(["  Einfaches Beispiel  "], KeytermComposition.WithServerBase));

        Assert.Equal(["Einfaches Beispiel"], schema.Keyterm);
    }

    [Fact]
    public void CreateLowLatency_RequestsNoNBestAlternatives()
    {
        var factory = CreateFactory();

        LiveSchema schema = factory.CreateLowLatency(AudioMimeType.Pcm, languageCode: "de", RecognitionProfile.Conversational);

        Assert.Null(schema.Alternatives);
    }

    [Fact]
    public void CreateLowLatency_WhenMimeIsNotPcm_Throws()
    {
        var factory = CreateFactory();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.CreateLowLatency(AudioMimeType.Wav, languageCode: null, RecognitionProfile.Conversational));

        Assert.Contains("requires Pcm audio", exception.Message);
    }

    [Fact]
    public void CreateLowLatency_WhenServerVocabularyExceedsProviderKeytermLimit_FailsClosed()
    {
        var commands = new CommandKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
            {
                ["de"] = Enumerable.Range(0, DeepgramKeytermRequestPolicy.MaximumCount + 1)
                    .Select(index => $"command-{index}")
            });
        var factory = CreateFactory(
            commandKeyterms: commands,
            enableWakeWordKeyterms: false,
            enableConfirmationKeyterms: false);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            factory.CreateLowLatency(
                AudioMimeType.Pcm,
                "de",
                RecognitionProfile.Conversational));

        Assert.Contains("provider limit", exception.Message, StringComparison.Ordinal);
    }

    private static DeepgramLiveSchemaFactory CreateFactory(
        SpellingKeytermVocabulary? spellingKeyterms = null,
        CommandKeytermVocabulary? commandKeyterms = null,
        bool enableCommandKeyterms = true,
        bool enableWakeWordKeyterms = true,
        WakeWordKeytermVocabulary? wakeWordKeyterms = null,
        bool enableConfirmationKeyterms = true,
        ConfirmationKeytermVocabulary? confirmationKeyterms = null)
    {
        return new DeepgramLiveSchemaFactory(
            new DeepgramOptions
            {
                ApiKey = "k",
                Endpoint = "https://api.eu.deepgram.com/",
                SttModel = "nova-3-general",
                EnableCommandKeyterms = enableCommandKeyterms,
                EnableWakeWordKeyterms = enableWakeWordKeyterms,
                EnableConfirmationKeyterms = enableConfirmationKeyterms
            },
            spellingKeyterms,
            commandKeyterms,
            wakeWordKeyterms,
            confirmationKeyterms);
    }
}
