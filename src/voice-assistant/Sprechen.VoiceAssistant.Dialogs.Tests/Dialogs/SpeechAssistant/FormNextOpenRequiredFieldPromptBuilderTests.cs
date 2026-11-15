using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormNextOpenRequiredFieldPromptBuilderTests
{
    [Fact]
    public void BuildPrompt_Moved_AnnouncesFieldLabelAndRemainingCount()
    {
        var text = Build(FocusNextOpenRequiredFieldResultKind.Moved, "Geburtsdatum", remaining: 3);

        Assert.Contains("Geburtsdatum", text);
        Assert.Contains("3", text);
    }

    [Fact]
    public void BuildPrompt_Wrapped_AnnouncesWrapAndFieldLabelAndCount()
    {
        var text = Build(FocusNextOpenRequiredFieldResultKind.WrappedToStart, "Vorname", remaining: 2);

        Assert.Contains("Zurück zum Anfang", text);
        Assert.Contains("Vorname", text);
        Assert.Contains("2", text);
    }

    [Fact]
    public void BuildPrompt_AlreadyOnSoleTarget_AnnouncesSoleField()
    {
        var text = Build(FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget, "Email", remaining: 1);

        Assert.Contains("Das einzige offene Pflichtfeld", text);
        Assert.Contains("Email", text);
    }

    [Fact]
    public void BuildPrompt_NoOpenRequiredField_AnnouncesNoneWithoutField()
    {
        var text = Build(FocusNextOpenRequiredFieldResultKind.NoOpenRequiredField, fieldLabel: null, remaining: 0);

        Assert.Contains("keine relevanten Pflichtfelder", text);
    }

    private static string Build(FocusNextOpenRequiredFieldResultKind kind, string? fieldLabel, int remaining)
        => FormNextOpenRequiredFieldPromptBuilder.BuildPrompt(
            contract: null,
            new FocusNextOpenRequiredFieldResult(kind, fieldLabel, remaining),
            fieldLabel,
            isoCode: "de",
            PolitenessLevel.Formal);
}
