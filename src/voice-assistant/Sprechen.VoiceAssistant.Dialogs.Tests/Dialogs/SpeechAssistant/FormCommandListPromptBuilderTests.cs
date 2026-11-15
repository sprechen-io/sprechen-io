using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.VoiceCommands;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormCommandListPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_WhenNotTruncated_AnnouncesTotal_GroupHeader_AndLabels()
    {
        var groups = new (VoiceCommandCategory Category, IReadOnlyList<string> Labels)[]
        {
            (VoiceCommandCategory.Navigation, ["weiter", "zurück"]),
        };

        var text = FormCommandListPromptBuilder.BuildPrompt(
            contract: null,
            groups: groups,
            totalCount: 2,
            truncated: false,
            isoCode: "de",
            PolitenessLevel.Formal);

        Assert.Contains("2", text, StringComparison.Ordinal);
        Assert.Contains("Navigation", text, StringComparison.Ordinal);
        Assert.Contains("weiter", text, StringComparison.Ordinal);
        Assert.Contains("zurück", text, StringComparison.Ordinal);
        Assert.DoesNotContain("wichtigsten", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPrompt_WhenTruncated_AnnouncesFullTotalAndTruncatedPhrasing()
    {
        var groups = new (VoiceCommandCategory Category, IReadOnlyList<string> Labels)[]
        {
            (VoiceCommandCategory.Navigation, ["weiter", "zurück"]),
            (VoiceCommandCategory.Control, ["absenden"]),
        };

        var text = FormCommandListPromptBuilder.BuildPrompt(
            contract: null,
            groups: groups,
            totalCount: 10,
            truncated: true,
            isoCode: "de",
            PolitenessLevel.Formal);

        Assert.Contains("10", text, StringComparison.Ordinal);
        Assert.Contains("wichtigsten", text, StringComparison.Ordinal);
        Assert.Contains("Navigation", text, StringComparison.Ordinal);
        Assert.Contains("Steuerung", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPrompt_WhenNoCommands_AnnouncesNone()
    {
        var text = FormCommandListPromptBuilder.BuildPrompt(
            contract: null,
            groups: [],
            totalCount: 0,
            truncated: false,
            isoCode: "de",
            PolitenessLevel.Formal);

        Assert.Contains("keine", text, StringComparison.OrdinalIgnoreCase);
    }
}
