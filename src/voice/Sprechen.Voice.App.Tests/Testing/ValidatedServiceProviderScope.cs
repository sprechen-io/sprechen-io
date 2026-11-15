using Microsoft.Extensions.DependencyInjection;

namespace Sprechen.Voice.App.Tests.Testing;

internal sealed class ValidatedServiceProviderScope : IDisposable, IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private bool _disposed;

    private ValidatedServiceProviderScope(ServiceProvider provider, IServiceScope scope)
    {
        _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
    }

    public IServiceProvider ServiceProvider => _scope.ServiceProvider;

    public static ValidatedServiceProviderScope Create(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        return new ValidatedServiceProviderScope(provider, provider.CreateScope());
    }

    public T GetRequiredService<T>()
        where T : notnull
        => ServiceProvider.GetRequiredService<T>();

    public void Dispose()
        => DisposeAsync().AsTask().GetAwaiter().GetResult();

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_scope is IAsyncDisposable asyncScope)
        {
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _scope.Dispose();
        }

        await _provider.DisposeAsync().ConfigureAwait(false);
    }
}
