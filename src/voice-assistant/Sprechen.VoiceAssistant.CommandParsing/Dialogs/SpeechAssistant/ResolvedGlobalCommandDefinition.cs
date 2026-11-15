using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt aufgelöste Befehlsdaten samt Sprachregeln für Erkennung und Vorlesebezeichnung.
/// </summary>
public sealed class ResolvedGlobalCommandDefinition
{
    public ResolvedGlobalCommandDefinition(
        string commandId,
        IReadOnlyList<LocalizedText> utterances,
        bool enabled,
        IReadOnlyList<string> supportedLanguages,
        string? defaultLanguage,
        bool strictLocalization,
        LocalizedText? spokenForm = null)
    {
        CommandId = commandId;
        Utterances = utterances;
        SpokenForm = spokenForm;
        Enabled = enabled;
        SupportedLanguages = supportedLanguages;
        DefaultLanguage = defaultLanguage;
        StrictLocalization = strictLocalization;
    }

    public string CommandId { get; }

    public IReadOnlyList<LocalizedText> Utterances { get; }

    /// <summary>
    /// Enthält die modellierten Vorleseformen oder null, wenn keine angegeben sind.
    /// Die Ansage erfordert zusätzlich eine Form für die angeforderte Sprache.
    /// Erkennungsäußerungen dienen nicht als Ersatz.
    /// </summary>
    /// <remarks>
    /// Ausschließlich für die Ausgabe bestimmt, nicht für Transkriptabgleich oder Erkennungshinweise.
    /// So verändert eine Umordnung der Erkennungsäußerungen nicht die hörbare Bezeichnung.
    /// </remarks>
    public LocalizedText? SpokenForm { get; }

    public bool Enabled { get; }

    public IReadOnlyList<string> SupportedLanguages { get; }

    public string? DefaultLanguage { get; }

    public bool StrictLocalization { get; }
}

