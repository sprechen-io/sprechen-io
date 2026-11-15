using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Prüft stündlich das konfigurierte Diagnoseverzeichnis und löscht Dateien,
/// deren letzte Änderung mehr als eine Stunde zurückliegt.
/// </summary>
internal sealed class RecognizedAudioDumpCleanup : BackgroundService
{
    private static readonly TimeSpan MaxFileAge = TimeSpan.FromHours(1);
    private static readonly TimeSpan SweepInterval = TimeSpan.FromHours(1);

    private readonly string? _dumpDirectory;
    private readonly ILogger<RecognizedAudioDumpCleanup> _logger;

    internal RecognizedAudioDumpCleanup(string? dumpDirectory, ILogger<RecognizedAudioDumpCleanup> logger)
    {
        _dumpDirectory = string.IsNullOrWhiteSpace(dumpDirectory) ? null : dumpDirectory.Trim();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_dumpDirectory is null)
        {
            return;
        }

        using var timer = new PeriodicTimer(SweepInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                SweepOnce();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Der Host wird beendet.
        }
    }

    internal int SweepOnce()
    {
        if (_dumpDirectory is null || !Directory.Exists(_dumpDirectory))
        {
            return 0;
        }

        var deleted = 0;
        var threshold = DateTime.UtcNow - MaxFileAge;
        foreach (var path in Directory.EnumerateFiles(_dumpDirectory))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(path) >= threshold)
                {
                    continue;
                }

                File.Delete(path);
                deleted++;
            }
            catch (Exception ex) when (ex is FileNotFoundException or IOException)
            {
                // Einzelne Dateifehler protokollieren und den Durchlauf fortsetzen.
                // Noch vorhandene Dateien werden im nächsten Durchlauf erneut geprüft.
                _logger.LogWarning(ex, "Could not delete recognized audio dump {Path}.", path);
            }
        }

        return deleted;
    }
}
