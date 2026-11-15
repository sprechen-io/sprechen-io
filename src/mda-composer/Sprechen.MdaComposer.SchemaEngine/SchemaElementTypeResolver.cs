using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Bestimmt den Schematyp aus einer Deklaration oder aus Strukturmerkmalen für Anzeige und Validierung.
/// </summary>
internal sealed class SchemaElementTypeResolver
{
    private readonly SchemaElementDeclaredTypeResolver _declaredTypeResolver = new();
    private readonly SchemaElementStructuralTypeInferrer _structuralTypeInferrer = new();

    public SchemaNodeType Resolve(JsonElement element)
    {
        return _declaredTypeResolver.TryResolve(element, out var declaredType)
            ? declaredType
            : _structuralTypeInferrer.Infer(element);
    }
}
