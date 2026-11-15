using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft kurze Nachfragen als Varianten des Wiederholungsbefehls sowie ihre Abgrenzung von längeren Eingaben.
/// Die Tests betreffen den Parser; sie belegen keine Befehlsausführung oder tatsächliche Hörbarkeit.
/// </summary>
public sealed class FormVoiceCommandRepeatReflexTests
{
    [Theory]
    // Kurze Nachfragen in den fünf geprüften Sprachen.
    [InlineData("de", "was")]
    [InlineData("de", "wie bitte")]
    [InlineData("de", "häh")]
    [InlineData("de", "hä")]
    [InlineData("en", "what")]
    [InlineData("en", "pardon")]
    [InlineData("en", "come again")]
    [InlineData("en", "say again")]
    [InlineData("fr", "quoi")]
    [InlineData("fr", "comment")]
    [InlineData("fr", "pardon")]
    [InlineData("fr", "répétez")]
    [InlineData("es", "qué")]
    [InlineData("es", "cómo")]
    [InlineData("es", "perdón")]
    [InlineData("es", "repita")]
    [InlineData("it", "cosa")]
    [InlineData("it", "come")]
    [InlineData("it", "scusa")]
    [InlineData("it", "ripeti")]
    // Weitere modellierte Formen des Wiederholungsbefehls.
    [InlineData("de", "wiederholen")]
    [InlineData("de", "nochmal")]
    [InlineData("en", "repeat")]
    [InlineData("en", "again")]
    [InlineData("fr", "repeter")]
    [InlineData("es", "repetir")]
    [InlineData("it", "ancora")]
    public void RepeatReflex_ResolvesToRepeatCommand(string isoCode, string utterance)
    {
        var parsed = FormGlobalCommandParser.TryParse(
            CreateContract(),
            utterance,
            isoCode,
            PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed, $"reflex '{utterance}' ({isoCode}) did not parse to any command.");
        Assert.Equal(GlobalCommandIds.Repeat, commandId);
    }

    [Theory]
    [InlineData("de", "was?")]
    [InlineData("de", "Wie bitte?")]
    [InlineData("de", "Häh?!")]
    [InlineData("en", "What?")]
    [InlineData("fr", "Quoi ?")]
    [InlineData("es", "¿Qué?")]
    [InlineData("it", "Cosa?")]
    public void RepeatReflex_WithPunctuationOrCasing_NormalizesAndRoutesToRepeat(string isoCode, string transcript)
    {
        var parsed = FormGlobalCommandParser.TryParse(
            CreateContract(),
            transcript,
            isoCode,
            PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed, $"punctuated reflex '{transcript}' ({isoCode}) did not parse to any command.");
        Assert.Equal(GlobalCommandIds.Repeat, commandId);
    }

    [Theory]
    [InlineData("de", "was")]
    [InlineData("de", "wie bitte")]
    [InlineData("en", "what")]
    [InlineData("es", "qué")]
    public void BareReflex_IsRecognizedAsAnIsolatedRepeatSignal(string isoCode, string transcript)
    {
        // Geprüft wird nur die Erkennung als alleinstehendes Wiederholungssignal.
        // Ob der Dialog den Befehl ohne Aktivierungswort ausführt, entscheidet eine andere Komponente.
        var isolated = FormGlobalCommandParser.IsIsolatedCommandSignal(
            CreateContract(),
            transcript,
            GlobalCommandIds.Repeat,
            isoCode,
            PolitenessLevel.Formal);

        Assert.True(isolated, $"bare reflex '{transcript}' ({isoCode}) was not recognized as an isolated repeat signal.");
    }

    [Theory]
    // In diesen längeren Sätzen gilt das einleitende Fragewort nicht als alleinstehendes Wiederholungssignal.
    [InlineData("de", "was kostet das haus")]
    [InlineData("en", "what is your name please tell me")]
    [InlineData("es", "qué calle es esta")]
    public void EmbeddedReflexInFreeText_IsNotAnIsolatedCommandSignal_SoItStaysInput(string isoCode, string transcript)
    {
        var isolated = FormGlobalCommandParser.IsIsolatedCommandSignal(
            CreateContract(),
            transcript,
            GlobalCommandIds.Repeat,
            isoCode,
            PolitenessLevel.Formal);

        Assert.False(isolated, $"embedded '{transcript}' ({isoCode}) must not be treated as an isolated repeat signal.");
    }

    [Theory]
    // Die vollständigen Befehlsphrasen behalten ihre eigene Zuordnung trotz gleichem Anfangswort.
    [InlineData("de", "was soll ich eingeben", GlobalCommandIds.Help)]
    [InlineData("de", "was kann ich sagen", GlobalCommandIds.CommandList)]
    [InlineData("de", "was gibt es noch", GlobalCommandIds.FieldList)]
    [InlineData("de", "was habe ich eingegeben", GlobalCommandIds.Summary)]
    [InlineData("en", "what should i enter", GlobalCommandIds.Help)]
    [InlineData("en", "what can i say", GlobalCommandIds.CommandList)]
    public void MultiWordCommandStartingWithReflex_StillResolvesToItsOwnCommand(
        string isoCode,
        string transcript,
        string expectedCommandId)
    {
        var parsed = FormGlobalCommandParser.TryParse(
            CreateContract(),
            transcript,
            isoCode,
            PolitenessLevel.Formal,
            out var commandId,
            out _);

        Assert.True(parsed, $"'{transcript}' ({isoCode}) did not parse to any command.");
        Assert.Equal(expectedCommandId, commandId);
    }

    private static FormContract CreateContract()
        => new()
        {
            Projections = new ContractProjections
            {
                Voice = new VoiceProjection
                {
                    Hints = new VoiceHintsDefinition
                    {
                        CommandModelId = FormVoiceCommandModelIds.Default
                    }
                }
            }
        };
}
