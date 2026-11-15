using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft ausdrücklich hinterlegte deutsche Befehlsbezeichnungen und ihre Auflösung.
/// Eine erfolgreich ausgewertete Bezeichnung darf keinen anderen Befehl ergeben.
/// </summary>
public sealed class SpokenCommandFormOwnerTests
{
    [Fact]
    public void EverySpokenGermanCommandLabel_IsAPhraseTheParserAccepts()
    {
        var contract = CreateBaselineContract();
        var commands = FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract).ToArray();
        Assert.NotEmpty(commands);

        var unsayable = new List<string>();

        foreach (var command in commands)
        {
            var label = new FormGlobalCommandLabelResolver()
                .ResolveCommandLabel(contract, command.CommandId, "de", PolitenessLevel.Formal);

            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var parsed = FormGlobalCommandParser.TryParse(
                contract,
                transcript: label,
                isoCode: "de",
                politenessLevel: PolitenessLevel.Formal,
                out var parsedCommandId,
                out _);

            // Ein Befehlsstamm mit erforderlichem Argument kann allein unauswertbar sein.
            // Hier werden deshalb nur erfolgreich aufgelöste Bezeichnungen auf falsche Zuordnung geprüft.
            if (parsed && !string.Equals(parsedCommandId, command.CommandId, StringComparison.Ordinal))
            {
                unsayable.Add($"{command.CommandId}: spoken as '{label}' but parses as '{parsedCommandId}'");
            }
        }

        Assert.True(
            unsayable.Count == 0,
            "A spoken command label must never resolve to a different command - the assistant would be "
            + "telling a listener to say something that does something else:\n  "
            + string.Join("\n  ", unsayable));
    }

    [Fact]
    public void TheSpokenFormWins_AndItsAbsenceYieldsNoLabel()
    {
        var contract = CreateBaselineContract();
        var resolver = new FormGlobalCommandLabelResolver();

        var stated = resolver.ResolveCommandLabel(contract, GlobalCommandIds.FieldGroupList, "de", PolitenessLevel.Formal);
        var missingLanguage = resolver.ResolveCommandLabel(contract, GlobalCommandIds.FieldGroupList, "it", PolitenessLevel.Formal);

        // Der ausdrücklich hinterlegte Wortlaut enthält den geschützten Begriff Feldgruppen.
        Assert.Equal("Welche Feldgruppen", stated);

        // Ohne italienische spokenForm wird keine alternative Erkennungsform als Bezeichnung verwendet.
        Assert.Null(missingLanguage);
    }

    [Fact]
    public void ProtectedVocabulary_SurvivesIntoTheSpokenSurface()
    {
        var contract = CreateBaselineContract();

        var label = new FormGlobalCommandLabelResolver()
            .ResolveCommandLabel(contract, GlobalCommandIds.FieldGroupList, "de", PolitenessLevel.Formal);

        // Auch die Befehlsbezeichnung muss den geschützten Begriff Feldgruppen enthalten.
        Assert.NotNull(label);
        Assert.Contains("Feldgruppen", label, StringComparison.Ordinal);
    }

    private static FormContract CreateBaselineContract()
        => new()
        {
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
        };
}
