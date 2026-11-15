using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Weitergabe der Ansageart an jeden ausgegebenen Abschnitt.
/// Ausgaben ohne ausdrücklich gesetzte Art müssen Prompt bleiben.
/// </summary>
public sealed class SelectionDialogPromptSpeakerAnnouncementKindTests
{
    [Theory]
    [InlineData(SpokenAnnouncementKind.NoInput)]
    [InlineData(SpokenAnnouncementKind.RecoverableFailure)]
    public async Task ADeclaredPrompt_ReachesThePlayerWithItsDeclaration(SpokenAnnouncementKind announcementKind)
    {
        var player = new RecordingTextToSpeechPlayer();
        var speaker = new SelectionDialogPromptSpeaker(player, NullLogger.Instance);
        var prompt = SelectionDialogPrompt
            .FromText("Ich habe nichts gehört.")
            .AsAnnouncement(announcementKind);

        await speaker.SpeakAsync(prompt, "de", new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65), CancellationToken.None);

        Assert.All(player.AnnouncementKinds, kind => Assert.Equal(announcementKind, kind));
        Assert.NotEmpty(player.AnnouncementKinds);
    }

    [Fact]
    public async Task EverySegmentOfAMultiPartPrompt_CarriesTheSameDeclaration()
    {
        // Einleitung, Listenelemente und Abschlusstext können getrennte Playeraufrufe sein;
        // jeder muss die Art der gesamten Ansage übernehmen.
        var player = new RecordingTextToSpeechPlayer();
        var speaker = new SelectionDialogPromptSpeaker(player, NullLogger.Instance);
        var prompt = SelectionDialogPrompt
            .FromSequence("Ich habe nichts gehört.", ["Nummer 1", "Nummer 2"], "Bitte noch einmal.")
            .AsAnnouncement(SpokenAnnouncementKind.NoInput);

        await speaker.SpeakAsync(prompt, "de", new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65), CancellationToken.None);

        Assert.True(player.AnnouncementKinds.Count > 1, "expected the prompt to be spoken in several parts");
        Assert.All(player.AnnouncementKinds, kind => Assert.Equal(SpokenAnnouncementKind.NoInput, kind));
    }

    [Fact]
    public async Task AnUndeclaredPrompt_StaysAnOrdinaryPrompt()
    {
        // Nicht jede Ausgabe darf pauschal als fehlende Eingabe oder behebbarer Fehler gelten.
        var player = new RecordingTextToSpeechPlayer();
        var speaker = new SelectionDialogPromptSpeaker(player, NullLogger.Instance);

        await speaker.SpeakAsync(SelectionDialogPrompt.FromText("Es gibt zwei Anwendungen."), "de", new SelectionDialogInteractionSettings(1.0d, 0, true, 85, 65), CancellationToken.None);

        Assert.All(player.AnnouncementKinds, kind => Assert.Equal(SpokenAnnouncementKind.Prompt, kind));
    }
}
