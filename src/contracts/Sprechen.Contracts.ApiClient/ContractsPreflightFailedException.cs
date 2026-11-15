using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Bewahrt einzelne Fehler der Vertragsprüfung auf, damit Aufrufer sie neben der zusammengefassten Fehlermeldung auswerten können.
/// </summary>
public sealed class ContractsPreflightFailedException : InvalidOperationException
{
    public ContractsPreflightFailedException(string message, IReadOnlyList<PreflightIssue> issues)
        : base(message)
    {
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
    }

    public IReadOnlyList<PreflightIssue> Issues { get; }
}
