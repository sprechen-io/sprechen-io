using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Vorschau auf den nächsten Hilfeschritt anhand des feldbezogenen
/// Fehlerzählers und der Wiederholungsgrenze. Deckt ausgeschaltete Hilfe
/// sowie Feldwechsel ab; eine echte Sprachwiedergabe wird nicht geprüft.
/// </summary>
public sealed class FormVoiceCommandAutoHelpTrackerIsHelpDueTests
{
    [Fact]
    public async Task IsHelpDueOnNextFailure_TrueExactlyBeforeTheHelpTurn()
    {
        var tracker = CreateTracker(maxRetries: 3);

        Assert.False(tracker.IsHelpDueOnNextFailure("name"));
        await tracker.RegisterFailureAsync("name", CancellationToken.None);
        Assert.False(tracker.IsHelpDueOnNextFailure("name"));
        await tracker.RegisterFailureAsync("name", CancellationToken.None);
        Assert.True(tracker.IsHelpDueOnNextFailure("name"));
        await tracker.RegisterFailureAsync("name", CancellationToken.None);
        // Nach dem Hilfeschritt beginnt der Zähler erneut.
        Assert.False(tracker.IsHelpDueOnNextFailure("name"));
    }

    [Fact]
    public async Task IsHelpDueOnNextFailure_AnotherField_StartsAtZero()
    {
        var tracker = CreateTracker(maxRetries: 2);
        await tracker.RegisterFailureAsync("name", CancellationToken.None);

        Assert.True(tracker.IsHelpDueOnNextFailure("name"));
        Assert.False(tracker.IsHelpDueOnNextFailure("alter"));
    }

    [Fact]
    public void IsHelpDueOnNextFailure_WithAutoHelpOff_IsAlwaysFalse()
    {
        var tracker = CreateTracker(maxRetries: 0);

        Assert.False(tracker.IsHelpDueOnNextFailure("name"));
    }

    [Fact]
    public void IsHelpDueOnNextFailure_WithMaxRetriesOne_IsTrueFromTheStart()
    {
        var tracker = CreateTracker(maxRetries: 1);

        Assert.True(tracker.IsHelpDueOnNextFailure("name"));
    }

    private static FormVoiceCommandAutoHelpTracker CreateTracker(int maxRetries)
        => new(
            new VoiceDialogSettings { MaxRetries = maxRetries },
            new Mock<IFormVoiceCommandPromptService>(MockBehavior.Loose).Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            new Mock<IFormDialogRuntime>(MockBehavior.Loose).Object,
            "de");
}
