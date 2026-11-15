using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Erzeugt typisierte Artefaktschlüssel ohne äußere Leerzeichen, damit abweichende Randabstände die Zuordnung nicht verändern.
/// </summary>
internal static class MdaArtifactKeyFactory
{
    public static FieldDefinitionsKey CreateFieldDefinitionsKey(
        string fieldDefinitionsId,
        string catalogVersion)
    {
        return new FieldDefinitionsKey(Normalize(fieldDefinitionsId), Normalize(catalogVersion));
    }

    public static VariableDefinitionsKey CreateVariableDefinitionsKey(
        string variableDefinitionsId,
        string catalogVersion)
    {
        return new VariableDefinitionsKey(Normalize(variableDefinitionsId), Normalize(catalogVersion));
    }

    public static FormVoiceProfileKey CreateFormVoiceProfileKey(
        string formVoiceProfileId,
        string profileVersion)
    {
        return new FormVoiceProfileKey(Normalize(formVoiceProfileId), Normalize(profileVersion));
    }

    public static SelectionDialogKey CreateSelectionDialogKey(
        string selectionDialogId,
        string modelVersion)
    {
        return new SelectionDialogKey(Normalize(selectionDialogId), Normalize(modelVersion));
    }

    public static ApplicationSelectionDialogProfileKey CreateApplicationSelectionDialogProfileKey(
        string profileId,
        string profileVersion)
    {
        return new ApplicationSelectionDialogProfileKey(Normalize(profileId), Normalize(profileVersion));
    }

    public static FormContractKey CreateContractKey(
        string applicationId,
        string formId,
        string contractVersion)
    {
        return new FormContractKey(Normalize(applicationId), Normalize(formId), Normalize(contractVersion));
    }

    private static string Normalize(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return value.Trim();
    }
}
