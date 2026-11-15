using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Konfidenzklassen und ihre Schwellen für erfasste Werte.
/// Bei gelockerter Untergrenze wird Uncertain statt Insufficient geliefert;
/// fehlende Konfidenz bleibt Uncertain.
/// </summary>
public sealed class SttConfidenceGateTests
{
    [Fact]
    public void Evaluate_WithStrictDefault_KeepsTheThreeBandReject()
    {
        var context = SpellingWorkflowTestFactory.CreateContext("de");
        var accept = CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault;
        var confirm = CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault;

        // Die explizit strenge Einstellung muss die vorgesehenen Klassen liefern.
        // Der zusätzliche Aufruf ohne Schalter prüft den Standardfall bei Konfidenz 50.
        AssertBand(SttConfidenceLevel.Sufficient, context, accept, relax: false);
        AssertBand(SttConfidenceLevel.Uncertain, context, accept - 1, relax: false);
        AssertBand(SttConfidenceLevel.Uncertain, context, confirm, relax: false);
        AssertBand(SttConfidenceLevel.Insufficient, context, confirm - 1, relax: false);
        AssertBand(SttConfidenceLevel.Insufficient, context, 50, relax: false);
        AssertBand(SttConfidenceLevel.Insufficient, context, 0, relax: false);

        Assert.Equal(SttConfidenceLevel.Insufficient, SttConfidenceGate.Evaluate(context, 50));
    }

    [Fact]
    public void Evaluate_WhenRelaxed_MapsBelowConfirmToAcceptWithReadback()
    {
        var context = SpellingWorkflowTestFactory.CreateContext("de");
        var accept = CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault;
        var confirm = CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault;

        // Die obere Annahmeschwelle bleibt unverändert; die gelockerte Untergrenze ergibt Uncertain.
        AssertBand(SttConfidenceLevel.Sufficient, context, accept, relax: true);
        AssertBand(SttConfidenceLevel.Uncertain, context, accept - 1, relax: true);
        AssertBand(SttConfidenceLevel.Uncertain, context, confirm, relax: true);
        AssertBand(SttConfidenceLevel.Uncertain, context, confirm - 1, relax: true);
        AssertBand(SttConfidenceLevel.Uncertain, context, 50, relax: true);
        AssertBand(SttConfidenceLevel.Uncertain, context, 0, relax: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Evaluate_WhenNoConfidenceReported_StaysUncertainRegardlessOfRelaxation(bool relax)
    {
        var context = SpellingWorkflowTestFactory.CreateContext("de");

        Assert.Equal(
            SttConfidenceLevel.Uncertain,
            SttConfidenceGate.Evaluate(context, sttConfidencePercent: null, relaxBelowConfirmFloor: relax));
    }

    private static void AssertBand(
        SttConfidenceLevel expected,
        FormVoiceCommandContext context,
        int confidence,
        bool relax)
        => Assert.Equal(expected, SttConfidenceGate.Evaluate(context, confidence, relaxBelowConfirmFloor: relax));
}
