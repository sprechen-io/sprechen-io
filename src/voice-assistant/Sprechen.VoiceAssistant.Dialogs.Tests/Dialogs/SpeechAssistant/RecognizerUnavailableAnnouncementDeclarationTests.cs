using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, dass der Hinweis auf eine nicht verfügbare Spracherkennung
/// als behebbarer Fehler gekennzeichnet und mit nichtleerem Text angefordert wird.
/// </summary>
public sealed class RecognizerUnavailableAnnouncementDeclarationTests
{
    [Fact]
    public async Task TheAnnouncement_DeclaresItselfARecoverableFailure()
    {
        var speechService = new RecordingSpeechService();
        var context = SpellingWorkflowTestFactory.CreateContext("de");

        await CharacterAccumulationRecognizerUnavailableAnnouncement.SpeakAsync(
            speechService,
            context,
            CancellationToken.None);

        Assert.Equal(
            [SpokenAnnouncementKind.RecoverableFailure],
            speechService.AnnouncementKinds);
    }

    [Fact]
    public async Task TheAnnouncement_StillSpeaksTheSentence()
    {
        // Die richtige Kategorie genügt nicht: Der Aufruf muss auch einen nichtleeren Text enthalten.
        var speechService = new RecordingSpeechService();
        var context = SpellingWorkflowTestFactory.CreateContext("de");

        await CharacterAccumulationRecognizerUnavailableAnnouncement.SpeakAsync(
            speechService,
            context,
            CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        Assert.False(string.IsNullOrWhiteSpace(call.Text));
    }
}
