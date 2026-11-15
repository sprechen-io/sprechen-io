using System.Collections.Immutable;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt Variablenwerte und Abgabestatus zur Wiederaufnahme einer einzelnen Formularsitzung.
/// </summary>
public sealed record SpeechAssistantFormSessionState(
    ImmutableDictionary<string, string> ValuesByVariableId,
    bool IsSubmitted)
{
    public static SpeechAssistantFormSessionState Empty { get; } =
        new(ImmutableDictionary.Create<string, string>(StringComparer.Ordinal), IsSubmitted: false);
}


