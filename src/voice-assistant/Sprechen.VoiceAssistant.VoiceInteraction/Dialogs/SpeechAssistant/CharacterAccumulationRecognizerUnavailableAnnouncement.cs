using Sprechen.Speech.ModelRuntime.PromptTemplates;

using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert beim schrittweisen Sammeln von Werten dieselbe lokalisierte Ansage
/// für nicht verfügbare Erkennung an. Die Rückmeldung stammt dadurch aus
/// einer gemeinsamen Quelle.
/// </summary>
internal static class CharacterAccumulationRecognizerUnavailableAnnouncement
{
    public static Task SpeakAsync(
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(speechService);
        ArgumentNullException.ThrowIfNull(context);

        var text = RecoverableCapturePromptTextResolver.GetRecognizerUnavailablePrompt(
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel());

        // Die Ansage nach Möglichkeit über die ausdrücklich deklarierte Rückmelderolle
        // ausgeben. Fehlt diese Rolle, dieselbe Ansage über den allgemeinen Sprachzugang
        // anfordern.
        return speechService is IDeclaredAnnouncementSpeaker declaring
            ? declaring.SpeakAnnouncementAsync(
                text,
                context.IsoCode,
                context.VoiceSettings,
                SpokenAnnouncementKind.RecoverableFailure,
                cancellationToken)
            : speechService.SpeakSafeAsync(text, context.IsoCode, context.VoiceSettings, cancellationToken);
    }
}
