using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Reicht Ansagen und ihre Kennzeichnung an die Sprachausgabe weiter
/// und unterscheidet angeforderten Abbruch von nicht zuordenbarer Unterbrechung.
/// </summary>
internal sealed class FormVoiceCommandPromptPlayback
{
    private readonly ITextToSpeechPlayer _textToSpeechPlayer;
    private readonly ILogger _logger;

    public FormVoiceCommandPromptPlayback(
        ITextToSpeechPlayer textToSpeechPlayer,
        ILogger logger)
    {
        _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(
        string prompt,
        string isoCode,
        double speechRate,
        CancellationToken cancellationToken)
        => _textToSpeechPlayer.SpeakAsync(prompt, isoCode, speechRate, cancellationToken);

    public Task SpeakSafeAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        CancellationToken cancellationToken)
        => SpeakSafeAsync(text, isoCode, voiceSettings, SpokenAnnouncementKind.Prompt, cancellationToken);

    /// <summary>
    /// Fordert eine Formularansage mit ihrer Ereignisart bei der Sprachausgabe an.
    /// So muss etwa ein technischer Fehler nicht allein anhand des Textes
    /// eingeordnet werden.
    /// </summary>
    public async Task SpeakSafeAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        try
        {
            await _textToSpeechPlayer
                .SpeakAsync(text, isoCode, voiceSettings.GetSpeechRate(), announcementKind, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Der übergebene Abbruch wurde nicht signalisiert. Die Ursache der
            // Ausgabeunterbrechung lässt sich hier nicht sicher zuordnen. Deshalb
            // kein Unterbrechungsereignis erfinden, sondern protokollieren und fortfahren.
            _logger.LogInformation(
                VoiceRuntimeLogEvents.PromptPlaybackCancelledUnattributed,
                "Prompt playback was cancelled without the caller asking; barge-in and a far-side "
                + "cancellation are indistinguishable here. (stage={Stage})",
                "SpeakSafeAsync");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(VoiceRuntimeLogEvents.TtsFailed, ex, "TTS failed.");
            throw;
        }
    }

    public async Task ObserveCompletionAsync(Task promptTask, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(promptTask);

        try
        {
            await promptTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            // Der Aufruferabbruch wurde oben behandelt. Eine andere Abbruchquelle
            // kann etwa die Ansageunterbrechung sein.
            _logger.LogInformation(
                VoiceRuntimeLogEvents.PromptPlaybackCancelledUnattributed,
                "Prompt playback was cancelled without the caller asking; barge-in and a far-side "
                + "cancellation are indistinguishable here. (stage={Stage})",
                "ObserveCompletionAsync");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(VoiceRuntimeLogEvents.PromptPlaybackFailed, ex, "Prompt playback failed.");
            throw;
        }
    }
}
