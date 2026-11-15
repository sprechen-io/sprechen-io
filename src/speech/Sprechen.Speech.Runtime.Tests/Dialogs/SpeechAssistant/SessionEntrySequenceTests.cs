using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Aufrufreihenfolge beim Sitzungsstart in fünf aufgeführten Sprachen,
/// einschließlich Studienansage, Einführung, Aufnahmemodus und mehrfacher Ausgabe.
/// Das Ereignisprotokoll erfasst Aufnahme- und Sprachausgabeaufrufe, kein physisches Mikrofonende.
/// </summary>
public sealed class SessionEntrySequenceTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task NoAnswerWindow_IsOpenWhileTheWakeWordIsBeingTaught(string isoCode)
    {
        var harness = await SessionEntryStartHarness.StartWithAcceptedEntryAsync(isoCode);

        var wakeWordLines = WakeWordLines(isoCode);
        var firstWakeWordLine = IndexOfFirst(
            harness, entry => entry.Kind == "speak" && wakeWordLines.Contains(entry.Detail));
        Assert.True(firstWakeWordLine >= 0, "the start never taught the activation word");

        var lastWindow = IndexOfLast(harness, entry => entry.Kind == "record");
        Assert.True(lastWindow >= 0, "the start never opened an answer window");

        Assert.True(
            lastWindow < firstWakeWordLine,
            $"a window was opened at position {lastWindow}, after the wake word was first spoken at "
            + $"{firstWakeWordLine} — the microphone would hear the assistant say it");
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task AfterAnAcceptedEntry_OnlyTheOnboardingFollows(string isoCode)
    {
        var harness = await SessionEntryStartHarness.StartWithAcceptedEntryAsync(isoCode);

        var lastWindow = IndexOfLast(harness, entry => entry.Kind == "record");
        var afterTheWindow = harness.Events
            .Skip(lastWindow + 1)
            .Where(entry => entry.Kind == "speak")
            .Select(entry => entry.Detail)
            .ToArray();

        Assert.Equal(
            [
                FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Formal),
                FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Formal),
            ],
            afterTheWindow);
    }

    [Theory]
    [InlineData("de", "Umfrage")]
    [InlineData("en", "Survey")]
    [InlineData("fr", "Enquête")]
    [InlineData("es", "Encuesta")]
    [InlineData("it", "Sondaggio")]
    public async Task AfterAnAcceptedStudyEntry_AnnouncementThenOnboarding(
        string isoCode,
        string applicationName)
    {
        var harness = await SessionEntryStartHarness.StartWithAcceptedStudyEntryAsync(isoCode);

        var lastWindow = IndexOfLast(harness, entry => entry.Kind == "record");
        var afterTheWindow = harness.Events
            .Skip(lastWindow + 1)
            .Where(entry => entry.Kind == "speak")
            .Select(entry => entry.Detail)
            .ToArray();

        Assert.Equal(
            [
                FormDialogFlowPromptTextResolver.GetStudyEntryStudyAnnouncementPrompt(isoCode, applicationName),
                FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Formal),
                FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Formal),
            ],
            afterTheWindow);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task TheOnboarding_StartsImmediatelyAfterTheWindowCloses(string isoCode)
    {
        // Die Doubles antworten sofort. Gemessen wird vom Aufnahmeaufruf bis zur nächsten
        // Sprachausgabeanforderung; eine zusätzlich eingefügte Verzögerung ab 500 ms wird so sichtbar.
        var harness = await SessionEntryStartHarness.StartWithAcceptedEntryAsync(isoCode);

        var lastWindow = harness.Events.Last(entry => entry.Kind == "record");
        var firstAfter = harness.Events
            .SkipWhile(entry => entry.At <= lastWindow.At)
            .First(entry => entry.Kind == "speak");

        Assert.True(
            firstAfter.At - lastWindow.At < TimeSpan.FromMilliseconds(500),
            $"the onboarding started {(firstAfter.At - lastWindow.At).TotalMilliseconds:F0}ms after the "
            + "window closed; an accepted entry must run straight on");
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task TheOnboarding_IsSpokenOnce_AndInFull(string isoCode)
    {
        // Beide Einführungssätze müssen einmal angefordert werden, damit Aktivierung
        // und Wiederholung auch ohne Bildschirm erklärt werden.
        var harness = await SessionEntryStartHarness.StartWithAcceptedEntryAsync(isoCode);

        foreach (var line in WakeWordLines(isoCode))
        {
            Assert.Equal(1, harness.Spoken.Count(spoken => spoken == line));
        }
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task TheEntryWindow_IsOpenedAsAShortConfirmation(string isoCode)
    {
        var harness = await SessionEntryStartHarness.StartWithAcceptedEntryAsync(isoCode);

        var firstWindow = harness.Events.First(entry => entry.Kind == "record");
        Assert.Equal(nameof(SpeechCaptureMode.ShortConfirmation), firstWindow.Detail);
    }

    private static int IndexOfFirst(
        SessionEntryStartHarness harness,
        Func<(string Kind, string Detail, TimeSpan At), bool> predicate)
    {
        for (var index = 0; index < harness.Events.Count; index++)
        {
            if (predicate(harness.Events[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static int IndexOfLast(
        SessionEntryStartHarness harness,
        Func<(string Kind, string Detail, TimeSpan At), bool> predicate)
    {
        for (var index = harness.Events.Count - 1; index >= 0; index--)
        {
            if (predicate(harness.Events[index]))
            {
                return index;
            }
        }

        return -1;
    }

    private static string[] WakeWordLines(string isoCode)
        =>
        [
            FormDialogFlowPromptTextResolver.GetSessionWakeWordIntroPrompt(isoCode, PolitenessLevel.Formal),
            FormDialogFlowPromptTextResolver.GetSessionRepeatHintPrompt(isoCode, PolitenessLevel.Formal),
        ];
}
