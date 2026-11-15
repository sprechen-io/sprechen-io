namespace Sprechen.Contracts.ApiClient;

/// <summary>
/// Erzeugt Ressourcenpfade mit URL-kodierten Kennungen, damit Schrägstriche innerhalb einer Kennung keine zusätzlichen Pfadsegmente bilden.
/// </summary>
public sealed class ContractsApiResourceRoutes
{
    public string FormsCatalog() => ContractsApiRoutes.FormsCatalogTemplate;

    public string FormContract(string applicationId, string formId, string contractVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(formId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contractVersion);

        return $"/contracts/{Encode(applicationId)}/{Encode(formId)}/{Encode(contractVersion)}";
    }

    public string FieldDefinitions(string fieldDefinitionsId, string catalogVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldDefinitionsId);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogVersion);

        return $"/contracts/field-definitions/{Encode(fieldDefinitionsId)}/{Encode(catalogVersion)}";
    }

    public string VariableDefinitions(string variableDefinitionsId, string catalogVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variableDefinitionsId);
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogVersion);

        return $"/contracts/variable-definitions/{Encode(variableDefinitionsId)}/{Encode(catalogVersion)}";
    }

    public string FormVoiceProfile(string formVoiceProfileId, string profileVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(formVoiceProfileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileVersion);

        return $"/contracts/form-voice-profiles/{Encode(formVoiceProfileId)}/{Encode(profileVersion)}";
    }

    public string ApplicationSelectionDialog(string selectionDialogId, string modelVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(selectionDialogId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelVersion);

        return $"/contracts/application-selection-dialogs/{Encode(selectionDialogId)}/{Encode(modelVersion)}";
    }

    public string ApplicationSelectionDialogProfile(string profileId, string profileVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(profileVersion);

        return $"/contracts/application-selection-dialog-profiles/{Encode(profileId)}/{Encode(profileVersion)}";
    }

    private static string Encode(string value)
    {
        return Uri.EscapeDataString(value.Trim());
    }
}
