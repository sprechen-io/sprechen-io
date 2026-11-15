using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass reine Aktivierungsformen am Sitzungseinstieg nicht als Antwort gelten.
/// Die Fälle berücksichtigen Satzzeichen, Großschreibung und Akzente.
/// </summary>
public sealed class SessionEntryActivationWordFilterTests
{
    [Theory]
    [InlineData("Okay, Alex.")]
    [InlineData("Okay alex")]
    [InlineData("okay alex.")]
    [InlineData("OKAY, ALEX!")]
    [InlineData("  Okay Alex  ")]
    // Auch nur zur Erkennung modellierte Formen und Akzentschreibweisen müssen ausgefiltert werden.
    [InlineData("Ok, Álex.")]
    [InlineData("ok alex")]
    [InlineData("OK ÁLEX")]
    [InlineData("Hey Alex")]
    public void AnUtteranceThatWasOnlyTheActivationWord_IsNoAnswer(string transcript)
    {
        Assert.Null(SessionEntryActivationWordFilter.Strip(transcript));
    }

    [Theory]
    [InlineData("Okay, Alex. Ja", "ja")]
    [InlineData("Okay Alex Italiano", "italiano")]
    [InlineData("ja", "ja")]
    public void AnAnswerSurvives_EvenBehindTheActivationWord(string transcript, string expected)
    {
        Assert.Equal(expected, SessionEntryActivationWordFilter.Strip(transcript));
    }

    [Fact]
    public void AWordThatMerelyContainsTheActivationWord_IsLeftAlone()
    {
        // Wortgrenzen verhindern, dass Teile einer eigentlichen Antwort entfernt werden.
        Assert.Equal("okays", SessionEntryActivationWordFilter.Strip("okays"));
    }

    /// <summary>
    /// Erzeugt für alle modellierten Sprachen und Aktivierungsformen Varianten
    /// mit Großschreibung, Satzzeichen und ersetzten Akzenten.
    /// </summary>
    public static TheoryData<string, string> EveryFormInEverySupportedLanguage
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
            {
                var forms = VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode)
                    .Concat(VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage(isoCode))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var form in forms)
                {
                    foreach (var variant in TranscriptVariantsOf(form))
                    {
                        data.Add(isoCode, variant);
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(EveryFormInEverySupportedLanguage))]
    public void InEverySupportedLanguage_TheBareActivationWord_IsNoAnswer(string isoCode, string transcript)
    {
        // Die Sprachkennung ordnet einen Fehler der zugehörigen Modellvariante zu.
        Assert.True(
            SessionEntryActivationWordFilter.Strip(transcript) is null,
            $"'{transcript}' would be read as an answer in the {isoCode} session");
    }

    private static IEnumerable<string> TranscriptVariantsOf(string form)
    {
        yield return form;
        yield return form.ToUpperInvariant();
        yield return form + ".";

        var words = form.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 1)
        {
            yield return string.Join(' ', words.Take(1)) + ", " + string.Join(' ', words.Skip(1)) + ".";
        }

        // Zusätzliche Akzente prüfen die Normalisierung unabhängig von der Schreibweise im Modell.
        var accented = form.Replace("a", "á", StringComparison.Ordinal);
        if (!string.Equals(accented, form, StringComparison.Ordinal))
        {
            yield return accented;
        }
    }

    [Fact]
    public void TheRecognitionOnlyForms_AreCoveredToo()
    {
        // Auch ausschließlich für die Erkennung modellierte Formen gehören zu den zu filternden Transkripten.
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            foreach (var form in VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage(isoCode))
            {
                Assert.Null(SessionEntryActivationWordFilter.Strip(form));
            }
        }
    }

    [Fact]
    public void TheActivationFormsComeFromTheModel_NotFromAConstantHere()
    {
        // Die erwarteten Formen werden aus dem Modell gelesen und folgen dadurch dessen Änderungen.
        var forms = VoiceInterfaceModelCatalog.WakeWord.GetAllActivationForms();

        Assert.NotEmpty(forms);
        Assert.All(forms, form => Assert.Null(SessionEntryActivationWordFilter.Strip(form)));
    }
}
