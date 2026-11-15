using Sprechen.Service.Deepgram;
using Sprechen.Service.Deepgram.Services;
using Sprechen.Service.Abstractions;
using Xunit;

namespace Sprechen.Service.Deepgram.Tests.Services;

public sealed class ConversationalKeytermSetTests
{
    private static DeepgramOptions Options(bool command = true, bool wakeWord = true, bool confirmation = true) => new()
    {
        ApiKey = "k",
        Endpoint = "https://api.eu.deepgram.com/",
        SttModel = "nova-3-general",
        EnableCommandKeyterms = command,
        EnableWakeWordKeyterms = wakeWord,
        EnableConfirmationKeyterms = confirmation
    };

    private static CommandKeytermVocabulary Commands(params string[] terms) =>
        new(new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal) { ["de"] = terms });

    private static WakeWordKeytermVocabulary WakeWord(string[] leadIns, string name) =>
        new(leadIns, new Dictionary<string, string>(StringComparer.Ordinal) { ["de"] = name });

    private static ConfirmationKeytermVocabulary Confirmation(params string[] terms) =>
        new(new Dictionary<string, IEnumerable<string>>(StringComparer.Ordinal) { ["de"] = terms });

    [Fact]
    public void Compose_UnionIsCommandThenWakeWordThenConfirmation_DeDuplicatedOrderPreserving()
    {
        // Mehrfach vorkommende Begriffe bleiben einmal erhalten: zuerst Befehle, dann neue Aktivierungs- und Bestätigungsbegriffe.
        var result = ConversationalKeytermSet.Compose(
            Options(),
            Commands("weiter", "okay"),
            WakeWord(["okay", "ok", "hey"], "alex"),
            Confirmation("ja", "nein", "okay"),
            "de");

        Assert.Equal(["weiter", "okay", "ok", "hey", "alex", "ja", "nein"], result);
    }

    [Fact]
    public void Compose_AlwaysBoostsConfirmationAnswers_SoShortAnswersSurviveTheCommandWakeBoost()
    {
        // Bei aktivierter Bestätigung müssen ja und nein in der zusammengestellten Liste enthalten sein.
        var result = ConversationalKeytermSet.Compose(
            Options(),
            Commands("weiter", "hilfe"),
            WakeWord(["okay", "ok", "hey"], "alex"),
            Confirmation("ja", "nein", "genau", "falsch"),
            "de");

        Assert.Contains("ja", result);
        Assert.Contains("nein", result);
    }

    [Fact]
    public void Compose_WhenOnlyConfirmationEnabled_ContainsOnlyConfirmationTerms()
    {
        var result = ConversationalKeytermSet.Compose(
            Options(command: false, wakeWord: false),
            Commands("weiter", "okay"),
            WakeWord(["okay", "ok", "hey"], "alex"),
            Confirmation("ja", "nein"),
            "de");

        Assert.Equal(["ja", "nein"], result);
    }

    [Fact]
    public void Compose_WhenCommandDisabled_OmitsCommandTerms_KeepsWakeAndConfirmation()
    {
        var result = ConversationalKeytermSet.Compose(
            Options(command: false),
            Commands("weiter", "hilfe"),
            WakeWord(["okay", "ok", "hey"], "alex"),
            Confirmation("ja", "nein"),
            "de");

        Assert.Equal(["okay", "ok", "hey", "alex", "ja", "nein"], result);
        Assert.DoesNotContain("weiter", result);
    }

    [Fact]
    public void Compose_WhenWakeWordDisabled_OmitsWakeTerms_KeepsCommandAndConfirmation()
    {
        var result = ConversationalKeytermSet.Compose(
            Options(wakeWord: false),
            Commands("weiter", "hilfe"),
            WakeWord(["okay", "ok", "hey"], "alex"),
            Confirmation("ja", "nein"),
            "de");

        Assert.Equal(["weiter", "hilfe", "ja", "nein"], result);
        Assert.DoesNotContain("alex", result);
    }

    [Fact]
    public void Compose_WhenAllDisabled_IsEmpty()
    {
        var result = ConversationalKeytermSet.Compose(
            Options(command: false, wakeWord: false, confirmation: false),
            Commands("weiter"),
            WakeWord(["okay"], "alex"),
            Confirmation("ja"),
            "de");

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void Compose_DefaultCatalog_ReservesAllCallerContextSlotsWithinProviderLimit(string languageCode)
    {
        var result = ConversationalKeytermSet.Compose(
            Options(),
            CommandKeytermVocabulary.Default,
            WakeWordKeytermVocabulary.Default,
            ConfirmationKeytermVocabulary.Default,
            languageCode);

        Assert.True(
            result.Count + SpeechKeytermPolicy.MaximumCount <= DeepgramKeytermRequestPolicy.MaximumCount,
            $"Language '{languageCode}' leaves fewer than {SpeechKeytermPolicy.MaximumCount} context slots.");
    }
}
