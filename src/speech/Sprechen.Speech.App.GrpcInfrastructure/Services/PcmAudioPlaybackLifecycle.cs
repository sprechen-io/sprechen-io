using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Verwaltet die Initialisierung des PCM-Players je Abtastrate und vermittelt
/// Einreihen, Lautstärkeanpassung, Ausspielen und Zurücksetzen der Audiodaten.
/// </summary>
internal sealed class PcmAudioPlaybackLifecycle(
    IPcmAudioPlayer pcmAudioPlayer,
    ILogger<PcmAudioPlaybackLifecycle> logger)
{
    private readonly ILogger<PcmAudioPlaybackLifecycle> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPcmAudioPlayer _pcmAudioPlayer = pcmAudioPlayer ?? throw new ArgumentNullException(nameof(pcmAudioPlayer));

    private bool _playerInitialized;
    private int _playerPcmSampleRateHz;

    internal async Task EnsureInitializedAsync(int sampleRateHz, CancellationToken cancellationToken)
    {
        if (sampleRateHz <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleRateHz), sampleRateHz, "Sample rate must be positive.");
        }

        if (_playerInitialized && _playerPcmSampleRateHz == sampleRateHz)
        {
            return;
        }

        await _pcmAudioPlayer.InitializeAsync(sampleRateHz, cancellationToken);
        _playerInitialized = true;
        _playerPcmSampleRateHz = sampleRateHz;
    }

    internal ValueTask ResetAsync(CancellationToken cancellationToken)
    {
        return _pcmAudioPlayer.ResetAsync(cancellationToken);
    }

    internal async ValueTask EnqueuePcm16LeAsync(byte[] bytes, Action? onAccepted, CancellationToken cancellationToken)
    {
        if (onAccepted is not null && _pcmAudioPlayer is IPcmPlaybackAdmission admission)
        {
            await admission.EnqueuePcm16LeAsync(bytes, onAccepted, cancellationToken);
            return;
        }

        await _pcmAudioPlayer.EnqueuePcm16LeAsync(bytes, cancellationToken);
        onAccepted?.Invoke();
    }

    internal ValueTask DrainAsync(Action? onAccepted, CancellationToken cancellationToken)
    {
        return onAccepted is not null && _pcmAudioPlayer is IPcmPlaybackAdmission admission
            ? admission.DrainAsync(onAccepted, cancellationToken)
            : _pcmAudioPlayer.DrainAsync(cancellationToken);
    }

    internal ValueTask SetMakeupAsync(double makeup, CancellationToken cancellationToken)
    {
        return _pcmAudioPlayer.SetMakeupAsync(makeup, cancellationToken);
    }

    internal async Task ResetBestEffortAsync()
    {
        if (!_playerInitialized)
        {
            return;
        }

        try
        {
            await _pcmAudioPlayer.ResetAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "PCM player reset failed during playback teardown.");
        }
    }
}
