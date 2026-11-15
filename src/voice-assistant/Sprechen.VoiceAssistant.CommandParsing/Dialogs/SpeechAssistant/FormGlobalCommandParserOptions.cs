using System.Collections.Generic;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Bewertungsgewichte und Höflichkeitswörter für den Abgleich formularweiter Befehle.
/// </summary>
public sealed class FormGlobalCommandParserOptions
{
    public double ExactMatchScore { get; init; } = 1.0d;
    public double ArgumentMatchScore { get; init; } = 0.94d;
    public double PrefixPhraseScore { get; init; } = 0.86d;
    public double SuffixPhraseScore { get; init; } = 0.81d;
    public double InfixPhraseScore { get; init; } = 0.76d;
    public double UnknownExtraTokenPenalty { get; init; } = 0.07d;
    public double PoliteExtraTokenPenalty { get; init; } = 0.03d;
    public double LeadingExtraTokenPenalty { get; init; } = 0.03d;

    // Die sprachübergreifende Vereinigung stammt aus dem modellierten Transkriptlexikon.
    public IReadOnlySet<string> PoliteModifierTokens { get; init; } = CommandPolitePaddingLexicon.UnionTokens;
}
