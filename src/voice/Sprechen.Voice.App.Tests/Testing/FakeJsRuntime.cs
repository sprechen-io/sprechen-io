using Microsoft.JSInterop;

namespace Sprechen.Voice.App.Tests.Testing;

internal sealed class FakeJsRuntime : IJSRuntime
{
    private readonly Dictionary<string, object?> _results = new(StringComparer.Ordinal);

    public void SetResult<TValue>(string identifier, TValue value)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier is required.", nameof(identifier));
        }

        _results[identifier] = value;
    }

    public void SetVoidResult(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier is required.", nameof(identifier));
        }

        _results[identifier] = null;
    }

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier is required.", nameof(identifier));
        }

        if (!_results.TryGetValue(identifier, out var result))
        {
            throw new InvalidOperationException($"No JS result configured for '{identifier}'.");
        }

        if (result is null)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        if (result is TValue typed)
        {
            return new ValueTask<TValue>(typed);
        }

        throw new InvalidOperationException($"JS result for '{identifier}' has type '{result.GetType().Name}', expected '{typeof(TValue).Name}'.");
    }
}


