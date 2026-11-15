using System.Collections.Concurrent;

namespace Sprechen.Common.Cqrs;

/// <summary>
/// Speichert gebundene Handleraufrufe nach Anfragetyp, damit Befehle und Abfragen
/// denselben Mechanismus zur Registrierung und Ausführung nutzen.
/// </summary>
public abstract class CachedDelegateDispatcherBase
{
    private readonly string _requestKind;
    private readonly ConcurrentDictionary<Type, CachedDelegateHandlerRegistration> _handlers = new();

    protected CachedDelegateDispatcherBase(string requestKind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestKind);

        _requestKind = requestKind;
    }

    protected void RegisterCore<TRequest, TResult>(
        Type requestType,
        Func<TRequest, CancellationToken, Task<TResult>> handleAsync)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentNullException.ThrowIfNull(handleAsync);

        _handlers[requestType] = new CachedDelegateHandlerRegistration(
            typeof(TResult),
            async (request, cancellationToken) =>
            {
                var task = handleAsync((TRequest)request, cancellationToken);
                if (task is null)
                {
                    throw new InvalidOperationException($"Handler for '{requestType.Name}' returned null task.");
                }

                var result = await task.ConfigureAwait(false);
                return result;
            });
    }

    protected async Task<TResult> DispatchCoreAsync<TResult>(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        if (!_handlers.TryGetValue(requestType, out var registration))
        {
            throw new InvalidOperationException($"No handler registered for {_requestKind} type '{requestType.Name}'.");
        }

        if (registration.ResultType != typeof(TResult))
        {
            throw new InvalidOperationException(
                $"Handler for '{requestType.Name}' is registered for result type '{registration.ResultType.Name}', but '{typeof(TResult).Name}' was requested.");
        }

        var result = await registration.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        if (result is TResult typedResult)
        {
            return typedResult;
        }

        if (result is null && default(TResult) is null)
        {
            return default!;
        }

        throw new InvalidOperationException(
            $"Handler for '{requestType.Name}' returned '{result?.GetType().Name ?? "<null>"}', expected '{typeof(TResult).Name}'.");
    }
}


