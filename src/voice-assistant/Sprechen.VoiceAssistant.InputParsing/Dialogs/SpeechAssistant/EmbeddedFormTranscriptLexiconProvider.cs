using System.Text.Json;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt das eingebettete Transkriptlexikon einmalig und stellt dessen
/// modellierte Wörter den Eingabeparsern bereit.
/// </summary>
internal sealed class EmbeddedFormTranscriptLexiconProvider : IFormTranscriptLexiconProvider
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<FormTranscriptLexicon> SharedLexicon = new(LoadLexicon);

    public static EmbeddedFormTranscriptLexiconProvider Instance { get; } = new();

    public FormTranscriptLexicon GetLexicon()
        => SharedLexicon.Value;

    private static FormTranscriptLexicon LoadLexicon()
    {
        var assembly = typeof(EmbeddedFormTranscriptLexiconProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(FormTranscriptLexiconResourceNames.Default)
            ?? throw new InvalidOperationException($"Missing embedded transcript lexicon resource '{FormTranscriptLexiconResourceNames.Default}'.");

        return JsonSerializer.Deserialize<FormTranscriptLexicon>(stream, SerializerOptions)
            ?? throw new InvalidOperationException($"Embedded transcript lexicon resource '{FormTranscriptLexiconResourceNames.Default}' could not be deserialized.");
    }
}
