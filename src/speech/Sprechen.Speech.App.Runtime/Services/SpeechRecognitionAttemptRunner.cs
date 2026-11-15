namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Begrenzt die Erkennungsversuche eines Auftrags und deren Wartezeiten, damit technische Fehler wiederholt werden können, ohne Abbruch oder fehlende Sprache zu wiederholen.
/// </summary>
internal sealed class SpeechRecognitionAttemptRunner
{
    private readonly ISpeechRecognitionClient _inner;
    private readonly IBackoffPolicy _backoff;

    internal SpeechRecognitionAttemptRunner(ISpeechRecognitionClient inner, IBackoffPolicy backoff)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _backoff = backoff ?? throw new ArgumentNullException(nameof(backoff));
    }

    internal async Task<SpeechTranscript> RunAsync(
        SpeechRecognitionWorkItem item,
        CancellationToken shutdown)
    {
        ArgumentNullException.ThrowIfNull(item);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(shutdown, item.RequestToken);

        Exception? lastException = null;
        for (var attempt = 0; attempt < _backoff.MaxRetries; attempt++)
        {
            try
            {
                return await _inner
                    .SpeechToTextAsync(item.Audio, item.LanguageCode, item.RecognitionProfile, linked.Token, item.Keyterms)
                    .ConfigureAwait(false);
            }
            catch (NoSpeechDetectedException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (IsFinalAttempt(attempt))
                {
                    break;
                }

                await DelayBeforeRetryAsync(attempt, linked.Token).ConfigureAwait(false);
            }
        }

        throw new SpeechRecognitionBackoffExhaustedException(_backoff.MaxRetries, lastException);
    }

    private bool IsFinalAttempt(int attempt)
    {
        return attempt + 1 >= _backoff.MaxRetries;
    }

    private async Task DelayBeforeRetryAsync(int attempt, CancellationToken cancellationToken)
    {
        await Task.Delay(_backoff.GetDelay(attempt), cancellationToken).ConfigureAwait(false);
    }
}
