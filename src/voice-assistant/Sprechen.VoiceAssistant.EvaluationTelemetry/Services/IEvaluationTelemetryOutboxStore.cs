namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Stellt synchronen dauerhaften Speicher für Checkpoints bereit, damit
/// der Versand erst nach dem Speichern eines Ereignisses beginnen kann.
/// </summary>
public interface IEvaluationTelemetryOutboxStore
{
    IReadOnlyList<string> ListKeys();

    string? Read(string key);

    /// <summary>
    /// Ersetzt den Checkpoint unter <paramref name="key"/> atomar.
    /// </summary>
    void Write(string key, string checkpoint);

    void Delete(string key);
}
