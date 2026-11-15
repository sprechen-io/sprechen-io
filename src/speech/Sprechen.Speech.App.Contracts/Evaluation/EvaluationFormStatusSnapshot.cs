namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Formularstand für die Auswertungstelemetrie mit Navigation, Fortschritt
/// und offenen Pflichtfeldern beziehungsweise Validierungsfehlern.
/// Der Dialogadapter bildet seinen <c>FormStatusSnapshot</c> auf diesen eigenen Vertrag ab.
/// </summary>
public sealed record EvaluationFormStatusSnapshot(
    EvaluationFormNavigation Navigation,
    EvaluationFormProgress Progress,
    int OpenRequiredFieldCount,
    int ValidationErrorCount);
