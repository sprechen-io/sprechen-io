using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass leere Textkennungen beim Erzeugen der Auswahltexte abgewiesen werden
/// und ein fehlendes Auswahlelement eine Ausnahme auslöst.
/// </summary>
public sealed class SelectionCommonIntentPromptBuilderHardeningTests
{
    [Fact]
    public void Constructor_RejectsAnEmptyPromptId_AndNamesTheSlot()
    {
        var ids = FormSelectionPromptBuilder.CommonIntentPromptIds with { LowConfidenceConfirm = " " };

        var exception = Assert.Throws<ArgumentException>(
            () => new SelectionCommonIntentPromptBuilder<FormInfo>(ids, form => form.DisplayName));

        Assert.Equal(nameof(SelectionCommonIntentPromptIds.LowConfidenceConfirm), exception.ParamName);
    }

    [Fact]
    public void Constructor_AcceptsTheShippedIds()
    {
        var builder = new SelectionCommonIntentPromptBuilder<FormInfo>(
            FormSelectionPromptBuilder.CommonIntentPromptIds,
            form => form.DisplayName);

        Assert.NotNull(builder);
    }

    [Fact]
    public void GetConfirmationPrompt_ThrowsForAMissingItem_BeforeAnyPromptIsResolved()
    {
        var builder = new SelectionCommonIntentPromptBuilder<FormInfo>(
            FormSelectionPromptBuilder.CommonIntentPromptIds,
            form => form.DisplayName);

        // Der Textkontext bleibt absichtlich null, damit die Prüfung kein aufgelöstes Textmodell benötigt.
        Assert.Throws<ArgumentNullException>(
            () => builder.GetConfirmationPrompt(null!, item: null!, "de", PolitenessLevel.Formal));
    }
}
