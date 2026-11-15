using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Forms.Services;

/// <summary>
/// Meldet einen Fehler beim Laden eines Formularvertrags und enthält
/// die zugehörigen Preflight-Befunde des Contracts-Dienstes.
/// </summary>
internal sealed class FormContractLoadFailedException : InvalidOperationException
{
    public FormContractLoadFailedException(string message, IReadOnlyList<PreflightIssue> issues)
        : base(message)
    {
        Issues = issues ?? throw new ArgumentNullException(nameof(issues));
    }

    public IReadOnlyList<PreflightIssue> Issues { get; }
}
