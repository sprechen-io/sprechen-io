using System.Text.Json;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Verknüpft Name, Teilschema und Pflichtstatus einer Eigenschaft für den Aufbau des Schemaknotens.
/// </summary>
internal sealed record SchemaElementPropertySchema(string Name, JsonElement Element, bool IsRequired);
