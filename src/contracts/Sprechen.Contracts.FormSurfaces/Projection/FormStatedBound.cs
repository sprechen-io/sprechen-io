namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Hält Vergleichswert und Markupdarstellung einer Regelgrenze zusammen, damit beide auf derselben Auswertung beruhen.
/// </summary>
internal sealed record FormStatedBound(long Ordinal, string MarkupText);
