namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Kennzeichnet Felddefinitionen durch Kennung und Katalogversion, damit ihre Schlüssel nicht mit denen anderer Artefaktarten verwechselt werden.
/// </summary>
internal readonly record struct FieldDefinitionsKey(string FieldDefinitionsId, string CatalogVersion);
