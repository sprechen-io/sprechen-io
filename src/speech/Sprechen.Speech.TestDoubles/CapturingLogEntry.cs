using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.TestDoubles;

public sealed record CapturingLogEntry(LogLevel Level, IReadOnlyDictionary<string, object?> Values);

