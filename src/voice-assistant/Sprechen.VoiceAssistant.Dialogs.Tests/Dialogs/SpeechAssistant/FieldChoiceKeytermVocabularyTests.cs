using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FieldChoiceKeytermVocabularyTests
{
    // Die Optionsnamen sind vor dieser Projektion lokalisiert worden.
    // Die Funktion übernimmt ihre Schreibweise und Reihenfolge als Erkennungshinweise.
    // Der Test verwendet vorgegebene Namen und misst keine akustische Erkennungswirkung.
    [Theory]
    [InlineData("Blau", "Rot", "Grün")]     // de
    [InlineData("Blue", "Red", "Green")]    // en
    [InlineData("Bleu", "Rouge", "Vert")]   // fr
    [InlineData("Azul", "Rojo", "Verde")]   // es
    [InlineData("Blu", "Rosso", "Verde")]   // it
    public void GetKeyterms_ProjectsOptionDisplayNames_InOrder_ForEachLanguage(string first, string second, string third)
    {
        var vocabulary = new FieldChoiceKeytermVocabulary();
        var options = new[]
        {
            new FormChoiceOption("c1", first),
            new FormChoiceOption("c2", second),
            new FormChoiceOption("c3", third)
        };

        var keyterms = vocabulary.GetKeyterms(options);

        Assert.Equal(new[] { first, second, third }, keyterms);
    }

    [Fact]
    public void GetKeyterms_SkipsBlankDisplayNames_AndDeduplicates_PreservingOrder()
    {
        var vocabulary = new FieldChoiceKeytermVocabulary();
        var options = new[]
        {
            new FormChoiceOption("c1", "Blau"),
            new FormChoiceOption("c2", "   "),
            new FormChoiceOption("c3", "Rot"),
            new FormChoiceOption("c4", "Blau")
        };

        var keyterms = vocabulary.GetKeyterms(options);

        Assert.Equal(new[] { "Blau", "Rot" }, keyterms);
    }

    // Eine leere Optionsliste liefert keine Erkennungshinweise.
    // Der Test prüft diese Projektion, keine allgemeine Datenschutzgarantie für alle Feldtypen.
    [Fact]
    public void GetKeyterms_WhenNoOptions_ReturnsEmpty()
    {
        var vocabulary = new FieldChoiceKeytermVocabulary();

        var keyterms = vocabulary.GetKeyterms(Array.Empty<FormChoiceOption>());

        Assert.Empty(keyterms);
    }
}
