using System.Text.Json;
using Sprechen.Mda.Contracts.V1.VoiceCommands;

namespace Sprechen.Mda.Contracts.Tests.Schema;

public sealed class VoiceCommandCategoryCatalogDriftTests
{
    [Fact]
    public void Catalog_RenderOrder_CoversEveryUsedCategory_WithoutDuplicates()
    {
        var renderOrder = VoiceCommandCategoryCatalog.RenderOrder;

        Assert.Equal(renderOrder.Count, renderOrder.Distinct().Count());

        var usedCategories = VoiceCommandCategoryCatalog.All.Values.ToHashSet();
        Assert.True(
            usedCategories.IsSubsetOf(renderOrder),
            "Every category that classifies a command must appear in RenderOrder.");
    }

    [Fact]
    public void Catalog_Categories_ArePartition_OverAllCommands()
    {
        var perCategoryTotal = Enum.GetValues<VoiceCommandCategory>()
            .Sum(category => VoiceCommandCategoryCatalog.All.Count(pair => pair.Value == category));

        Assert.Equal(VoiceCommandCategoryCatalog.All.Count, perCategoryTotal);
    }

    [Theory]
    [MemberData(nameof(ExpectedCategories))]
    public void Catalog_Category_MatchesExpected(string commandId, VoiceCommandCategory expected)
    {
        Assert.Equal(expected, VoiceCommandCategoryCatalog.CategoryOf(commandId));
    }

    public static IEnumerable<object[]> ExpectedCategories() =>
        new List<object[]>
        {
            new object[] { "next", VoiceCommandCategory.Navigation },
            new object[] { "back", VoiceCommandCategory.Navigation },
            new object[] { "jumpTo", VoiceCommandCategory.Navigation },
            new object[] { "nextError", VoiceCommandCategory.Navigation },
            new object[] { "nextOpenRequiredField", VoiceCommandCategory.Navigation },
            new object[] { "repeat", VoiceCommandCategory.Monitoring },
            new object[] { "status", VoiceCommandCategory.Monitoring },
            new object[] { "summary", VoiceCommandCategory.Monitoring },
            new object[] { "formStatus", VoiceCommandCategory.Monitoring },
            new object[] { "help", VoiceCommandCategory.Discovery },
            new object[] { "formList", VoiceCommandCategory.Discovery },
            new object[] { "fieldGroupList", VoiceCommandCategory.Discovery },
            new object[] { "fieldList", VoiceCommandCategory.Discovery },
            new object[] { "commandList", VoiceCommandCategory.Discovery },
            new object[] { "inputModeSpelling", VoiceCommandCategory.InputMode },
            new object[] { "inputModeVoice", VoiceCommandCategory.InputMode },
            new object[] { "inputModeDigits", VoiceCommandCategory.InputMode },
            new object[] { "inputModeKeyboard", VoiceCommandCategory.InputMode },
            new object[] { "inputModeLiteral", VoiceCommandCategory.InputMode },
            new object[] { "inputModeLiteralOff", VoiceCommandCategory.InputMode },
            new object[] { "digitsDeleteLastDigit", VoiceCommandCategory.InputMode },
            new object[] { "digitsDeleteLastBlock", VoiceCommandCategory.InputMode },
            new object[] { "digitsRepeatLastBlock", VoiceCommandCategory.InputMode },
            new object[] { "digitsRedictateLastBlock", VoiceCommandCategory.InputMode },
            new object[] { "submit", VoiceCommandCategory.Control },
            new object[] { "cancel", VoiceCommandCategory.Control },
            new object[] { "editValue", VoiceCommandCategory.Control },
            new object[] { "acceptValue", VoiceCommandCategory.Control },
            new object[] { "switchForm", VoiceCommandCategory.Control },
            new object[] { "backToFormSelection", VoiceCommandCategory.Control },
            new object[] { "backToAppSelection", VoiceCommandCategory.Control },
            new object[] { "changeLanguage", VoiceCommandCategory.Control },
            new object[] { "changePoliteness", VoiceCommandCategory.Control },
            new object[] { "openApplications", VoiceCommandCategory.Control },
            new object[] { "allowBargeInOn", VoiceCommandCategory.Control },
            new object[] { "allowBargeInOff", VoiceCommandCategory.Control },
        };
}
