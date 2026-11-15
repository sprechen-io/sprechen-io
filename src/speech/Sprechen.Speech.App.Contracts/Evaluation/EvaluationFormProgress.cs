namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Abschlussfortschritt für die Auswertungstelemetrie, getrennt nach
/// allen Feldern, Pflichtfeldern und Feldgruppen.
/// </summary>
public sealed record EvaluationFormProgress(
    EvaluationFormProgressCounter AllFields,
    EvaluationFormProgressCounter RequiredFieldsOnly,
    EvaluationFormProgressCounter FieldGroupBased);
