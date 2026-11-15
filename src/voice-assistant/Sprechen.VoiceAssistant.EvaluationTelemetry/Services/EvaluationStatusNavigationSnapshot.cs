namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bewahrt die zuletzt gemeldete Navigation zum Vergleich, damit unveränderte
/// Fokus- und Positionsangaben kein zusätzliches Navigationsereignis erzeugen.
/// </summary>
internal sealed record EvaluationStatusNavigationSnapshot(
    string? FieldId,
    string FieldGroupId,
    int NavigationIndex,
    int NavigationTotal);
