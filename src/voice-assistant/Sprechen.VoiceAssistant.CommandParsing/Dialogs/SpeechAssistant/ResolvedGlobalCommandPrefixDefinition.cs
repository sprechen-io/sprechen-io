using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Befehlspräfixe samt Sprachregeln für den lokalisierten Präfixabgleich.
/// </summary>
public sealed class ResolvedGlobalCommandPrefixDefinition
{
    public ResolvedGlobalCommandPrefixDefinition(
        string kind,
        IReadOnlyList<LocalizedText> utterances,
        IReadOnlyList<string> supportedLanguages,
        string? defaultLanguage,
        bool strictLocalization)
    {
        Kind = kind;
        Utterances = utterances;
        SupportedLanguages = supportedLanguages;
        DefaultLanguage = defaultLanguage;
        StrictLocalization = strictLocalization;
    }

    public string Kind { get; }

    public IReadOnlyList<LocalizedText> Utterances { get; }

    public IReadOnlyList<string> SupportedLanguages { get; }

    public string? DefaultLanguage { get; }

    public bool StrictLocalization { get; }
}

