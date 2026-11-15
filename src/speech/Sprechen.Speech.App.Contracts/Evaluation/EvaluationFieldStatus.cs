using System.Collections.Immutable;

namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Status eines Feldes für die Auswertungstelemetrie: Zugehörigkeit,
/// Relevanz, Pflichtstatus, Abschluss und blockierende Fehlercodes.
/// </summary>
public sealed record EvaluationFieldStatus(
    string FieldId,
    string FieldGroupId,
    bool Relevant,
    bool Required,
    bool Completed,
    ImmutableArray<string> BlockingErrorCodes);
