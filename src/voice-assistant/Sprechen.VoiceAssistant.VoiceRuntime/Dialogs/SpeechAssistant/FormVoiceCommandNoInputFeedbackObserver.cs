using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.ModelRuntime.PromptTemplates;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert vor einem erneuten Aufnahmeversuch nach fehlender Eingabe
/// einen Hinweis an. Der Sprachdienst aktiviert ihn für seine innere
/// Wiederholungsschleife. Bei einzelnen Bestätigungsversuchen fordert
/// der äußere Ablauf die erneute Ansage an.
/// </summary>
internal sealed class FormVoiceCommandNoInputFeedbackObserver : IFormVoiceCommandAttemptObserver
{
    private readonly ITextToSpeechPlayer _textToSpeechPlayer;

    // Der Sprachdienst setzt diesen Kontext für die laufende innere
    // Aufnahmeschleife und löscht ihn abschließend. Der Besitzer muss
    // die Zugriffe serialisieren. Der Speicherplatz selbst synchronisiert nicht.
    private (string IsoCode, double SpeechRate, PolitenessLevel PolitenessLevel)? _activeCapture;

    public FormVoiceCommandNoInputFeedbackObserver(ITextToSpeechPlayer textToSpeechPlayer)
    {
        _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
    }

    public void SetActiveCapture(string isoCode, double speechRate, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        _activeCapture = (isoCode, speechRate, politenessLevel);
    }

    public void ClearActiveCapture() => _activeCapture = null;

    public async Task OnAttemptStartingAsync(FormVoiceCommandAttempt attempt, CancellationToken cancellationToken)
    {
        // Nur eine vorangegangene Eingabezeitüberschreitung mit aktivem Kontext
        // löst den Hinweis aus. Der erste Versuch benötigt keine zweite Ansage.
        if (attempt.PrecedingFailure != FormVoiceCommandAttemptFailure.NoInputTimeout
            || _activeCapture is not { } capture)
        {
            return;
        }

        var prompt = NoInputCapturePromptTextResolver.GetNoInputPrompt(capture.IsoCode, capture.PolitenessLevel);

        // Den Hinweis vollständig abwarten, bevor das nächste Fenster öffnet.
        // Sonst könnte die Aufnahme die eigene Ansage erfassen. Die Kennzeichnung
        // als fehlende Eingabe unterscheidet gleichlautende Katalogtexte bei der Auswertung.
        await _textToSpeechPlayer
            .SpeakAsync(prompt, capture.IsoCode, capture.SpeechRate, SpokenAnnouncementKind.NoInput, cancellationToken)
            .ConfigureAwait(false);
    }
}
