using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Hält Formularsitzungen nach Kennung zusammen, um ihre Werte und Abgabestände innerhalb einer Anwendung weiterzuführen.
/// </summary>
public sealed record SpeechAssistantApplicationSessionState(
    ImmutableDictionary<string, SpeechAssistantFormSessionState> FormsById)
{
    public static SpeechAssistantApplicationSessionState Empty { get; } =
        new(ImmutableDictionary.Create<string, SpeechAssistantFormSessionState>(StringComparer.OrdinalIgnoreCase));
}


