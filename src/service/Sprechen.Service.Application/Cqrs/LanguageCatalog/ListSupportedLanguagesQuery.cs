using Sprechen.Common.Cqrs;

namespace Sprechen.Service.Application.Cqrs.LanguageCatalog;

/// <summary>
/// Abfrage der unterstützten Sprachen und der Standardsprache.
/// </summary>
public sealed record ListSupportedLanguagesQuery : IQuery<SupportedLanguagesResult>;
