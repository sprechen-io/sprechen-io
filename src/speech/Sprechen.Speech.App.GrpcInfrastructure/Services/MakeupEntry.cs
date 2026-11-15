namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Verknüpft die gemessene Lautheitsanhebung einer Sprache in Dezibel
/// mit der Aura-Stimme, für die dieser Messwert gilt.
/// </summary>
/// <param name="MeasuredVoice">Kennung der vermessenen Deepgram-Aura-Stimme. Sie muss <c>DeepgramOptions.TtsModels[iso]</c> entsprechen.</param>
/// <param name="MakeupDb">Anhebung zur Referenzlautheit R = −16 LUFS in Dezibel, ermittelt aus dem Median der vermessenen Äußerungen.</param>
public readonly record struct MakeupEntry(string MeasuredVoice, double MakeupDb);
