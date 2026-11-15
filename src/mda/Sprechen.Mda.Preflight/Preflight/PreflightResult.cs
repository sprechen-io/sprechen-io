namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Gesammelte Fehler und Warnungen einer Modellvorprüfung.
/// </summary>
public sealed class PreflightResult
{
    /// <summary>
    /// Übernimmt Fehler und optionale Warnungen. Fehlende Warnungen werden als leere Liste behandelt.
    /// </summary>
    public PreflightResult(IReadOnlyList<PreflightIssue> errors, IReadOnlyList<PreflightIssue>? warnings = null)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        Warnings = warnings ?? Array.Empty<PreflightIssue>();
    }

    /// <summary>
    /// Ob keine Fehler vorliegen. Warnungen beeinflussen diesen Wert nicht.
    /// </summary>
    public bool Ok => Errors.Count == 0;

    /// <summary>
    /// Befunde, die eine erfolgreiche Vorprüfung verhindern.
    /// </summary>
    public IReadOnlyList<PreflightIssue> Errors { get; }

    /// <summary>
    /// Warnungen, die eine erfolgreiche Vorprüfung nicht verhindern.
    /// </summary>
    public IReadOnlyList<PreflightIssue> Warnings { get; }
}
