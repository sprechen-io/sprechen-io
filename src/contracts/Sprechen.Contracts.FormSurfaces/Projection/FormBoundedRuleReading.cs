namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Beschreibt die Namen und das Format von Regelgrenzen, damit ein- und zweiseitige Begrenzungen gleich ausgewertet werden.
/// </summary>
internal sealed record FormBoundedRuleReading(
    string? LowerBoundName,
    string? UpperBoundName,
    bool BoundsAreCalendarDates);
