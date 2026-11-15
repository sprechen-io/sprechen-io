using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.TestDoubles;

public sealed record RecordedLogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception);
