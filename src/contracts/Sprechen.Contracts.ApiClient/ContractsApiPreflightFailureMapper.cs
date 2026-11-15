using Sprechen.Mda.Contracts.V1.Preflight;

namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Wandelt Fehler der Vertragsprüfung in Ausnahmen um, damit Aufrufer ungültige Verträge nicht weiterverarbeiten.
/// </summary>
internal sealed class ContractsApiPreflightFailureMapper
{
    public void ThrowInvalidOperationIfFailed(PreflightResult preflight, string subject)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (!preflight.Ok)
        {
            throw new InvalidOperationException($"{subject} preflight failed: {PreflightIssueFormatter.Format(preflight.Errors)}");
        }
    }

    public void ThrowContractsPreflightFailedIfFailed(PreflightResult preflight, string subject)
    {
        ArgumentNullException.ThrowIfNull(preflight);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        if (!preflight.Ok)
        {
            throw new ContractsPreflightFailedException(
                $"{subject} preflight failed: {PreflightIssueFormatter.Format(preflight.Errors)}",
                preflight.Errors);
        }
    }
}
