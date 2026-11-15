namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Verwaltet Vorgabelisten nach Schema, JSON-Pointer und Bezeichnung, damit die Oberfläche passende Werte anbieten kann.
/// </summary>
public sealed class AuthoringValuePresetCatalog
{
    private readonly Dictionary<string, AuthoringValuePreset> _presets = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AuthoringValuePreset> All
        => _presets.Values
            .OrderBy(static preset => preset.RootSchemaFileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static preset => preset.JsonPointer, StringComparer.Ordinal)
            .ThenBy(static preset => preset.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public IReadOnlyList<AuthoringValuePreset> GetPresets(string rootSchemaFileName, string jsonPointer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootSchemaFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPointer);

        return _presets.Values
            .Where(preset =>
                preset.RootSchemaFileName.Equals(rootSchemaFileName, StringComparison.OrdinalIgnoreCase)
                && preset.JsonPointer.Equals(jsonPointer, StringComparison.Ordinal))
            .OrderBy(static preset => preset.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void ReplaceAll(IEnumerable<AuthoringValuePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        _presets.Clear();
        foreach (var preset in presets)
        {
            AddPreset(
                preset.RootSchemaFileName,
                preset.JsonPointer,
                preset.Label,
                preset.Values,
                preset.Description);
        }
    }

    public void AddPreset(
        string rootSchemaFileName,
        string jsonPointer,
        string label,
        IEnumerable<string> values,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootSchemaFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(jsonPointer);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentNullException.ThrowIfNull(values);

        var normalizedValues = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedValues.Count == 0)
        {
            throw new InvalidOperationException("At least one preset value is required.");
        }

        var preset = new AuthoringValuePreset(
            rootSchemaFileName,
            jsonPointer,
            label.Trim(),
            normalizedValues,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim());

        _presets[BuildKey(preset.RootSchemaFileName, preset.JsonPointer, preset.Label)] = preset;
    }

    public void RemovePreset(string rootSchemaFileName, string jsonPointer, string label)
    {
        _presets.Remove(BuildKey(rootSchemaFileName, jsonPointer, label));
    }

    private static string BuildKey(string rootSchemaFileName, string jsonPointer, string label)
        => $"{rootSchemaFileName}|{jsonPointer}|{label}";
}
