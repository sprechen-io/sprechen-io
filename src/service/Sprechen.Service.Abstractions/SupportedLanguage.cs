namespace Sprechen.Service.Abstractions;

/// <summary>
/// Sprachcode mit zugehörigem Anzeigenamen.
/// </summary>
public sealed record SupportedLanguage(string IsoCode, string DisplayName);
