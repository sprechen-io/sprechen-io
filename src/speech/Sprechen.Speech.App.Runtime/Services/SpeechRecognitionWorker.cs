using System.Threading.Channels;

namespace Sprechen.Speech.App.Runtime.Services;

/// <summary>
/// Verarbeitet Erkennungsaufträge nacheinander mit begrenzter Warteschlange, damit konkurrierende Aufrufer eine gemeinsame Reihenfolge und Wiederholungsregel erhalten.
/// </summary>
/// <remarks>
/// Eine volle Warteschlange weist den Auftrag synchron ab. Technische Erkennungsfehler
/// führen nach dem Versuchslimit zum Fehler im zurückgegebenen Task.
/// Leere Transkripte und <see cref="NoSpeechDetectedException"/> gelangen ohne Wiederholung
/// zum Aufrufer. Auftragsabbruch und Herunterfahren signalisieren dem laufenden Clientaufruf den Abbruch.
/// Ein anderweitiger Abbruch wird als Fehler des Auftrags weitergegeben.
/// </remarks>
public sealed class SpeechRecognitionWorker : IAsyncDisposable
{
    public const int DefaultMaxQueueDepth = 1;

    private readonly SpeechRecognitionWorkItemTerminalFlow _terminalFlow;
    private readonly Channel<SpeechRecognitionWorkItem> _queue;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _loop;
    private int _disposed;

    public SpeechRecognitionWorker(ISpeechRecognitionClient inner)
        : this(inner, backoff: null, maxQueueDepth: DefaultMaxQueueDepth)
    {
    }

    public SpeechRecognitionWorker(ISpeechRecognitionClient inner, IBackoffPolicy? backoff, int maxQueueDepth = DefaultMaxQueueDepth)
    {
        ArgumentNullException.ThrowIfNull(inner);
        if (maxQueueDepth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueueDepth), maxQueueDepth, "Queue depth must be positive.");
        }

        var retryPolicy = backoff ?? new ExponentialBackoffPolicy();
        _terminalFlow = new SpeechRecognitionWorkItemTerminalFlow(inner, retryPolicy);
        MaxQueueDepth = maxQueueDepth;
        _queue = Channel.CreateBounded<SpeechRecognitionWorkItem>(new BoundedChannelOptions(maxQueueDepth)
        {
            FullMode = BoundedChannelFullMode.Wait, // TryWrite meldet Überlast sofort, damit der Aufrufer nicht auf freien Platz wartet.
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false,
        });

        _loop = Task.Run(() => RunLoopAsync(_shutdownCts.Token));
    }

    public int MaxQueueDepth { get; }

    /// <summary>
    /// Reiht einen Erkennungsauftrag ein und liefert dessen Ergebnis für Erfolg, Fehler, fehlende Sprache oder Abbruch zurück.
    /// Eine volle Warteschlange wird synchron durch <see cref="SpeechRecognitionQueueFullException"/> gemeldet.
    /// </summary>
    public Task<SpeechTranscript> EnqueueAsync(
        CapturedSpeechAudio audio,
        string? languageCode,
        CancellationToken cancellationToken)
        => EnqueueAsync(audio, languageCode, RecognitionProfile.Conversational, cancellationToken);

    /// <summary>
    /// Reiht einen Auftrag mit vorgegebenem Erkennungsprofil ein, damit der Aufrufer die Erkennung an den Dialogschritt anpassen kann.
    /// Der Aufruf ohne Profil verwendet <see cref="RecognitionProfile.Conversational"/>.
    /// </summary>
    public Task<SpeechTranscript> EnqueueAsync(
        CapturedSpeechAudio audio,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        CancellationToken cancellationToken)
        => EnqueueAsync(audio, languageCode, recognitionProfile, keyterms: null, cancellationToken);

    /// <summary>
    /// Reiht einen Auftrag mit Erkennungsprofil und optionalen Schlüsselwörtern ein, damit der Erkenner etwa Namen der angebotenen Anwendungen gewichten kann.
    /// Die Wörter werden unverändert weitergereicht. Eine leere Liste fügt keine Schlüsselwortgewichtung hinzu.
    /// </summary>
    public Task<SpeechTranscript> EnqueueAsync(
        CapturedSpeechAudio audio,
        string? languageCode,
        RecognitionProfile recognitionProfile,
        SpeechKeyterms? keyterms,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(audio);

        if (_disposed != 0)
        {
            throw new ObjectDisposedException(nameof(SpeechRecognitionWorker));
        }

        var tcs = new TaskCompletionSource<SpeechTranscript>(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new SpeechRecognitionWorkItem(audio, languageCode, recognitionProfile, keyterms, cancellationToken, tcs);
        if (!_queue.Writer.TryWrite(item))
        {
            throw new SpeechRecognitionQueueFullException(MaxQueueDepth);
        }

        return tcs.Task;
    }

    private async Task RunLoopAsync(CancellationToken shutdown)
    {
        try
        {
            await foreach (var item in _queue.Reader.ReadAllAsync(shutdown).ConfigureAwait(false))
            {
                await _terminalFlow.ProcessAsync(item, shutdown).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
        {
            // DisposeAsync schließt die noch wartenden Aufträge nach dem Herunterfahren ab.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (System.Threading.Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _queue.Writer.TryComplete();

        try
        {
            _shutdownCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }

        try
        {
            await _loop.ConfigureAwait(false);
        }
        catch
        {
            // Auch nach einem Schleifenfehler müssen wartende Aufträge abgeschlossen werden.
        }

        // Noch ungelesene Aufträge werden abgebrochen, damit ihre Aufrufer
        // nach Ende der Schleife kein dauerhaft offenes Ergebnis behalten.
        while (_queue.Reader.TryRead(out var leftover))
        {
            leftover.Tcs.TrySetCanceled();
        }

        _shutdownCts.Dispose();
    }
}
