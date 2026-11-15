using Bunit;

namespace Sprechen.Voice.App.Tests.Testing;

internal static class BunitJsInteropExtensions
{
    public static void VerifyNotInvoked(this BunitJSInterop jsInterop, string identifier)
    {
        ArgumentNullException.ThrowIfNull(jsInterop);

        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier is required.", nameof(identifier));
        }

        JSRuntimeAssertExtensions.VerifyNotInvoke(jsInterop, identifier, string.Empty);
    }
}

