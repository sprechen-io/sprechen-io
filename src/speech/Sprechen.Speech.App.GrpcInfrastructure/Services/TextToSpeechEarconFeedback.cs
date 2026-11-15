using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Beendet wiederholte Hinweistöne vor Sprache und spielt den modellierten
/// Fehlerklang, wenn vor dem ersten PCM-Block die Wartefrist endet.
/// </summary>
internal sealed class TextToSpeechEarconFeedback
{
    private readonly IEarconPlayer _earconPlayer;
    private readonly EarconId _firstPcmTimeoutEarconId;
    private readonly ILogger<TextToSpeechEarconFeedback> _logger;

    public TextToSpeechEarconFeedback(
        IEarconPlayer earconPlayer,
        ILogger<TextToSpeechEarconFeedback> logger,
        EarconId firstPcmTimeoutEarconId)
    {
        if (!Enum.IsDefined(firstPcmTimeoutEarconId))
        {
            throw new ArgumentOutOfRangeException(nameof(firstPcmTimeoutEarconId));
        }

        _firstPcmTimeoutEarconId = firstPcmTimeoutEarconId;
        _earconPlayer = earconPlayer ?? throw new ArgumentNullException(nameof(earconPlayer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StopBestEffortAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Nur wiederholte Hinweistöne stoppen: Ein gerade angeforderter einmaliger
            // Bestätigungston soll beim Beginn der Ergebnisansage hörbar bleiben.
            // Einmalige Töne dürfen sich kurz mit der Sprachausgabe überlappen.
            // Die Mikrofonsperre beobachtet sowohl Hinweistöne als auch die Sprachausgabe.
            await _earconPlayer.StopLoopingAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Stopping the looping earcon before text-to-speech playback failed.");
        }
    }

    public async Task PlayTimeoutBestEffortAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await _earconPlayer.PlayAsync(_firstPcmTimeoutEarconId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Playing the local speech-output timeout cue failed.");
        }

        cancellationToken.ThrowIfCancellationRequested();
    }
}
