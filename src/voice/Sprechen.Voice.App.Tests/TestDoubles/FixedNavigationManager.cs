using Microsoft.AspNetCore.Components;

namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class FixedNavigationManager : NavigationManager
{
    public FixedNavigationManager(string baseUri)
        : this(baseUri, baseUri)
    {
    }

    public FixedNavigationManager(string baseUri, string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        Initialize(baseUri, uri);
    }

    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
    }
}
