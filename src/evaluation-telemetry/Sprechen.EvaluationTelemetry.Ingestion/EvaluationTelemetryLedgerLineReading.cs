using System.Text;

namespace Sprechen.EvaluationTelemetry.Ingestion;

/// <summary>
/// Enthält einen gelesenen Datensatz oder einen konkreten Ablehnungsgrund,
/// damit ungültige Archivzeilen mit ihrer Ursache gemeldet werden können.
/// </summary>
public sealed class EvaluationTelemetryLedgerLineReading
{
    private readonly Func<string>? _sentLine;

    private EvaluationTelemetryLedgerLineReading(
        EvaluationTelemetryLedgerRecord? record,
        Func<string>? sentLine,
        string refusal)
    {
        Record = record;
        _sentLine = sentLine;
        Refusal = refusal;
    }

    /// <summary>
    /// Der gelesene Datensatz. Bei einer Ablehnung ist der Wert null.
    /// </summary>
    public EvaluationTelemetryLedgerRecord? Record { get; }

    /// <summary>
    /// Kanonische Ereignisdaten ohne Empfangszeitpunkt des Servers.
    /// Diese Form dient dem Inhaltsvergleich wiederholter Zustellungen.
    /// </summary>
    public string SentLine => _sentLine is null
        ? throw new InvalidOperationException("A refused evaluation telemetry ledger line has no sent line.")
        : _sentLine();

    /// <summary>
    /// Der Ablehnungsgrund. Bei erfolgreich gelesenen Zeilen leer.
    /// </summary>
    public string Refusal { get; }

    /// <summary>
    /// Hält den gelesenen Datensatz und erzeugt seine Vergleichsform erst beim Zugriff,
    /// um unnötige Serialisierungen zu vermeiden.
    /// </summary>
    public static EvaluationTelemetryLedgerLineReading Read(
        EvaluationTelemetryLedgerRecord record,
        Func<string> sentLine)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(sentLine);

        return new EvaluationTelemetryLedgerLineReading(record, sentLine, string.Empty);
    }

    /// <summary>
    /// Formatiert Ort, Regel und Grund einer Ablehnung wie die Meldungen der Schemaprüfung.
    /// </summary>
    public static EvaluationTelemetryLedgerLineReading Refused(string instanceLocation, string rule, string reason)
    {
        return Refused($"{instanceLocation} {rule}: {reason}");
    }

    /// <summary>
    /// Liefert eine Ablehnung mit dem bereits zusammengefassten Grund und ohne Datensatz.
    /// </summary>
    public static EvaluationTelemetryLedgerLineReading Refused(string refusal)
    {
        if (string.IsNullOrWhiteSpace(refusal))
        {
            throw new ArgumentException("A refused ledger line must name what refused it.", nameof(refusal));
        }

        return new EvaluationTelemetryLedgerLineReading(
            record: null,
            sentLine: null,
            CollapseToOneLine(refusal));
    }

    /// <summary>
    /// Fasst den Ablehnungsgrund auf einer Zeile zusammen,
    /// damit Dateipfad und Zeilennummer in derselben Meldung lesbar bleiben.
    /// </summary>
    private static string CollapseToOneLine(string refusal)
    {
        var collapsed = new StringBuilder(refusal.Length);
        var pendingSeparator = false;
        foreach (var character in refusal)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSeparator = collapsed.Length > 0;
                continue;
            }

            if (pendingSeparator)
            {
                collapsed.Append(' ');
                pendingSeparator = false;
            }

            collapsed.Append(character);
        }

        return collapsed.ToString();
    }
}
