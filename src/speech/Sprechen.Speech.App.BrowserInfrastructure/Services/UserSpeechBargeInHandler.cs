using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Unterbricht laufende Sprachausgabe beim Aufnahmebeginn und schont dabei Earcons,
/// damit hörbare Orientierungssignale nicht als Anlass für eine Unterbrechung gelten.
/// </summary>
internal sealed class UserSpeechBargeInHandler(
    IBargeInCoordinator bargeInCoordinator,
    IPcmAudioPlayer pcmAudioPlayer,
    ISpeechBargeInSignals bargeInSignals,
    IEarconPlaybackState earconPlaybackState,
    ILogger<UserSpeechBargeInHandler> logger) : IUserSpeechBargeInHandler
{
    private readonly IBargeInCoordinator _bargeInCoordinator = bargeInCoordinator ?? throw new ArgumentNullException(nameof(bargeInCoordinator));
    private readonly IPcmAudioPlayer _pcmAudioPlayer = pcmAudioPlayer ?? throw new ArgumentNullException(nameof(pcmAudioPlayer));
    private readonly ISpeechBargeInSignals _bargeInSignals = bargeInSignals ?? throw new ArgumentNullException(nameof(bargeInSignals));
    private readonly IEarconPlaybackState _earconPlaybackState = earconPlaybackState ?? throw new ArgumentNullException(nameof(earconPlaybackState));
    private readonly ILogger<UserSpeechBargeInHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask OnSpeechCaptureStartingAsync(CancellationToken cancellationToken)
    {
        if (_earconPlaybackState.IsPlaying)
        {
            _logger.LogDebug("Suppressing barge-in while an earcon is playing.");
            return;
        }

        _bargeInCoordinator.BargeIn();

        try
        {
            await _bargeInSignals.PublishBargeInAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Publishing barge-in message failed.");
        }

        try
        {
            await _pcmAudioPlayer.ResetAsync(cancellationToken);
        }
        catch (SpeechErrorException ex)
        {
            _logger.LogDebug(ex, "PCM player reset failed during barge-in.");
        }
    }
}


