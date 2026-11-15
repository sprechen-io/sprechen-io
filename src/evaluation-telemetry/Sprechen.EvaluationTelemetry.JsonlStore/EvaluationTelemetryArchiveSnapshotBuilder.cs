using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Erzeugt ein geprüftes ZIP-Archiv auf einem temporären Dateistream
/// und übergibt dessen Freigabe an den Empfänger.
/// </summary>
internal sealed class EvaluationTelemetryArchiveSnapshotBuilder
{
    private static readonly UTF8Encoding Utf8NoBomStrict = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private static readonly byte[] LineFeedBytes = [(byte)'\n'];

    private readonly IEvaluationTelemetryLedgerRecordValidator _ledgerRecordValidator;

    public EvaluationTelemetryArchiveSnapshotBuilder(
        IEvaluationTelemetryLedgerRecordValidator ledgerRecordValidator)
    {
        _ledgerRecordValidator = ledgerRecordValidator;
    }

    internal async Task<EvaluationTelemetryArchiveSnapshot> BuildAsync(
        string dataDirectory,
        CancellationToken cancellationToken)
    {
        var generatedAtUtc = DateTimeOffset.UtcNow;
        var partitions = new List<EvaluationTelemetryArchivePartition>();
        var observedEventIds = new HashSet<string>(StringComparer.Ordinal);
        var ledgerPaths = Directory
            .EnumerateFiles(dataDirectory, "*.jsonl", SearchOption.AllDirectories)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var archivePath = Path.Combine(
            dataDirectory,
            $".evaluation-telemetry-archive-{Guid.NewGuid():N}.tmp");
        FileStream? archiveStream = null;
        try
        {
            archiveStream = new FileStream(
                archivePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 81920,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
            if (OperatingSystem.IsLinux())
            {
                // Unter Linux bleibt der geöffnete Stream nach Entfernen des Verzeichniseintrags lesbar.
                // So bleibt auch bei einem Prozessabbruch kein benanntes temporäres Archiv zurück.
                File.Delete(archivePath);
            }

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var ledgerPath in ledgerPaths)
                {
                    var relativePath = Path.GetRelativePath(dataDirectory, ledgerPath)
                        .Replace(Path.DirectorySeparatorChar, '/');
                    var archivePathForLedger = $"partitions/{relativePath}";
                    var entry = archive.CreateEntry(archivePathForLedger, CompressionLevel.Optimal);
                    entry.LastWriteTime = generatedAtUtc;
                    await using var entryStream = entry.Open();
                    var partition = await CopyValidatedLedgerAsync(
                            ledgerPath,
                            archivePathForLedger,
                            entryStream,
                            observedEventIds,
                            cancellationToken)
                        .ConfigureAwait(false);
                    partitions.Add(partition);
                }

                var manifest = new EvaluationTelemetryArchiveManifest(
                    SchemaVersion: 1,
                    GeneratedAtUtc: generatedAtUtc.UtcDateTime,
                    PartitionCount: partitions.Count,
                    EventCount: partitions.Sum(static partition => partition.EventCount),
                    Partitions: partitions);
                var manifestEntry = archive.CreateEntry("archive-manifest.json", CompressionLevel.Optimal);
                manifestEntry.LastWriteTime = generatedAtUtc;
                await using var manifestStream = manifestEntry.Open();
                await JsonSerializer.SerializeAsync(
                    manifestStream,
                    manifest,
                    EvaluationTelemetryArchiveManifest.SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            await archiveStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            archiveStream.Position = 0;
            var sha256 = Convert.ToHexStringLower(
                await SHA256.HashDataAsync(archiveStream, cancellationToken).ConfigureAwait(false));
            archiveStream.Position = 0;
            var snapshot = new EvaluationTelemetryArchiveSnapshot(
                archiveStream,
                sha256,
                partitions.Count,
                partitions.Sum(static partition => partition.EventCount),
                generatedAtUtc);
            archiveStream = null;
            return snapshot;
        }
        finally
        {
            if (archiveStream is not null)
            {
                await archiveStream.DisposeAsync().ConfigureAwait(false);
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
            }
        }
    }

    private async Task<EvaluationTelemetryArchivePartition> CopyValidatedLedgerAsync(
        string ledgerPath,
        string archivePath,
        Stream destination,
        HashSet<string> observedEventIds,
        CancellationToken cancellationToken)
    {
        await using var source = new FileStream(
            ledgerPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 8192,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (source.Length == 0)
        {
            throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is empty.");
        }

        var readBuffer = new byte[8192];
        var lineBuffer = new byte[EvaluationTelemetryIngestionLimits.MaximumEnvelopeBytes];
        var lineLength = 0;
        var eventCount = 0;
        long byteLength = 0;
        using var partitionHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        while (await source.ReadAsync(readBuffer, cancellationToken).ConfigureAwait(false) is var bytesRead
            && bytesRead > 0)
        {
            for (var index = 0; index < bytesRead; index++)
            {
                var value = readBuffer[index];
                if (value == (byte)'\r')
                {
                    throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' contains a CR byte.");
                }

                if (value != (byte)'\n')
                {
                    if (lineLength == lineBuffer.Length)
                    {
                        throw new InvalidDataException(
                            $"The JSONL ledger '{ledgerPath}' contains an envelope beyond the bounded ingestion size.");
                    }

                    lineBuffer[lineLength++] = value;
                    continue;
                }

                if (lineLength == 0)
                {
                    throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' contains an empty line.");
                }

                string line;
                try
                {
                    line = Utf8NoBomStrict.GetString(lineBuffer, 0, lineLength);
                }
                catch (DecoderFallbackException ex)
                {
                    throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is not valid UTF-8.", ex);
                }

                var reading = _ledgerRecordValidator.ReadCanonicalLine(line);
                if (reading.Record is not { } record)
                {
                    throw new InvalidDataException(
                        $"The JSONL ledger '{ledgerPath}' refuses line {eventCount + 1}: {reading.Refusal}");
                }

                if (!observedEventIds.Add(record.EventId))
                {
                    throw new InvalidDataException(
                        $"The evaluation telemetry ledgers contain duplicate event identifier '{record.EventId}'.");
                }

                await destination.WriteAsync(lineBuffer.AsMemory(0, lineLength), cancellationToken)
                    .ConfigureAwait(false);
                await destination.WriteAsync(LineFeedBytes, cancellationToken).ConfigureAwait(false);
                partitionHash.AppendData(lineBuffer, 0, lineLength);
                partitionHash.AppendData(LineFeedBytes);
                byteLength += lineLength + 1;
                eventCount++;
                lineLength = 0;
            }
        }

        if (lineLength != 0)
        {
            throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is not newline-terminated.");
        }

        if (eventCount == 0)
        {
            throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is empty.");
        }

        return new EvaluationTelemetryArchivePartition(
            archivePath,
            Convert.ToHexStringLower(partitionHash.GetHashAndReset()),
            byteLength,
            eventCount);
    }
}
