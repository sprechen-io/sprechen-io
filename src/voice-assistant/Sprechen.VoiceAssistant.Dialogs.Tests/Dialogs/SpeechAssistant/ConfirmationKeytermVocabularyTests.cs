using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;
using Xunit;
using Xunit.Abstractions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class ConfirmationKeytermVocabularyTests
{
    private readonly ITestOutputHelper _output;

    public ConfirmationKeytermVocabularyTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Die Bestätigung enthält auch das Abschlusswort ohne Aktivierungswort neben den Antwortverben.
    // Die Anzahl wird je Sprache ausgegeben und auf weniger als 40 begrenzt.
    [Theory]
    [InlineData("de", "fertig", "übernehmen")]
    [InlineData("en", "done", "accept")]
    [InlineData("fr", "terminer", "accepter")]
    [InlineData("es", "terminar", "aceptar")]
    [InlineData("it", "fine", "accettare")]
    public void EverySupportedLanguage_BoostsTheDoneWordAnchorFree_AndReportsItsCount(
        string isoCode,
        string doneWord,
        string acceptWord)
    {
        var keyterms = new ConfirmationKeytermVocabulary().GetKeyterms(
            new FormContract(),
            isoCode,
            PolitenessLevel.Formal);

        _output.WriteLine($"confirmation keyterms {isoCode}: {keyterms.Count}");
        Assert.Contains(doneWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(acceptWord, keyterms, StringComparer.OrdinalIgnoreCase);
        Assert.True(keyterms.Count < 40, $"The '{isoCode}' confirmation vocabulary must stay small ({keyterms.Count}).");
    }

    [Fact]
    public void YesNoTokens_German_AreThereToBeBoosted()
    {
        // Das kleine Bestätigungsvokabular braucht die modellierten deutschen Ja-/Nein-Wörter.
        // Der Test prüft deren Verfügbarkeit, nicht die Erkennungsqualität.
        var yesNo = VoiceInterfaceModelCatalog.Lexicon.YesNo;

        Assert.Contains("ja", yesNo.GetYesTokens("de"));
        Assert.Contains("nein", yesNo.GetNoTokens("de"));
    }

    [Fact]
    public void YesNoTokens_AreScopedByLanguage()
    {
        var yesNo = VoiceInterfaceModelCatalog.Lexicon.YesNo;

        Assert.Contains("oui", yesNo.GetYesTokens("fr"));
        Assert.DoesNotContain("oui", yesNo.GetYesTokens("de"));
    }
}
