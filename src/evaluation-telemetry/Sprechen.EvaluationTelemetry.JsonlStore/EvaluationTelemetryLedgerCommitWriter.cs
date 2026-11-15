using System.Security.Cryptography;
using System.Text;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Schreibt Ledgerzeilen so, dass ein Abbruch keine halbe Zeile hinterlässt.
/// </summary>
internal sealed class EvaluationTelemetryLedgerCommitWriter
{
    // Exklusive Dateisperre ohne Verfall. Die Zeitspanne ist nur die Wartezeit beim Erwerb.
    private const string DirectoryLeaseFileName = ".evaluation-telemetry.lock";
    private static readonly TimeSpan DirectoryLeaseTimeout = TimeSpan.FromSeconds(10);
    private static readonly UTF8Encoding Utf8NoBomStrict = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly Action<string, string> _commitTempFile;

    public EvaluationTelemetryLedgerCommitWriter(Action<string, string> commitTempFile)
    {
        _commitTempFile = commitTempFile;
    }

    internal string BuildTargetPath(
        string dataDirectory,
        EvaluationTelemetryLedgerRecord ledgerRecord)
    {
        if (string.IsNullOrWhiteSpace(dataDirectory))
        {
            throw new InvalidOperationException("The evaluation telemetry JSONL data directory is not configured.");
        }

        return Path.Combine(
            Path.GetFullPath(dataDirectory),
            $"participant-{BuildPartitionIdentifier(ledgerRecord.ParticipantId)}",
            $"task-{BuildPartitionIdentifier(ledgerRecord.TaskId)}-{ledgerRecord.Condition}.jsonl");
    }

    internal async Task<FileStream> AcquireDirectoryLeaseAsync(
        string dataDirectory,
        CancellationToken cancellationToken)
    {
        var leasePath = Path.Combine(dataDirectory, DirectoryLeaseFileName);
        var deadline = DateTime.UtcNow + DirectoryLeaseTimeout;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    leasePath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.Asynchronous | FileOptions.WriteThrough);
            }
            catch (IOException) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    internal async Task CommitNewLineAsync(
        string targetPath,
        string jsonLine,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(targetPath)
            ?? throw new InvalidOperationException("The telemetry target path has no parent directory.");
        Directory.CreateDirectory(directory);

        var tempPath = Path.Combine(
            directory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var tempStream = new FileStream(
                tempPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                if (File.Exists(targetPath))
                {
                    await using var currentStream = new FileStream(
                        targetPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        bufferSize: 4096,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    var needsSeparator = currentStream.Length > 0 && !EndsWithNewline(currentStream);
                    currentStream.Position = 0;
                    await currentStream.CopyToAsync(tempStream, cancellationToken).ConfigureAwait(false);
                    if (needsSeparator)
                    {
                        await tempStream.WriteAsync("\n"u8.ToArray(), cancellationToken).ConfigureAwait(false);
                    }
                }

                var newLine = Utf8NoBomStrict.GetBytes(jsonLine + "\n");
                await tempStream.WriteAsync(newLine, cancellationToken).ConfigureAwait(false);
                await tempStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                tempStream.Flush(flushToDisk: true);
            }

            _commitTempFile(tempPath, targetPath);
            EvaluationTelemetryLinuxDirectoryDurability.Flush(directory);
            var partitionParent = Path.GetDirectoryName(directory);
            if (partitionParent is not null)
            {
                EvaluationTelemetryLinuxDirectoryDurability.Flush(partitionParent);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    private static bool EndsWithNewline(FileStream stream)
    {
        stream.Position = stream.Length - 1;
        return stream.ReadByte() == '\n';
    }

    private static string BuildPartitionIdentifier(string value)
    {
        var hash = SHA256.HashData(Utf8NoBomStrict.GetBytes(value));
        return string.Create(hash.Length * 2, hash, static (characters, bytes) =>
        {
            const string hex = "0123456789abcdef";
            for (var index = 0; index < bytes.Length; index++)
            {
                characters[index * 2] = hex[bytes[index] >> 4];
                characters[(index * 2) + 1] = hex[bytes[index] & 0x0f];
            }
        });
    }
}
