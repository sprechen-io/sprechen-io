using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.MdaFormAccess;

/// <summary>
/// Definiert den Zugriff auf eine bestimmte Formularvertragsversion, damit Forms und Voice Verträge unabhängig vom Transport laden können.
/// </summary>
public interface IMdaFormContractDataClient
{
    Task<FormContract> GetFormContractAsync(
        string applicationId,
        string formId,
        string contractVersion,
        CancellationToken cancellationToken);
}
