namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Wartet beim Abbau einer gRPC-Duplexsitzung zunächst eine Schonfrist ab
/// und signalisiert danach den Sitzungsabbruch. Das anschließende Beobachten
/// wartet weiterhin auf die Tasks. Ignorieren sie den Abbruch, ist die Gesamtdauer unbeschränkt.
/// </summary>
internal sealed class GrpcDuplexStreamTeardown
{
    private readonly TimeSpan _stopGrace;

    public GrpcDuplexStreamTeardown(TimeSpan stopGrace)
    {
        if (stopGrace <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stopGrace),
                stopGrace,
                "The stop grace period must be positive so teardown is bounded but can still flush in-flight writes.");
        }

        _stopGrace = stopGrace;
    }

    /// <summary>
    /// Wartet zunächst die Schonfrist auf beide Schleifen und gibt deren Fehler
    /// bei rechtzeitigem Abschluss weiter. Andernfalls wird der Sitzungsabbruch
    /// signalisiert und ohne erneute Frist auf beide Schleifen gewartet. Ihre Fehler werden dann nur beobachtet.
    /// </summary>
    public async Task AwaitSendReceiveWithGraceAsync(
        Task? sendTask,
        Task? receiveTask,
        CancellationTokenSource sessionCts)
    {
        ArgumentNullException.ThrowIfNull(sessionCts);

        var send = sendTask ?? Task.CompletedTask;
        var receive = receiveTask ?? Task.CompletedTask;
        var loops = Task.WhenAll(send, receive);

        var finished = await Task.WhenAny(loops, Task.Delay(_stopGrace)).ConfigureAwait(false);
        if (ReferenceEquals(finished, loops))
        {
            await loops.ConfigureAwait(false);
            return;
        }

        Cancel(sessionCts);
        await ObserveAsync(send).ConfigureAwait(false);
        await ObserveAsync(receive).ConfigureAwait(false);
    }

    /// <summary>
    /// Wartet beim Freigeben zunächst die doppelte Schonfrist auf einen laufenden
    /// Stoppvorgang. Nach Fristablauf wird der Sitzungsabbruch signalisiert und der
    /// Stopp ohne weitere Zeitgrenze beobachtet. Seine Fehler werden nicht weitergegeben.
    /// </summary>
    public async Task AwaitInFlightStopWithGraceAsync(Task stopTask, CancellationTokenSource sessionCts)
    {
        ArgumentNullException.ThrowIfNull(stopTask);
        ArgumentNullException.ThrowIfNull(sessionCts);

        try
        {
            await stopTask.WaitAsync(_stopGrace + _stopGrace).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            Cancel(sessionCts);
            await ObserveAsync(stopTask).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Fehler des Stoppvorgangs werden beim Freigeben nicht erneut weitergegeben.
        }
    }

    /// <summary>
    /// Wartet ohne Zeitgrenze auf einen vorhandenen Task und beobachtet dessen
    /// Fehler, damit er nicht unbeobachtet bleibt. Die Ausnahme wird hier nicht weitergegeben.
    /// </summary>
    public static async Task ObserveAsync(Task? task)
    {
        if (task is null)
        {
            return;
        }

        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
    }

    private static void Cancel(CancellationTokenSource sessionCts)
    {
        try
        {
            sessionCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
