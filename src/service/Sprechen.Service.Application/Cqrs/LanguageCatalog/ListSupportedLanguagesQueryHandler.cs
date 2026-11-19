using Monstertacle.SharpQrs;

namespace Sprechen.Service.Application.Cqrs.LanguageCatalog;

/// <summary>
/// Übernimmt unterstützte Sprachen und Standardsprache aus dem Sprachdienst in das Abfrageergebnis.
/// </summary>
internal sealed class ListSupportedLanguagesQueryHandler
    : IQueryHandler<ListSupportedLanguagesQuery, SupportedLanguagesResult>
{
    private readonly ISupportedLanguagesService _supportedLanguages;

    public ListSupportedLanguagesQueryHandler(ISupportedLanguagesService supportedLanguages)
    {
        _supportedLanguages = supportedLanguages ?? throw new ArgumentNullException(nameof(supportedLanguages));
    }

    public Task<SupportedLanguagesResult> HandleAsync(
        ListSupportedLanguagesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return Task.FromResult(
            new SupportedLanguagesResult(_supportedLanguages.DefaultIsoCode, _supportedLanguages.Languages));
    }
}
