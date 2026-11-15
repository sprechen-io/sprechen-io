namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed record RecordedTelemetryCall(string Method, IReadOnlyList<string> StringArguments);
