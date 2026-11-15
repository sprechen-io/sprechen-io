namespace Sprechen.Speech.App.Contracts.Evaluation;

/// <summary>
/// Belegt die dauerhafte Annahme des unveränderlichen <c>task_end</c>-Umschlags
/// durch die Telemetrieannahme. Der Beleg ordnet Sitzungskennung, Ereigniskennung
/// und Inhaltsprüfsumme der noch festzuschreibenden Anwendungstransaktion zu.
/// </summary>
public sealed record EvaluationSessionAcceptance(
    string SessionId,
    string EventId,
    string BodySha256)
{
    public string SessionId { get; } = Require(SessionId, nameof(SessionId));

    public string EventId { get; } = Require(EventId, nameof(EventId));

    public string BodySha256 { get; } = Require(BodySha256, nameof(BodySha256));

    private static string Require(string value, string name)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("An evaluation acceptance member is required.", name)
            : value.Trim();
}
