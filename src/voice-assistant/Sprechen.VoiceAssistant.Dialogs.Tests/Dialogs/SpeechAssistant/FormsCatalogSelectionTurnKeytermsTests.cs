using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormsCatalogSelectionTurnKeytermsTests
{
    private static FormsCatalogSelectionTurnKeyterms Create(params string[] candidates)
        => new(
            FormsCatalogIntentTestCatalog.Create(),
            "de",
            PolitenessLevel.Formal,
            candidates);

    [Fact]
    public void ForConfirming_NamesFewerTermsThanListening()
    {
        var keyterms = Create("Einfaches Beispiel", "Komplexes Beispiel");

        var listening = keyterms.ForListening();
        var confirming = keyterms.ForConfirming(SelectionConfirmationCaptureStage.Initial);

        // Bei der Auswahl gehören die Anwendungsnamen zu den Erkennungshinweisen;
        // bei der ersten Bestätigung wird eine kleinere Menge benötigt.
        Assert.NotNull(listening);
        Assert.NotNull(confirming);
        Assert.True(
            confirming!.Terms.Count < listening!.Terms.Count,
            $"confirming {confirming.Terms.Count} should be smaller than listening {listening.Terms.Count}");
    }

    [Fact]
    public void ForConfirming_Initial_DoesNotOfferTheCandidateName()
    {
        var keyterms = Create("Einfaches Beispiel");

        var initial = keyterms.ForConfirming(SelectionConfirmationCaptureStage.Initial);
        var authorization = keyterms.ForConfirming(SelectionConfirmationCaptureStage.ExplicitAuthorization);

        // Der Anwendungsname gehört erst bei der ausdrücklichen Freigabe zu den
        // Erkennungshinweisen, weil er in der ersten Bestätigungsstufe nicht als Antwort gilt.
        Assert.DoesNotContain("Einfaches Beispiel", initial!.Terms);
        Assert.Contains("Einfaches Beispiel", authorization!.Terms);
    }

    [Fact]
    public void EveryTurn_AsksForItsTermsAlone()
    {
        var keyterms = Create("Einfaches Beispiel");

        Assert.Equal(KeytermComposition.WithoutServerBase, keyterms.ForListening()!.Composition);
        Assert.Equal(
            KeytermComposition.WithoutServerBase,
            keyterms.ForConfirming(SelectionConfirmationCaptureStage.Initial)!.Composition);
    }

    [Fact]
    public void ForConfirming_ResolvesSomethingForEveryIntentItClaimsToHear()
    {
        // Die Bestätigungsabsichten werden im Code ausgewählt. Wenn ihre modellierten
        // Texte fehlen, darf daraus keine vollständig leere Hinweismenge entstehen.
        var confirming = Create().ForConfirming(SelectionConfirmationCaptureStage.Initial);

        Assert.NotNull(confirming);
        Assert.NotEmpty(confirming!.Terms);
    }
}
