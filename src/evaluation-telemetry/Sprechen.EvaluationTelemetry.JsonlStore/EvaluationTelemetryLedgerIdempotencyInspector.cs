using System.Text;
using Sprechen.EvaluationTelemetry.Ingestion;

namespace Sprechen.EvaluationTelemetry.JsonlStore;

/// <summary>
/// Erkennt schon geschriebene Umschläge, damit ein erneuter Versand denselben
/// Datensatz nicht verdoppelt.
/// </summary>
internal sealed class EvaluationTelemetryLedgerIdempotencyInspector
{
    private static readonly UTF8Encoding Utf8NoBomStrict = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly IEvaluationTelemetryLedgerRecordValidator _ledgerRecordValidator;
    private readonly Dictionary<string, string> _observedSentLines = new(StringComparer.Ordinal);
    private Dictionary<string, long>? _observedFileLengths;

    public EvaluationTelemetryLedgerIdempotencyInspector(
        IEvaluationTelemetryLedgerRecordValidator ledgerRecordValidator)
    {
        _ledgerRecordValidator = ledgerRecordValidator;
    }

    /// <summary>
    /// Vergleicht die Ereignisidentität mit dem zwischengespeicherten Bestand.
    /// Der Aufrufer muss dabei die Dateisperre für das Datenverzeichnis halten.
    /// </summary>
    internal async Task<EvaluationTelemetryEventStoreStatus?> InspectIdentityAsync(
        string dataDirectory,
        EvaluationTelemetryLedgerRecord ledgerRecord,
        CancellationToken cancellationToken)
    {
        // Der Empfangszeitpunkt gehört nicht zum Inhaltsvergleich,
        // weil derselbe Datensatz bei erneuter Zustellung einen anderen Zeitpunkt erhalten kann.
        var incoming = _ledgerRecordValidator.ReadCanonicalLine(ledgerRecord.JsonLine);
        if (incoming.Record is null)
        {
            throw new InvalidDataException(
                $"The evaluation telemetry ledger line to store is refused by its own reader: {incoming.Refusal}");
        }

        if (!ObservedFileSetIsCurrent(dataDirectory))
        {
            await RebuildObservedIdentitiesAsync(dataDirectory, cancellationToken).ConfigureAwait(false);
        }

        if (!_observedSentLines.TryGetValue(ledgerRecord.EventId, out var observedSentLine))
        {
            return null;
        }

        return string.Equals(observedSentLine, incoming.SentLine, StringComparison.Ordinal)
            ? EvaluationTelemetryEventStoreStatus.AlreadyPresent
            : EvaluationTelemetryEventStoreStatus.Conflicting;
    }

    /// <summary>
    /// Prüft die gespeicherte Form der Zielpartition. Formfehler in anderen Partitionen
    /// sollen die Annahme hier nicht blockieren.
    /// </summary>
    internal async Task InspectPartitionFormAsync(
        string ledgerPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(ledgerPath))
        {
            return;
        }

        await using var stream = new FileStream(
            ledgerPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await ValidateLedgerFileShapeAsync(stream, ledgerPath, cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(
            stream,
            Utf8NoBomStrict,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);

        var observedEventIds = new HashSet<string>(StringComparer.Ordinal);
        var lineNumber = 0;
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            lineNumber++;
            if (line.Length == 0)
            {
                throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' contains an empty line.");
            }

            var reading = _ledgerRecordValidator.ReadCanonicalLine(line);
            if (reading.Record is not { } record)
            {
                throw new InvalidDataException(
                    $"The JSONL ledger '{ledgerPath}' refuses line {lineNumber}: {reading.Refusal}");
            }

            if (!observedEventIds.Add(record.EventId))
            {
                throw new InvalidDataException(
                    $"The evaluation telemetry ledgers contain duplicate event identifier '{record.EventId}'.");
            }
        }
    }

    /// <summary>
    /// Aktualisiert den zwischengespeicherten Bestand nach dem Schreiben,
    /// damit er vor der nächsten Annahme nicht erneut aus den Dateien aufgebaut werden muss.
    /// </summary>
    internal void RecordPersistedIdentity(
        EvaluationTelemetryLedgerRecord ledgerRecord,
        string ledgerPath)
    {
        var incoming = _ledgerRecordValidator.ReadCanonicalLine(ledgerRecord.JsonLine);
        if (incoming.Record is null)
        {
            throw new InvalidDataException(
                $"The evaluation telemetry ledger line to store is refused by its own reader: {incoming.Refusal}");
        }

        _observedSentLines[ledgerRecord.EventId] = incoming.SentLine;
        _observedFileLengths ??= new Dictionary<string, long>(StringComparer.Ordinal);
        _observedFileLengths[ledgerPath] = new FileInfo(ledgerPath).Length;
    }

    private bool ObservedFileSetIsCurrent(string dataDirectory)
    {
        if (_observedFileLengths is null)
        {
            return false;
        }

        if (!Directory.Exists(dataDirectory))
        {
            return _observedFileLengths.Count == 0;
        }

        var seen = 0;
        foreach (var ledgerPath in Directory.EnumerateFiles(
            dataDirectory,
            "*.jsonl",
            SearchOption.AllDirectories))
        {
            seen++;
            if (!_observedFileLengths.TryGetValue(ledgerPath, out var observedLength)
                || observedLength != new FileInfo(ledgerPath).Length)
            {
                return false;
            }
        }

        return seen == _observedFileLengths.Count;
    }

    private async Task RebuildObservedIdentitiesAsync(
        string dataDirectory,
        CancellationToken cancellationToken)
    {
        var sentLines = new Dictionary<string, string>(StringComparer.Ordinal);
        var fileLengths = new Dictionary<string, long>(StringComparer.Ordinal);
        if (Directory.Exists(dataDirectory))
        {
            foreach (var ledgerPath in Directory
                .EnumerateFiles(dataDirectory, "*.jsonl", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal))
            {
                await using var stream = new FileStream(
                    ledgerPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    bufferSize: 4096,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);
                fileLengths[ledgerPath] = stream.Length;
                await ObserveLedgerIdentitiesAsync(stream, sentLines, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        _observedSentLines.Clear();
        foreach (var pair in sentLines)
        {
            _observedSentLines.Add(pair.Key, pair.Value);
        }

        _observedFileLengths = fileLengths;
    }

    private async Task ObserveLedgerIdentitiesAsync(
        FileStream stream,
        Dictionary<string, string> sentLines,
        CancellationToken cancellationToken)
    {
        if (stream.Length == 0 || !await TryHasCanonicalLedgerShapeAsync(stream, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        using var reader = new StreamReader(
            stream,
            Utf8NoBomStrict,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096,
            leaveOpen: true);
        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var reading = _ledgerRecordValidator.ReadCanonicalLine(line);
            if (reading.Record is not { } record)
            {
                continue;
            }

            if (!sentLines.TryAdd(record.EventId, reading.SentLine))
            {
                throw new InvalidDataException(
                    $"The evaluation telemetry ledgers contain duplicate event identifier '{record.EventId}'.");
            }
        }
    }

    private static async Task ValidateLedgerFileShapeAsync(
        FileStream stream,
        string ledgerPath,
        CancellationToken cancellationToken)
    {
        if (stream.Length == 0)
        {
            throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is empty.");
        }

        var buffer = new byte[4096];
        while (await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) is var bytesRead
            && bytesRead > 0)
        {
            if (buffer.AsSpan(0, bytesRead).Contains((byte)'\r'))
            {
                throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' contains a CR byte.");
            }
        }

        if (!EndsWithNewline(stream))
        {
            throw new InvalidDataException($"The JSONL ledger '{ledgerPath}' is not newline-terminated.");
        }

        stream.Position = 0;
    }

    private static async Task<bool> TryHasCanonicalLedgerShapeAsync(
        FileStream stream,
        CancellationToken cancellationToken)
    {
        if (stream.Length == 0)
        {
            return false;
        }

        var buffer = new byte[4096];
        while (await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) is var bytesRead
            && bytesRead > 0)
        {
            if (buffer.AsSpan(0, bytesRead).Contains((byte)'\r'))
            {
                return false;
            }
        }

        if (!EndsWithNewline(stream))
        {
            return false;
        }

        stream.Position = 0;
        return true;
    }

    private static bool EndsWithNewline(FileStream stream)
    {
        stream.Position = stream.Length - 1;
        return stream.ReadByte() == '\n';
    }
}
