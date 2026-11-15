namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Navigationsposition für die Auswertungstelemetrie mit aktuellem Feld,
/// Feldgruppe, normalisiertem Positionsindex und Gesamtzahl.
/// </summary>
public sealed record EvaluationFormNavigation(
    string? CurrentFieldId,
    string CurrentFieldGroupId,
    int Index,
    int Total);
