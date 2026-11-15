namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Enthält auswählbare Vorgabewerte für eine Position im Schema, um wiederholte Eingaben beim Bearbeiten zu erleichtern.
/// </summary>
public sealed record AuthoringValuePreset(
    string RootSchemaFileName,
    string JsonPointer,
    string Label,
    IReadOnlyList<string> Values,
    string? Description = null);
