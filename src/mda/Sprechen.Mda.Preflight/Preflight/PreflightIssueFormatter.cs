namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Formatiert Vorprüfungsbefunde als kompakte Liste ihrer Kennungen und Fundstellen.
/// </summary>
public static class PreflightIssueFormatter
{
    /// <summary>
    /// Verbindet die Befunde mit Semikolon. Je Befund werden Code, optionale Kennung
    /// in eckigen Klammern und optionaler Pfad nach <c>@</c> ausgegeben.
    /// </summary>
    public static string Format(IEnumerable<PreflightIssue> issues)
    {
        return string.Join(
            "; ",
            issues.Select(issue =>
                $"{issue.Code}{(string.IsNullOrWhiteSpace(issue.Id) ? string.Empty : $"[{issue.Id}]")}{(string.IsNullOrWhiteSpace(issue.Path) ? string.Empty : $"@{issue.Path}")}"));
    }
}
