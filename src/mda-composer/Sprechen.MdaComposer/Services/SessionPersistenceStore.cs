namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Speichert Dokument, Schemaauswahl und Vorgabelisten im Browser, damit die Bearbeitung nach einem Neuladen fortgesetzt werden kann.
/// </summary>
public sealed class SessionPersistenceStore
{
    private const string SchemaKey = "mda-composer:schema";
    private const string DocumentKey = "mda-composer:document";
    private const string AuthoringValuePresetsKey = "mda-composer:authoring-value-presets";

    private readonly ComposerJsInterop _js;
    private readonly AuthoringValuePresetPersistenceMapper _presetMapper = new();

    public SessionPersistenceStore(ComposerJsInterop js)
    {
        _js = js;
    }

    public async Task SaveSessionAsync(string? schemaFileName, string? documentJson)
    {
        try
        {
            await SaveOptionalStateAsync(SchemaKey, schemaFileName);
            await SaveOptionalStateAsync(DocumentKey, documentJson);
        }
        catch
        {
            // Speicherfehler werden abgefangen, damit die aktuelle Bearbeitung fortgesetzt werden kann.
        }
    }

    public async Task<(string? SchemaFileName, string? DocumentJson)> LoadSessionAsync()
    {
        try
        {
            var schema = await _js.LoadStateAsync(SchemaKey);
            var document = await _js.LoadStateAsync(DocumentKey);
            return (schema, document);
        }
        catch
        {
            return (null, null);
        }
    }

    public async Task SaveAuthoringValuePresetsAsync(IReadOnlyList<SerializableAuthoringValuePreset> presets)
    {
        try
        {
            var json = _presetMapper.Serialize(presets);
            await _js.SaveStateAsync(AuthoringValuePresetsKey, json);
        }
        catch
        {
            // Das Speichern der Vorgaben darf bei einem Browserfehler die Bearbeitung nicht unterbrechen.
        }
    }

    public async Task<IReadOnlyList<SerializableAuthoringValuePreset>> LoadAuthoringValuePresetsAsync()
    {
        try
        {
            var json = await _js.LoadStateAsync(AuthoringValuePresetsKey);
            return _presetMapper.Deserialize(json);
        }
        catch
        {
            // Nicht lesbare Vorgaben werden durch eine leere Liste ersetzt.
        }

        return [];
    }

    public async Task ClearSessionAsync()
    {
        try
        {
            await _js.RemoveStateAsync(SchemaKey);
            await _js.RemoveStateAsync(DocumentKey);
        }
        catch
        {
        }
    }

    private async Task SaveOptionalStateAsync(string key, string? value)
    {
        if (value is not null)
        {
            await _js.SaveStateAsync(key, value);
            return;
        }

        await _js.RemoveStateAsync(key);
    }
}
