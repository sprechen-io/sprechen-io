using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

internal sealed record StructuredLogEntry(LogLevel Level, IReadOnlyDictionary<string, object?> Values, EventId EventId = default);

