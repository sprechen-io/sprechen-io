using Monstertacle.SharpQrs;

namespace Sprechen.Service.Application.Cqrs.SpeechSynthesis;

/// <summary>
/// Auftrag zur Sprachsynthese mit einer Folge von Audioblöcken als Ergebnis.
/// </summary>
public sealed record SynthesizeSpeechCommand(SynthesizeSpeechInput Input)
    : ICommand<IAsyncEnumerable<AudioSynthesisChunk>>;
