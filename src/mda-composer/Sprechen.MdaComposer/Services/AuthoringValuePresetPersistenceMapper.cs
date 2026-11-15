using System.Text.Json;

namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Serialisiert Vorgabelisten als JSON und liest sie zurück, damit sie im Browserspeicher abgelegt werden können.
/// </summary>
internal sealed class AuthoringValuePresetPersistenceMapper
{
    public string Serialize(IReadOnlyList<SerializableAuthoringValuePreset> presets)
        => JsonSerializer.Serialize(presets);

    public IReadOnlyList<SerializableAuthoringValuePreset> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<SerializableAuthoringValuePreset>>(json) ?? [];
    }
}
