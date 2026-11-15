namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Kennzeichnet Variablendefinitionen durch Kennung und Katalogversion, damit ihre Schlüssel nicht mit anderen Artefaktarten verwechselt werden.
/// </summary>
internal readonly record struct VariableDefinitionsKey(string VariableDefinitionsId, string CatalogVersion);
