using Microsoft.JSInterop;

namespace Sprechen.Voice.App.Tests.EvaluationTelemetry;

internal sealed class RecordingInProcessJavascript : IJSInProcessRuntime
{
    internal string? Identifier { get; private set; }

    internal object?[] Arguments { get; private set; } = [];

    public TValue Invoke<TValue>(string identifier, params object?[]? args)
    {
        Identifier = identifier;
        Arguments = args ?? [];
        return (TValue)(object)Array.Empty<string>();
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromResult(Invoke<TValue>(identifier, args));

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
        => ValueTask.FromResult(Invoke<TValue>(identifier, args));
}
