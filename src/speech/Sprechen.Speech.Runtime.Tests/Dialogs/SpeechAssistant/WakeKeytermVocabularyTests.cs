namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Aktivierungsformen, einzelne Namensteile und die Zusammensetzung der Erkennungshinweise.
/// </summary>
public sealed class WakeKeytermVocabularyTests
{
    [Fact]
    public void Build_FromTheModeledGermanInventory_BoostsTheFormsAndTheBareWakeWord()
    {
        var inventory = new WakeWordInventory(
            VoiceInterfaceModelCatalog.WakeWord.GetActivationFormsForLanguage("de"),
            VoiceInterfaceModelCatalog.WakeWord.GetRecognitionOnlyActivationFormsForLanguage("de"));

        var keyterms = new WakeKeytermVocabulary().Build(inventory);

        Assert.NotNull(keyterms);

        // Neben allen modellierten Formen muss der alleinstehende Namensteil als eigener Erkennungshinweis enthalten sein.
        foreach (var form in inventory.ActivationForms.Concat(inventory.RecognitionOnlyActivationForms))
        {
            Assert.Contains(form, keyterms.Terms, StringComparer.OrdinalIgnoreCase);
        }

        Assert.Contains("alex", keyterms.Terms, StringComparer.OrdinalIgnoreCase);

        // Der Aktivierungswortschatz bleibt von den allgemeinen Gesprächsbegriffen des Servers getrennt.
        Assert.Equal(KeytermComposition.WithoutServerBase, keyterms.Composition);

        // Der gemeinsame Name aus mehreren Aktivierungsformen darf nicht mehrfach enthalten sein.
        Assert.Equal(keyterms.Terms.Count, keyterms.Terms.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Build_WhenTheInventoryIsEmpty_ReturnsNoKeytermsRatherThanAnEmptyExpectation()
    {
        // Ein leeres Inventar liefert null statt einer leeren Überschreibung;
        // so bleibt die Entscheidung über den Serverwortschatz beim Aufrufer.
        var keyterms = new WakeKeytermVocabulary().Build(new WakeWordInventory([], []));

        Assert.Null(keyterms);
    }

    [Fact]
    public void Build_FromAnOverriddenSingleWordInventory_StillBoostsTheWordItself()
    {
        var keyterms = new WakeKeytermVocabulary().Build(new WakeWordInventory(["computer"], []));

        Assert.NotNull(keyterms);
        Assert.Equal(["computer"], keyterms.Terms);
    }
}
