using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Variables;

namespace Sprechen.Contracts.Services.Loading;

/// <summary>
/// Bündelt geladene Referenzartefakte und Modelle, damit Vertragsprüfung und Speicher denselben Bestand verwenden können.
/// </summary>
internal sealed record MdaReferenceArtifacts(
    ArtifactSet<FieldDefinitionsKey, FieldDefinitions> FieldDefinitions,
    ArtifactSet<VariableDefinitionsKey, VariableDefinitions> VariableDefinitions,
    ArtifactSet<FormVoiceProfileKey, FormVoiceProfile> FormVoiceProfiles,
    ArtifactSet<SelectionDialogKey, ApplicationSelectionDialog> SelectionDialogs,
    ArtifactSet<ApplicationSelectionDialogProfileKey, ApplicationSelectionDialogProfile> SelectionDialogProfiles,
    MdaJsonArtifact FormsCatalog,
    FormsCatalogDocument FormsCatalogDocument,
    ArtifactSet<FormContractKey, FormContractDocument> FormContracts);
