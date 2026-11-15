using System.Security.Cryptography;
using System.Text;
using Deepgram.Models.Listen.v1.REST;
using Sprechen.Service.Abstractions;
using Sprechen.Service.Deepgram.Options;
using Sprechen.Service.Deepgram.Services;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class DeepgramPrerecordedSchemaFactoryTests
{
    [Fact]
    public void Create_WhenConversationalProfile_KeepsConversationalFormattingFlags()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Conversational).Schema;

        Assert.Equal("nova-3-general", schema.Model);
        Assert.True(schema.SmartFormat);
        Assert.True(schema.Punctuate);
        Assert.Null(schema.Numerals);
        Assert.Equal("de", schema.Language);
        Assert.False(schema.DetectLanguage);
    }

    [Fact]
    public void Create_WhenLanguageCodeIsMissing_EnablesDetectLanguage()
    {
        var factory = CreateFactory();

        var composition = factory.Compose(languageCode: null, RecognitionProfile.Conversational);
        PreRecordedSchema schema = composition.Schema;

        Assert.True(schema.DetectLanguage);
        Assert.True(string.IsNullOrWhiteSpace(schema.Language));
        Assert.False(composition.KeytermParameterPresent);
        Assert.False(composition.ConfirmationKeytermsApplied);
        Assert.Equal(0, composition.ConfirmationKeytermCount);
    }

    [Fact]
    public void Create_WhenSpellingProfile_DisablesSmartFormatPunctuateAndNumerals()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Spelling).Schema;

        Assert.False(schema.SmartFormat);
        Assert.False(schema.Punctuate);
        Assert.False(schema.Numerals);
    }

    [Fact]
    public void Create_WhenDigitsProfile_DisablesSmartFormatPunctuateButForcesNumerals()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Digits).Schema;

        Assert.False(schema.SmartFormat);
        Assert.False(schema.Punctuate);
        Assert.True(schema.Numerals);
    }

    [Fact]
    public void Create_WhenProfileIsUnknownValue_FallsBackToConversationalFlags()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", (RecognitionProfile)999).Schema;

        Assert.True(schema.SmartFormat);
        Assert.True(schema.Punctuate);
        Assert.Null(schema.Numerals);
    }

    [Fact]
    public void Create_WhenSpellingProfile_AttachesDerivedAlphabetKeyterms()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Spelling).Schema;

        Assert.NotNull(schema.Keyterm);
        Assert.Equal(SpellingKeytermVocabulary.Default.Keyterms, schema.Keyterm);
        Assert.Equal(88, schema.Keyterm!.Count);
        Assert.Equal(
            "C0455F3A8C91576252BD0131A3DBD864C3816CBB8BE4BD4EE41B1BAE5D920473",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", schema.Keyterm)))));
        Assert.Contains("anton", schema.Keyterm!);
    }

    [Fact]
    public void Create_WhenConversationalProfile_AttachesCommandAndWakeWordKeyterms()
    {
        var factory = CreateFactory();

        var composition = factory.Compose(languageCode: "de", RecognitionProfile.Conversational);
        PreRecordedSchema schema = composition.Schema;

        Assert.NotNull(schema.Keyterm);
        foreach (var commandKeyterm in CommandKeytermVocabulary.Default.GetKeyterms("de"))
        {
            Assert.Contains(commandKeyterm, schema.Keyterm!);
        }

        Assert.Contains("buchstabieren", schema.Keyterm!);
        Assert.Contains("alex", schema.Keyterm!);
    }

    [Fact]
    public void Create_WhenConversationalProfileNoLanguage_HasNoKeyterm()
    {
        var factory = CreateFactory();

        var composition = factory.Compose(languageCode: null, RecognitionProfile.Conversational);
        PreRecordedSchema schema = composition.Schema;

        Assert.Null(schema.Keyterm);
        Assert.False(composition.KeytermParameterPresent);
        Assert.False(composition.ConfirmationKeytermsApplied);
        Assert.False(composition.ContextKeytermsRequested);
        Assert.False(composition.ContextKeytermsApplied);
    }

    [Fact]
    public void Create_WhenAllConversationalKeytermsDisabled_HasNoKeyterm()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);

        var composition = factory.Compose(languageCode: "de", RecognitionProfile.Conversational);
        PreRecordedSchema schema = composition.Schema;

        Assert.Null(schema.Keyterm);
        Assert.False(composition.KeytermParameterPresent);
        Assert.Equal(0, composition.KeytermCount);
        Assert.False(composition.ConfirmationKeytermsApplied);
        Assert.Equal(0, composition.ConfirmationKeytermCount);
        Assert.False(composition.ContextKeytermsRequested);
        Assert.False(composition.ContextKeytermsApplied);
    }

    [Fact]
    public void Create_WhenKeytermsSupplied_UnionsThemWithConversationalKeyterms()
    {
        var factory = CreateFactory();
        var keyterms = new[] { "Einfaches Beispiel", "Komplexes Beispiel" };

        var composition = factory.Compose("de", RecognitionProfile.Conversational, new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));
        PreRecordedSchema schema = composition.Schema;

        Assert.NotNull(schema.Keyterm);
        Assert.Contains("Einfaches Beispiel", schema.Keyterm!);
        Assert.Contains("Komplexes Beispiel", schema.Keyterm!);
        Assert.Contains("ja", schema.Keyterm!);
        Assert.True(composition.KeytermParameterPresent);
        Assert.Equal(schema.Keyterm!.Count, composition.KeytermCount);
        Assert.True(composition.ConfirmationKeytermsApplied);
        Assert.Equal(
            ConfirmationKeytermVocabulary.Default.GetKeyterms("de").Count,
            composition.ConfirmationKeytermCount);
        Assert.True(composition.ContextKeytermsRequested);
        Assert.Equal(2, composition.ContextKeytermRequestedCount);
        Assert.True(composition.ContextKeytermsApplied);
        Assert.Equal(2, composition.ContextKeytermAppliedCount);
    }

    [Fact]
    public void Create_WhenKeytermsSuppliedWithoutConversationalBoost_AttachesOnlyKeyterms()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);
        var keyterms = new[] { "Einfaches Beispiel", "Komplexes Beispiel" };

        var composition = factory.Compose("de", RecognitionProfile.Conversational, new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));
        PreRecordedSchema schema = composition.Schema;

        Assert.Equal(keyterms, schema.Keyterm);
        Assert.Equal(2, composition.KeytermCount);
        Assert.False(composition.ConfirmationKeytermsApplied);
        Assert.Equal(0, composition.ConfirmationKeytermCount);
        Assert.True(composition.ContextKeytermsRequested);
        Assert.Equal(2, composition.ContextKeytermRequestedCount);
        Assert.True(composition.ContextKeytermsApplied);
        Assert.Equal(2, composition.ContextKeytermAppliedCount);
    }

    [Fact]
    public void Create_WhenKeytermsEmpty_LeavesSchemaUnchanged()
    {
        var factory = CreateFactory(enableCommandKeyterms: false, enableWakeWordKeyterms: false, enableConfirmationKeyterms: false);

        var composition = factory.Compose("de", RecognitionProfile.Conversational, keyterms: new SpeechKeyterms([], KeytermComposition.WithServerBase));
        PreRecordedSchema schema = composition.Schema;

        Assert.Null(schema.Keyterm);
        Assert.False(composition.KeytermParameterPresent);
        Assert.Equal(0, composition.KeytermCount);
        Assert.False(composition.ContextKeytermsRequested);
        Assert.Equal(0, composition.ContextKeytermRequestedCount);
        Assert.False(composition.ContextKeytermsApplied);
        Assert.Equal(0, composition.ContextKeytermAppliedCount);
    }

    [Fact]
    public void Compose_WhenSourcesOverlapAndContextRepeats_ReportsDistinctAppliedProvenanceFromFinalSchema()
    {
        // ja stammt sowohl aus dem Bestätigungsvokabular als auch aus den Begriffen der Anfrage.
        var confirmation = new ConfirmationKeytermVocabulary(
            new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal)
            {
                ["de"] = ["ja", "okay", "ja"]
            });
        var factory = CreateFactory(
            enableCommandKeyterms: false,
            enableWakeWordKeyterms: false,
            confirmationKeyterms: confirmation);
        var keyterms = new[] { "Private Alpha", "Private Alpha", "ja", "Private Beta" };

        var composition = factory.Compose("de", RecognitionProfile.Conversational, new SpeechKeyterms(keyterms, KeytermComposition.WithServerBase));

        // Herkunftszähler können sich überschneiden; die endgültige Begriffsliste enthält trotzdem keine Duplikate.
        Assert.Equal(["ja", "okay", "Private Alpha", "Private Beta"], composition.Schema.Keyterm);
        Assert.Equal(4, composition.KeytermCount);
        Assert.True(composition.ConfirmationKeytermsApplied);
        Assert.Equal(2, composition.ConfirmationKeytermCount);
        Assert.True(composition.ContextKeytermsRequested);
        Assert.Equal(3, composition.ContextKeytermRequestedCount);
        Assert.True(composition.ContextKeytermsApplied);
        Assert.Equal(3, composition.ContextKeytermAppliedCount);
    }

    [Fact]
    public void Compose_WhenContextContainsBlankTerm_RejectsBeforeProviderSchemaCreation()
    {
        var factory = CreateFactory();

        var exception = Assert.Throws<ArgumentException>(() =>
            factory.Compose("de", RecognitionProfile.Conversational, new SpeechKeyterms(["Valid", "   "], KeytermComposition.WithServerBase)));

        Assert.Contains("must not be blank", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WhenServerVocabularyExceedsProviderKeytermLimit_FailsClosed()
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
            factory.Compose("de", RecognitionProfile.Conversational));

        Assert.Contains("provider limit", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Compose_WhenConfirmationDisabledButContextContainsConfirmationToken_DoesNotMisattributeSource()
    {
        var factory = CreateFactory(
            enableCommandKeyterms: false,
            enableWakeWordKeyterms: false,
            enableConfirmationKeyterms: false);

        var composition = factory.Compose("de", RecognitionProfile.Conversational, new SpeechKeyterms(["ja"], KeytermComposition.WithServerBase));

        Assert.Equal(["ja"], composition.Schema.Keyterm);
        Assert.Equal(1, composition.KeytermCount);
        Assert.False(composition.ConfirmationKeytermsApplied);
        Assert.Equal(0, composition.ConfirmationKeytermCount);
        Assert.True(composition.ContextKeytermsRequested);
        Assert.Equal(1, composition.ContextKeytermRequestedCount);
        Assert.True(composition.ContextKeytermsApplied);
        Assert.Equal(1, composition.ContextKeytermAppliedCount);
    }

    [Fact]
    public void Compose_WithoutServerBase_BoostsTheTurnsTermsAndNothingElse()
    {
        var factory = CreateFactory();

        var composition = factory.Compose(
            "de",
            RecognitionProfile.Conversational,
            new SpeechKeyterms(["blau", "grün"], KeytermComposition.WithoutServerBase));

        // WithoutServerBase muss trotz aktivierter Servervokabulare ausschließlich die Anfragebegriffe übernehmen.
        Assert.Equal(["blau", "grün"], composition.Schema.Keyterm);
    }

    [Fact]
    public void Compose_WithoutServerBaseAndNoTerms_BoostsNothingAtAll()
    {
        var factory = CreateFactory();

        var composition = factory.Compose(
            "de",
            RecognitionProfile.Conversational,
            new SpeechKeyterms([], KeytermComposition.WithoutServerBase));

        // Eine leere Liste mit WithoutServerBase muss auch die Ergänzung des Servervokabulars verhindern.
        Assert.Null(composition.Schema.Keyterm);
        Assert.Equal(0, composition.KeytermCount);
    }

    [Fact]
    public void Create_WhenDigitsProfile_HasNoKeyterm()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Digits).Schema;

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void Create_WhenSpellingProfileAndEmptyVocabulary_HasNoKeyterm()
    {
        var factory = CreateFactory(new SpellingKeytermVocabulary([]));

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Spelling).Schema;

        Assert.Null(schema.Keyterm);
    }

    [Fact]
    public void Create_RequestsNoNBestAlternatives()
    {
        var factory = CreateFactory();

        PreRecordedSchema schema = factory.Compose(languageCode: "de", RecognitionProfile.Conversational).Schema;

        Assert.Null(schema.Alternatives);
    }

    private static DeepgramPrerecordedSchemaFactory CreateFactory(
        SpellingKeytermVocabulary? spellingKeyterms = null,
        CommandKeytermVocabulary? commandKeyterms = null,
        bool enableCommandKeyterms = true,
        bool enableWakeWordKeyterms = true,
        WakeWordKeytermVocabulary? wakeWordKeyterms = null,
        bool enableConfirmationKeyterms = true,
        ConfirmationKeytermVocabulary? confirmationKeyterms = null)
    {
        return new DeepgramPrerecordedSchemaFactory(
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
