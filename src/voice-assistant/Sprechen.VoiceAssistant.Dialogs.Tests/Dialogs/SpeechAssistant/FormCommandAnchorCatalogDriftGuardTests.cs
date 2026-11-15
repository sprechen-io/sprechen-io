using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TextNormalization;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass der Befehlsparser die Aktivierungsformen aus <see cref="VoiceInterfaceModelCatalog"/>
/// als Präfix und alleinstehend akzeptiert, einschließlich kompakter Schreibweisen.
/// Zwei Gegenbeispiele prüfen die Sprachabgrenzung. Dies beweist weder die Abwesenheit
/// weiterer akzeptierter Formen noch den Verzicht auf Literale im Parserquelltext.
/// </summary>
public sealed class FormCommandAnchorCatalogDriftGuardTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void Detector_AcceptsExactlyTheCatalogEffectiveFormsForEachLanguage(string isoCode)
    {
        // Die Standardformen stammen unmittelbar aus dem Katalog, ohne Überschreibungen.
        var catalogForms = CatalogEffectiveForms(isoCode);
        Assert.NotEmpty(catalogForms);

        foreach (var form in catalogForms)
        {
            // Jede Katalogform muss vor einem Befehl als Aktivierungspräfix erkannt werden.
            Assert.True(
                FormGlobalCommandParser.HasCommandLeadIn($"{form} weiter", catalogForms),
                $"Expected '{form} weiter' to be a command lead-in for '{isoCode}'.");

            // Auch die alleinstehende Katalogform muss erkannt werden.
            Assert.True(
                FormGlobalCommandParser.IsBareCommandAnchor(form, catalogForms),
                $"Expected bare '{form}' to be a bare command anchor for '{isoCode}'.");

            // Dieselbe Form wird auch ohne Leerzeichen und diakritische Zeichen geprüft.
            var glued = TextNormalizer.NormalizeForCompactMatching(form);
            Assert.True(
                FormGlobalCommandParser.HasCommandLeadIn($"{glued} weiter", catalogForms),
                $"Expected glued '{glued} weiter' to be a command lead-in for '{isoCode}'.");
            Assert.True(
                FormGlobalCommandParser.IsBareCommandAnchor(glued, catalogForms),
                $"Expected glued bare '{glued}' to be a bare command anchor for '{isoCode}'.");
        }
    }

    [Fact]
    public void Detector_DoesNotAcceptAnotherLanguagesFormUnderThisLanguagesCatalogForms()
    {
        // Eine italienische Form darf hier nicht mit den deutschen Formen übereinstimmen und umgekehrt.
        var deForms = CatalogEffectiveForms("de");
        var itForms = CatalogEffectiveForms("it");

        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okay guida aiuto", deForms));
        Assert.False(FormGlobalCommandParser.HasCommandLeadIn("okay guide help", itForms));
    }

    private static string[] CatalogEffectiveForms(string isoCode)
        =>
        [
            .. VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage(isoCode),
            .. VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage(isoCode)
        ];
}
