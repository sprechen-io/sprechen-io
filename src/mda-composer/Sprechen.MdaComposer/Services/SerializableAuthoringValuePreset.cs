namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Beschreibt eine speicherbare Vorgabeliste mit Schema und JSON-Pointer, um ihre Zuordnung beim Wiederherstellen zu erhalten.
/// </summary>
public sealed record SerializableAuthoringValuePreset(
    string RootSchemaFileName,
    string JsonPointer,
    string Label,
    List<string> Values,
    string? Description);
