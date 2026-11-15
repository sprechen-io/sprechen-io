using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Bündelt Zuordnungen von Kennungen zu Variablen, Feldern und Feldgruppen, damit die Projektion direkt auf diese zugreifen kann.
/// </summary>
internal sealed record FormSurfaceProjectionIndex(
    IReadOnlyDictionary<string, VariableDefinition> VariablesById,
    IReadOnlyDictionary<string, FieldDefinition> FieldsById,
    IReadOnlyDictionary<string, FieldGroupDefinition> FieldGroupsById);
