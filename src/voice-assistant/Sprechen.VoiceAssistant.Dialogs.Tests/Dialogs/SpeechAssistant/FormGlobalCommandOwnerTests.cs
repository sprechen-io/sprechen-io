using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.VoiceCommands;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormGlobalCommandOwnerTests
{
    [Fact]
    public void CandidateMatcher_WhenArgumentHasPoliteSuffix_TrimsArgument()
    {
        var matched = FormGlobalCommandCandidateMatcher.TryMatchWithOptionalArgument(
            GlobalCommandIds.SwitchForm,
            normalizedTranscript: "wechsle zu teil 2 bitte",
            normalizedUtterance: "wechsle zu",
            new FormGlobalCommandParserOptions(),
            out var commandId,
            out var argument,
            out var score);

        Assert.True(matched);
        Assert.Equal(GlobalCommandIds.SwitchForm, commandId);
        Assert.Equal("teil 2", argument);
        Assert.True(score > 0d);
    }

    [Fact]
    public void IsolationMatcher_WhenOnlyPolitePaddingSurroundsUtterance_ReturnsTrue()
    {
        var matched = FormGlobalCommandIsolationMatcher.IsUtteranceWithOnlyPolitePadding(
            "bitte wiederholen",
            "wiederholen",
            new HashSet<string>(StringComparer.Ordinal)
            {
                "bitte"
            });

        Assert.True(matched);
    }

    [Fact]
    public void PrefixStripper_WhenLocalizedPrefixMatches_StripsPrefix()
    {
        var prefixes = new[]
        {
            new ResolvedGlobalCommandPrefixDefinition(
                VoiceCommandPrefixKinds.Command,
                [
                    new LocalizedText
                    {
                        ["de"] = "Befehl"
                    }
                ],
                ["de"],
                "de",
                strictLocalization: false)
        };

        var stripped = FormGlobalCommandPrefixStripper.TryStripPrefix(
            "Befehl weiter",
            prefixes,
            "de",
            PolitenessLevel.Formal,
            out var strippedTranscript);

        Assert.True(stripped);
        Assert.Equal("weiter", strippedTranscript);
    }
}
