using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Hinweistexte von <see cref="FormDateComponentPromptBuilder"/> für ungültige Datumskomponenten.
/// Die Texte nennen die erwartete Komponente, ihren Wertebereich und die Abbruchmöglichkeit.
/// Unbekannte Werte für Komponente oder Fehlergrund müssen eine Ausnahme auslösen.
/// </summary>
public sealed class FormDateComponentPromptBuilderTests
{
    private const string DayNotRecognizedDe = "Das habe ich nicht verstanden. Der Tag ist eine Zahl zwischen 1 und 31.";
    private const string MonthNotRecognizedDe = "Das habe ich nicht verstanden. Der Monat ist eine Zahl zwischen 1 und 12.";
    private const string YearNotRecognizedDe = "Das habe ich nicht verstanden. Das Jahr ist eine vierstellige Zahl.";

    private const string DaySlotPromptDe = "Welcher Tag?";
    private const string MonthSlotPromptDe = "Welcher Monat?";
    private const string YearSlotPromptDe = "Welches Jahr? Bitte vierstellig.";
    private const string ExitCueDe = "Oder sagen Sie abbrechen.";

    [Theory]
    [InlineData(DateComponentSlot.Day, DayNotRecognizedDe, DaySlotPromptDe)]
    [InlineData(DateComponentSlot.Month, MonthNotRecognizedDe, MonthSlotPromptDe)]
    [InlineData(DateComponentSlot.Year, YearNotRecognizedDe, YearSlotPromptDe)]
    public void GetReaskPrompt_NotRecognized_StatesTheSlotExpectation(
        DateComponentSlot slot,
        string expectedReason,
        string expectedSlotPrompt)
    {
        var result = FormDateComponentPromptBuilder.GetReaskPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            slot,
            DateComponentReaskReason.NotRecognized);

        Assert.Equal($"{expectedReason} {expectedSlotPrompt} {ExitCueDe}", result);
    }

    [Fact] // Der Fehlergrund bleibt ohne persönliche Anrede; die Abbruchaufforderung verwendet hier die informelle Variante.
    public void GetReaskPrompt_NotRecognized_InformalRegister_ResolvesTheInformalExitCue()
    {
        var result = FormDateComponentPromptBuilder.GetReaskPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Informal,
            DateComponentSlot.Day,
            DateComponentReaskReason.NotRecognized);

        Assert.Equal($"{DayNotRecognizedDe} {DaySlotPromptDe} Oder sag abbrechen.", result);
    }

    [Fact]
    public void GetReaskPrompt_YearOutOfRange_RestatesTheFourDigitExpectation()
    {
        // Auch die vorsorglich unterstützte Kombination Jahr und OutOfRange muss
        // die erwartete vierstellige Jahresangabe nennen.
        var result = FormDateComponentPromptBuilder.GetReaskPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            DateComponentSlot.Year,
            DateComponentReaskReason.OutOfRange);

        Assert.Equal($"Bitte das ganze Jahr, vierstellig. {YearSlotPromptDe} {ExitCueDe}", result);
    }

    [Theory]
    [InlineData(DateComponentSlot.Day, "Der Tag muss zwischen 1 und 31 liegen.", DaySlotPromptDe)]
    [InlineData(DateComponentSlot.Month, "Der Monat muss zwischen 1 und 12 liegen.", MonthSlotPromptDe)]
    public void GetReaskPrompt_OutOfRange_KeepsTheRangeReasonPerSlot(
        DateComponentSlot slot,
        string expectedReason,
        string expectedSlotPrompt)
    {
        var result = FormDateComponentPromptBuilder.GetReaskPrompt(
            CreateContract(),
            "de",
            PolitenessLevel.Formal,
            slot,
            DateComponentReaskReason.OutOfRange);

        Assert.Equal($"{expectedReason} {expectedSlotPrompt} {ExitCueDe}", result);
    }

    [Theory]
    [InlineData(DateComponentReaskReason.NotRecognized)]
    [InlineData(DateComponentReaskReason.OutOfRange)]
    public void GetReaskPrompt_UndefinedSlot_FailsFast(DateComponentReaskReason reason)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FormDateComponentPromptBuilder.GetReaskPrompt(
                CreateContract(),
                "de",
                PolitenessLevel.Formal,
                (DateComponentSlot)99,
                reason));
    }

    [Fact]
    public void GetReaskPrompt_UndefinedReason_FailsFast()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FormDateComponentPromptBuilder.GetReaskPrompt(
                CreateContract(),
                "de",
                PolitenessLevel.Formal,
                DateComponentSlot.Day,
                (DateComponentReaskReason)99));
    }

    [Fact]
    public void GetSlotPrompt_UndefinedSlot_FailsFast()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FormDateComponentPromptBuilder.GetSlotPrompt(
                CreateContract(),
                "de",
                PolitenessLevel.Formal,
                (DateComponentSlot)99));
    }

    private static FormContract CreateContract()
        => new()
        {
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de"
        };
}
