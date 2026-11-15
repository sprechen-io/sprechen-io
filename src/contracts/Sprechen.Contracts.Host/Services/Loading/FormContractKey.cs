namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Kennzeichnet Formularverträge durch Anwendung, Formular und Version, damit ihre Schlüssel nicht mit anderen Artefaktarten verwechselt werden.
/// </summary>
internal readonly record struct FormContractKey(string ApplicationId, string FormId, string ContractVersion);
