using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Sprechen.ServiceDefaults;

/// <summary>
/// Ergänzt jeden Logdatensatz um den Anwendungsnamen, damit Abfragen
/// die Anwendung direkt über ein Ereignisattribut filtern können.
/// </summary>
internal sealed class ApplicationNameLogRecordEnricher : BaseProcessor<LogRecord>
{
    internal const string ApplicationNameAttributeKey = "ApplicationName";

    private readonly string _applicationName;

    internal ApplicationNameLogRecordEnricher(string applicationName)
    {
        _applicationName = string.IsNullOrWhiteSpace(applicationName)
            ? throw new ArgumentException("An application name is required.", nameof(applicationName))
            : applicationName;
    }

    public override void OnEnd(LogRecord logRecord)
    {
        ArgumentNullException.ThrowIfNull(logRecord);

        var attributes = logRecord.Attributes is { } existing
            ? new List<KeyValuePair<string, object?>>(existing)
            : new List<KeyValuePair<string, object?>>();
        attributes.Add(new KeyValuePair<string, object?>(ApplicationNameAttributeKey, _applicationName));
        logRecord.Attributes = attributes;

        base.OnEnd(logRecord);
    }
}
