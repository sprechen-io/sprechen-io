
namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpBrowserLanguageProvider : IBrowserLanguageProvider
{
    public ValueTask<IReadOnlyList<string>> GetPreferredLanguageTagsAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
}
