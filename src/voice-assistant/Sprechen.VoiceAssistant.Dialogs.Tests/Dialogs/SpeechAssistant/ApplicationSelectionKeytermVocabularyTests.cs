using Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class ApplicationSelectionKeytermVocabularyTests
{
    // Beide ähnlichen Beispieltitel müssen in den Erkennungshinweisen enthalten sein.
    // Der Test prüft die Liste für die vorgegebenen Namen in fünf Sprachen,
    // nicht die Erkennungsqualität oder den aktuell ausgelieferten Katalog.
    [Theory]
    [InlineData("Einfaches Beispiel", "Komplexes Beispiel")]   // de
    [InlineData("Simple example", "Complex example")]          // en
    [InlineData("Exemple simple", "Exemple complexe")]         // fr
    [InlineData("Ejemplo sencillo", "Ejemplo complejo")]       // es
    [InlineData("Esempio semplice", "Esempio complesso")]      // it
    public void GetKeyterms_ContainsBothExampleApplicationNames_ForEachLanguage(string simpleName, string complexName)
    {
        var vocabulary = new ApplicationSelectionKeytermVocabulary();
        var applications = new[]
        {
            new ApplicationInfo("sample.simple", simpleName, "desc", Array.Empty<string>()),
            new ApplicationInfo("sample.complex", complexName, "desc", Array.Empty<string>())
        };

        var keyterms = vocabulary.GetKeyterms(applications);

        Assert.Equal(new[] { simpleName, complexName }, keyterms);
    }

    [Fact]
    public void GetKeyterms_SkipsBlankNames_AndDeduplicates()
    {
        var vocabulary = new ApplicationSelectionKeytermVocabulary();
        var applications = new[]
        {
            new ApplicationInfo("a", "Einfaches Beispiel", null, Array.Empty<string>()),
            new ApplicationInfo("b", "   ", null, Array.Empty<string>()),
            new ApplicationInfo("c", "Einfaches Beispiel", null, Array.Empty<string>())
        };

        var keyterms = vocabulary.GetKeyterms(applications);

        Assert.Equal(new[] { "Einfaches Beispiel" }, keyterms);
    }

    [Fact]
    public void GetKeyterms_WhenNoApplications_ReturnsEmpty()
    {
        var vocabulary = new ApplicationSelectionKeytermVocabulary();

        var keyterms = vocabulary.GetKeyterms(Array.Empty<ApplicationInfo>());

        Assert.Empty(keyterms);
    }
}
