namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Überträgt jedes Erkennungsende auf das Auftragsergebnis, damit auch unerwartete Fehler keinen wartenden Aufrufer zurücklassen.
/// </summary>
internal sealed class SpeechRecognitionWorkItemTerminalFlow
{
    private readonly SpeechRecognitionAttemptRunner _attemptRunner;

    internal SpeechRecognitionWorkItemTerminalFlow(ISpeechRecognitionClient inner, IBackoffPolicy backoff)
        : this(new SpeechRecognitionAttemptRunner(inner, backoff))
    {
    }

    internal SpeechRecognitionWorkItemTerminalFlow(SpeechRecognitionAttemptRunner attemptRunner)
    {
        _attemptRunner = attemptRunner ?? throw new ArgumentNullException(nameof(attemptRunner));
    }

    internal async Task ProcessAsync(SpeechRecognitionWorkItem item, CancellationToken shutdown)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (TryCancelBeforeStart(item, shutdown))
        {
            return;
        }

        try
        {
            var transcript = await _attemptRunner.RunAsync(item, shutdown).ConfigureAwait(false);
            item.Tcs.TrySetResult(transcript);
        }
        catch (NoSpeechDetectedException nspe)
        {
            item.Tcs.TrySetException(nspe);
        }
        catch (OperationCanceledException) when (item.RequestToken.IsCancellationRequested)
        {
            item.Tcs.TrySetCanceled(item.RequestToken);
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            item.Tcs.TrySetCanceled(shutdown);
        }
        catch (SpeechRecognitionBackoffExhaustedException ex)
        {
            item.Tcs.TrySetException(ex);
        }
        catch (Exception ex)
        {
            // Auch ein fremder Abbruch ohne lokales Abbruchsignal muss den Auftrag
            // abschließen, damit sein Aufrufer kein dauerhaft offenes Ergebnis behält.
            item.Tcs.TrySetException(ex);
        }
    }

    private static bool TryCancelBeforeStart(SpeechRecognitionWorkItem item, CancellationToken shutdown)
    {
        if (item.RequestToken.IsCancellationRequested)
        {
            item.Tcs.TrySetCanceled(item.RequestToken);
            return true;
        }

        if (shutdown.IsCancellationRequested)
        {
            item.Tcs.TrySetCanceled(shutdown);
            return true;
        }

        return false;
    }
}
