using System.Text.Json;
using Sprechen.VoiceAssistant.EvaluationTelemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Telemetry;

/// <summary>
/// Speichert im Test alle unterschiedlichen Ereignishüllen aus geschriebenen Zwischenständen. So bleiben ihre Inhalte auch nach einer Empfangsbestätigung prüfbar.
/// </summary>
internal sealed class RecordingCheckpointStore : IEvaluationTelemetryOutboxStore
{
    private readonly Dictionary<string, string> _checkpoints = new(StringComparer.Ordinal);
    private readonly List<string> _bodies = [];

    public IReadOnlyList<string> ListKeys() => [.. _checkpoints.Keys.Order(StringComparer.Ordinal)];

    public string? Read(string key) => _checkpoints.GetValueOrDefault(key);

    public void Write(string key, string checkpoint)
    {
        _checkpoints[key] = checkpoint;
        using var document = JsonDocument.Parse(checkpoint);
        Collect(document.RootElement);
    }

    public void Delete(string key) => _checkpoints.Remove(key);

    /// <summary>
    /// Liefert die Daten der zuletzt neu aufgenommenen Hülle mit diesem Ereignisnamen.
    /// </summary>
    internal JsonElement LastDataOf(string eventName)
    {
        var body = _bodies
            .Select(line => JsonDocument.Parse(line).RootElement)
            .LastOrDefault(root => root.GetProperty("event").GetString() == eventName);

        Assert.Equal(JsonValueKind.Object, body.ValueKind);
        return body.GetProperty("data");
    }

    private void Collect(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals("body") && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var body = property.Value.GetString()!;
                        if (!_bodies.Contains(body, StringComparer.Ordinal))
                        {
                            _bodies.Add(body);
                        }

                        continue;
                    }

                    Collect(property.Value);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    Collect(item);
                }

                break;
            default:
                break;
        }
    }
}
