using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Feld- und Feldgruppenstand für die Auswertungstelemetrie.
/// Aufeinanderfolgende Momentaufnahmen dienen zur Erkennung von Statusänderungen.
/// Dieser Vertrag ist vom Dialogtyp <c>FormDetailedStatusSnapshot</c> getrennt.
/// </summary>
public sealed record EvaluationDetailedFormStatus(
    ImmutableArray<EvaluationFieldStatus> Fields,
    ImmutableArray<EvaluationFieldGroupStatus> FieldGroups);
