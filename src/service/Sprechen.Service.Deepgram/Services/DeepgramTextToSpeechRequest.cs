using Deepgram.Models.Speak.v2.WebSocket;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Enthält Text, Modell, Abtastrate und WebSocket-Schema einer Syntheseanfrage.
/// </summary>
internal sealed record DeepgramTextToSpeechRequest(
    string Text,
    string Model,
    int SampleRateHz,
    SpeakSchema Schema);
