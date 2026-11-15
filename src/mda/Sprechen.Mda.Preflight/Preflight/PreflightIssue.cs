namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Einzelner Fehler oder eine Warnung der Modellvorprüfung mit stabiler Kennung.
/// </summary>
public sealed record PreflightIssue(
    string Code,
    string Message,
    string? Path = null,
    string? Id = null);

