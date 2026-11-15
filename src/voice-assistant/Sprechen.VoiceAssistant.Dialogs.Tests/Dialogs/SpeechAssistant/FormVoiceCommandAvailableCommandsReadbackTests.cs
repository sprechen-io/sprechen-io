using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.VoiceCommands;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAvailableCommandsReadbackTests
{
    [Fact]
    public void BuildReadback_GroupsByCategory_InRenderOrder_OmittingEmptyGroups()
    {
        var builder = new FormVoiceCommandAvailableCommandsPromptBuilder(
            (_, _, _) =>
            [
                ("submit", "absenden"),
                ("next", "weiter"),
                ("back", "zurück"),
            ]);

        var readback = builder.BuildReadback(CreateContract(), "de", PolitenessLevel.Formal, perGroupCap: 8);

        Assert.Equal(2, readback.Groups.Count);
        Assert.Equal(VoiceCommandCategory.Navigation, readback.Groups[0].Category);
        Assert.Equal(VoiceCommandCategory.Control, readback.Groups[1].Category);
        Assert.Equal(["weiter", "zurück"], readback.Groups[0].ShownLabels);
        Assert.Equal(["absenden"], readback.Groups[1].ShownLabels);
        Assert.Equal(3, readback.TotalCount);
        Assert.False(readback.Truncated);
    }

    [Fact]
    public void BuildReadback_WhenGroupExceedsCap_CapsPerGroupButReportsFullGroupTotal()
    {
        var builder = new FormVoiceCommandAvailableCommandsPromptBuilder(
            (_, _, _) =>
            [
                ("next", "a"),
                ("back", "b"),
                ("jumpTo", "c"),
                ("nextError", "d"),
                ("nextOpenRequiredField", "e"),
            ]);

        var readback = builder.BuildReadback(CreateContract(), "de", PolitenessLevel.Formal, perGroupCap: 2);

        var navigation = Assert.Single(readback.Groups);
        Assert.Equal(VoiceCommandCategory.Navigation, navigation.Category);
        Assert.Equal(2, navigation.ShownLabels.Count);
        Assert.Equal(["a", "b"], navigation.ShownLabels);
        Assert.Equal(5, navigation.GroupTotal);
        Assert.Equal(5, readback.TotalCount);
        Assert.True(readback.Truncated);
    }

    [Fact]
    public void BuildReadback_DeduplicatesLabels_AndSkipsEmpty()
    {
        var builder = new FormVoiceCommandAvailableCommandsPromptBuilder(
            (_, _, _) =>
            [
                ("next", "weiter"),
                ("back", ""),
                ("jumpTo", "   "),
                ("status", "Status"),
                ("nextError", "weiter"),
            ]);

        var readback = builder.BuildReadback(CreateContract(), "de", PolitenessLevel.Formal, perGroupCap: 8);

        Assert.Equal(2, readback.TotalCount);
        var navigation = readback.Groups.Single(group => group.Category == VoiceCommandCategory.Navigation);
        Assert.Equal(["weiter"], navigation.ShownLabels);
        var monitoring = readback.Groups.Single(group => group.Category == VoiceCommandCategory.Monitoring);
        Assert.Equal(["Status"], monitoring.ShownLabels);
    }

    [Fact]
    public void BuildReadback_WhenNoCommands_ReturnsNoGroups()
    {
        var builder = new FormVoiceCommandAvailableCommandsPromptBuilder((_, _, _) => []);

        var readback = builder.BuildReadback(CreateContract(), "de", PolitenessLevel.Formal, perGroupCap: 8);

        Assert.Empty(readback.Groups);
        Assert.Equal(0, readback.TotalCount);
        Assert.False(readback.Truncated);
    }

    private static FormContract CreateContract()
    {
        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true
        };
    }
}
