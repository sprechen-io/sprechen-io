using Microsoft.JSInterop;
using Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

namespace Sprechen.Voice.App.Tests.EvaluationTelemetry;

internal sealed class RecordingEvaluationTelemetryDeliverJavascript : IJSRuntime
{
    private readonly Func<CancellationToken, EvaluationTelemetryBrowserDeliveryResult> _respond;
    private readonly bool _hangUntilCanceled;
    private readonly bool _throwJsException;

    internal RecordingEvaluationTelemetryDeliverJavascript(
        EvaluationTelemetryBrowserDeliveryResult result)
        : this(_ => result, hangUntilCanceled: false, throwJsException: false)
    {
    }

    private RecordingEvaluationTelemetryDeliverJavascript(
        Func<CancellationToken, EvaluationTelemetryBrowserDeliveryResult> respond,
        bool hangUntilCanceled,
        bool throwJsException)
    {
        _respond = respond;
        _hangUntilCanceled = hangUntilCanceled;
        _throwJsException = throwJsException;
    }

    internal static RecordingEvaluationTelemetryDeliverJavascript HangUntilCanceled()
        => new(_ => new EvaluationTelemetryBrowserDeliveryResult { Status = 204 }, hangUntilCanceled: true, throwJsException: false);

    internal static RecordingEvaluationTelemetryDeliverJavascript ThrowJsException()
        => new(_ => new EvaluationTelemetryBrowserDeliveryResult { Status = 204 }, hangUntilCanceled: false, throwJsException: true);

    internal string? Identifier { get; private set; }

    internal object?[] Arguments { get; private set; } = [];

    internal int InvokeCount { get; private set; }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public async ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        Identifier = identifier;
        Arguments = args ?? [];
        InvokeCount++;
        if (_hangUntilCanceled)
        {
            var completion = new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using var registration = cancellationToken.Register(
                static state => ((TaskCompletionSource<TValue>)state!).TrySetCanceled(),
                completion);
            return await completion.Task.ConfigureAwait(false);
        }

        if (_throwJsException)
        {
            throw new JSException("evaluationTelemetryOutbox: delivery not acknowledged");
        }

        var result = _respond(cancellationToken);
        if (result is TValue typed)
        {
            return typed;
        }

        throw new InvalidOperationException(
            $"JS result has type '{result.GetType().Name}', expected '{typeof(TValue).Name}'.");
    }
}
