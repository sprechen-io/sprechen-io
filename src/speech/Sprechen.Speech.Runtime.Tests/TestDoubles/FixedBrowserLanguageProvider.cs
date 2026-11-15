
namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class FixedBrowserLanguageProvider : IBrowserLanguageProvider
{
    private readonly string[] _languageTags;

    public FixedBrowserLanguageProvider(params string?[] languageTags)
    {
        ArgumentNullException.ThrowIfNull(languageTags);

        _languageTags = languageTags
            .Where(languageTag => !string.IsNullOrWhiteSpace(languageTag))
            .Select(languageTag => languageTag!.Trim())
            .ToArray();
    }

    public ValueTask<IReadOnlyList<string>> GetPreferredLanguageTagsAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult<IReadOnlyList<string>>(_languageTags);
}
