namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class FixedSupportedLanguagesClient(SupportedLanguages supportedLanguages) : ISupportedLanguagesClient
{
    private readonly SupportedLanguages _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));

    public Task<SupportedLanguages> GetAsync(CancellationToken ct)
    {
        var clone = new SupportedLanguages(
            _supportedLanguages.DefaultIsoCode,
            _supportedLanguages.Languages.ToArray());

        return Task.FromResult(clone);
    }
}
