namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Bündelt referenzierte Felddefinitionen, Variablendefinitionen und Dialogprofile, damit weitere Prüfungen deren benötigte Schlüssel kennen.
/// </summary>
internal sealed record FormContractBundleConsistencyResult(
    IReadOnlySet<FieldDefinitionsKey> ReferencedFieldDefinitionsKeys,
    IReadOnlySet<VariableDefinitionsKey> ReferencedVariableDefinitionsKeys,
    IReadOnlySet<FormVoiceProfileKey> ReferencedFormVoiceProfileKeys);
