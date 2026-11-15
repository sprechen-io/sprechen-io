namespace Sprechen.Common.Cqrs;

/// <summary>
/// Verknüpft einen Handleraufruf mit seinem Ergebnistyp, damit der Dispatcher
/// den angeforderten Rückgabetyp vor der Ausführung prüfen kann.
/// </summary>
internal sealed record CachedDelegateHandlerRegistration(
    Type ResultType,
    Func<object, CancellationToken, Task<object?>> InvokeAsync);

