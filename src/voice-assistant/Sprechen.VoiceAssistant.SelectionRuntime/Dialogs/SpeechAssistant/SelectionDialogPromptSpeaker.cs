using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant.Logging;

namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Gibt die Segmente einer Auswahlansage mit den vorgegebenen Pausen wieder und beobachtet
/// Wiedergabeabschlüsse, ohne einen fremden Abbruch pauschal als Nutzerunterbrechung zu deuten.
/// </summary>
internal sealed class SelectionDialogPromptSpeaker
{
    private readonly ITextToSpeechPlayer _textToSpeechPlayer;
    private readonly ILogger _logger;

    public SelectionDialogPromptSpeaker(
        ITextToSpeechPlayer textToSpeechPlayer,
        ILogger logger)
    {
        _textToSpeechPlayer = textToSpeechPlayer ?? throw new ArgumentNullException(nameof(textToSpeechPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SpeakAsync(
        SelectionDialogPrompt prompt,
        string isoCode,
        SelectionDialogInteractionSettings interactionSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(interactionSettings);

        if (!prompt.HasContent)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(prompt.IntroText))
        {
            await SpeakTextAsync(prompt.IntroText, isoCode, interactionSettings.SpeechRate, prompt.AnnouncementKind, cancellationToken).ConfigureAwait(false);
        }

        for (var i = 0; i < prompt.ItemTexts.Length; i++)
        {
            await SpeakTextAsync(prompt.ItemTexts[i], isoCode, interactionSettings.SpeechRate, prompt.AnnouncementKind, cancellationToken).ConfigureAwait(false);

            if (i < prompt.ItemTexts.Length - 1 && interactionSettings.PauseBetweenItemsMs > 0)
            {
                await Task.Delay(
                        TimeSpan.FromMilliseconds(interactionSettings.PauseBetweenItemsMs),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        if (!string.IsNullOrWhiteSpace(prompt.TrailingText))
        {
            await SpeakTextAsync(prompt.TrailingText, isoCode, interactionSettings.SpeechRate, prompt.AnnouncementKind, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task SpeakAsync(string text, string isoCode, CancellationToken cancellationToken)
    {
        var prompt = SelectionDialogPrompt.FromText(text);
        if (!prompt.HasContent)
        {
            return;
        }

        await SpeakTextAsync(prompt.Text, isoCode, speechRate: 1.0d, prompt.AnnouncementKind, cancellationToken).ConfigureAwait(false);
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
            // Einen nicht vom Aufrufer angeforderten Abbruch beobachten, ohne daraus eine
            // Nutzerunterbrechung abzuleiten. Diese Grenze kann deren Ursache nicht eindeutig
            // bestimmen.
            _logger.LogInformation(
                SelectionRuntimeLogEvents.PromptPlaybackCancelledUnattributed,
                "Selection prompt playback was cancelled without the caller asking; barge-in and a "
                + "far-side cancellation are indistinguishable here.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(SelectionRuntimeLogEvents.PromptPlaybackFailed, ex, "Prompt playback failed.");
            throw;
        }
    }

    private async Task SpeakTextAsync(
        string text,
        string isoCode,
        double speechRate,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken)
    {
        try
        {
            await _textToSpeechPlayer.SpeakAsync(text, isoCode, speechRate, announcementKind, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(SelectionRuntimeLogEvents.TtsFailed, ex, "TTS failed.");
            throw;
        }
    }
}
