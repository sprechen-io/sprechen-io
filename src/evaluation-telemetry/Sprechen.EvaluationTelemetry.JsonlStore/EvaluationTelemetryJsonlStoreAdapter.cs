using Microsoft.Extensions.Options;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Stellt Ereignisspeicher und Archivzugriff auf JSONL-Dateien bereit
/// und synchronisiert beide über dieselben Sperren.
/// </summary>
internal sealed class EvaluationTelemetryJsonlStoreAdapter :
    IEvaluationTelemetryEventStore,
    IEvaluationTelemetryArchiveReader,
    IDisposable
{
    internal const string ArchiveTempFileSearchPattern = ".evaluation-telemetry-archive-*.tmp";

    private readonly IOptions<EvaluationTelemetryJsonlStoreOptions> _options;
    private readonly EvaluationTelemetryLedgerRecordConsistencyValidator _consistencyValidator;
    private readonly EvaluationTelemetryLedgerIdempotencyInspector _idempotencyInspector;
    private readonly EvaluationTelemetryLedgerCommitWriter _commitWriter;
    private readonly EvaluationTelemetryArchiveSnapshotBuilder _archiveSnapshotBuilder;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public EvaluationTelemetryJsonlStoreAdapter(
        IOptions<EvaluationTelemetryJsonlStoreOptions> options,
        IEvaluationTelemetryLedgerRecordValidator ledgerRecordValidator)
        : this(
            options,
            ledgerRecordValidator,
            static (tempPath, targetPath) => File.Move(tempPath, targetPath, overwrite: true))
    {
    }

    internal EvaluationTelemetryJsonlStoreAdapter(
        IOptions<EvaluationTelemetryJsonlStoreOptions> options,
        IEvaluationTelemetryLedgerRecordValidator ledgerRecordValidator,
        Action<string, string> commitTempFile)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(ledgerRecordValidator);
        ArgumentNullException.ThrowIfNull(commitTempFile);
        _consistencyValidator = new EvaluationTelemetryLedgerRecordConsistencyValidator(ledgerRecordValidator);
        _idempotencyInspector = new EvaluationTelemetryLedgerIdempotencyInspector(ledgerRecordValidator);
        _commitWriter = new EvaluationTelemetryLedgerCommitWriter(commitTempFile);
        _archiveSnapshotBuilder = new EvaluationTelemetryArchiveSnapshotBuilder(ledgerRecordValidator);
    }

    public async Task<EvaluationTelemetryEventStoreStatus> AcceptAsync(
        EvaluationTelemetryLedgerRecord ledgerRecord,
        CancellationToken cancellationToken = default)
    {
        _consistencyValidator.Validate(ledgerRecord);

        var options = _options.Value;
        if (!options.IsEnabled)
        {
            return EvaluationTelemetryEventStoreStatus.Unavailable;
        }

        var dataDirectory = Path.GetFullPath(options.DataDirectory);
        var targetPath = _commitWriter.BuildTargetPath(dataDirectory, ledgerRecord);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(dataDirectory);
            await using var directoryLease = await _commitWriter.AcquireDirectoryLeaseAsync(
                dataDirectory,
                cancellationToken).ConfigureAwait(false);
            var existingStatus = await _idempotencyInspector.InspectIdentityAsync(
                dataDirectory,
                ledgerRecord,
                cancellationToken).ConfigureAwait(false);
            await _idempotencyInspector.InspectPartitionFormAsync(targetPath, cancellationToken)
                .ConfigureAwait(false);
            if (existingStatus is not null)
            {
                return existingStatus.Value;
            }

            await _commitWriter.CommitNewLineAsync(targetPath, ledgerRecord.JsonLine, cancellationToken)
                .ConfigureAwait(false);
            _idempotencyInspector.RecordPersistedIdentity(ledgerRecord, targetPath);
            return EvaluationTelemetryEventStoreStatus.Persisted;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async Task<EvaluationTelemetryArchiveSnapshot> CreateSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var options = _options.Value;
        if (!options.IsEnabled)
        {
            throw new InvalidOperationException("The evaluation telemetry JSONL store is disabled.");
        }

        var dataDirectory = Path.GetFullPath(options.DataDirectory);
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            Directory.CreateDirectory(dataDirectory);
            await using var directoryLease = await _commitWriter.AcquireDirectoryLeaseAsync(
                dataDirectory,
                cancellationToken).ConfigureAwait(false);
            return await _archiveSnapshotBuilder.BuildAsync(
                dataDirectory,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        _writeLock.Dispose();
    }
}
