using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.SpeechCapture;

/// <summary>
/// Mikrofonregel während der Sprachausgabe: hardOff sieht Stummschalten während
/// der Wiedergabe vor, heuristic ein geöffnetes Mikrofon. Die Ausführung
/// dieser Regel erfolgt außerhalb des Modells.
/// </summary>
public sealed class SpeechCaptureMicGateProfile
{
    [JsonPropertyName("policy")]
    public string Policy { get; set; } = string.Empty;
}
