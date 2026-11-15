namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet bekannte modellierte Auflösungsstrategien der internen Auswahl zu und verwendet bei unbekannten Angaben die Kontextentscheidung.
/// </summary>
internal sealed class InputVsCommandResolutionStrategyMapper
{
    internal const string ContextDefault = "context_default";

    private static readonly IReadOnlyDictionary<string, string> MappedStrategies =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["deterministic"] = ContextDefault,
            ["none"] = ContextDefault,
            [ContextDefault] = ContextDefault,
            ["prefix"] = "prefix",
            ["spell_fallback"] = "spell_fallback",
            ["query"] = "query"
        };

    public string Map(string? resolutionStrategy)
    {
        if (string.IsNullOrWhiteSpace(resolutionStrategy))
        {
            return ContextDefault;
        }

        return MappedStrategies.TryGetValue(resolutionStrategy.Trim(), out var mapped)
            ? mapped
            : ContextDefault;
    }
}
