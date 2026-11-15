using System.Text.Json;

namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Ziel eines <c>$ref</c>: Datei, Definitionsname und Element, damit der Generator den Verweis nicht selbst zerlegt.
/// </summary>
internal readonly record struct SchemaReferenceTarget(
    string SchemaPath,
    string DefinitionName,
    JsonElement SchemaRoot,
    JsonElement Element);
