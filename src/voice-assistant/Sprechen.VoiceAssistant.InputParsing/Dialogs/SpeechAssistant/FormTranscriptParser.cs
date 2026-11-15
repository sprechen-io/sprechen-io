using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die spezialisierten Formulartranskriptparser hinter gemeinsamen
/// Verträgen bereit und versorgt sie mit einem gemeinsamen Lexikonindex.
/// </summary>
internal sealed class FormTranscriptParser : IFormTranscriptParser, IFormDateComponentParser
{
    private readonly FormYesNoTranscriptParser _yesNoParser;
    private readonly FormNumericTranscriptParser _numericParser;
    private readonly FormSpellingTranscriptParser _spellingParser;
    private readonly FormTemporalTranscriptParser _temporalParser;
    private readonly FormChoiceTranscriptParser _choiceParser;
    private readonly FormDateComponentParser _dateComponentParser;

    public FormTranscriptParser(IYesNoTranscriptClassifier yesNoClassifier, INegationParticleResolver negationParticleResolver)
        : this(yesNoClassifier, negationParticleResolver, EmbeddedFormTranscriptLexiconProvider.Instance)
    {
    }

    public FormTranscriptParser(
        IYesNoTranscriptClassifier yesNoClassifier,
        INegationParticleResolver negationParticleResolver,
        IFormTranscriptLexiconProvider lexiconProvider)
    {
        ArgumentNullException.ThrowIfNull(yesNoClassifier);
        ArgumentNullException.ThrowIfNull(negationParticleResolver);
        ArgumentNullException.ThrowIfNull(lexiconProvider);

        var lexicon = lexiconProvider.GetLexicon() ?? throw new InvalidOperationException("Transcript lexicon provider returned no lexicon.");
        var lexiconIndex = new FormTranscriptLexiconIndex(lexicon);
        _yesNoParser = new FormYesNoTranscriptParser(yesNoClassifier);
        _numericParser = new FormNumericTranscriptParser(lexiconIndex);
        _spellingParser = new FormSpellingTranscriptParser(lexiconIndex);
        _temporalParser = new FormTemporalTranscriptParser(lexicon: lexiconIndex);
        _choiceParser = new FormChoiceTranscriptParser(lexiconIndex, negationParticleResolver);
        _dateComponentParser = new FormDateComponentParser(_numericParser);
    }

    public FormTranscriptParseOutcome ParseYesNo(string requestedIsoCode, string transcript)
        => _yesNoParser.Parse(requestedIsoCode, transcript);

    public FormTranscriptParseOutcome ParseInteger(string? requestedIsoCode, string transcript)
        => _numericParser.ParseInteger(requestedIsoCode, transcript);

    public FormTranscriptParseOutcome ParseDigits(string? requestedIsoCode, string transcript)
        => _numericParser.ParseDigits(requestedIsoCode, transcript);

    public FormTranscriptParseOutcome ParseSpelling(string? requestedIsoCode, string transcript)
        => _spellingParser.Parse(requestedIsoCode, transcript);

    public FormTranscriptParseOutcome ParseDate(string requestedIsoCode, string transcript)
        => _temporalParser.ParseDate(requestedIsoCode, transcript);

    public FormDateComponentParseOutcome ParseDateComponent(
        string? requestedIsoCode,
        DateComponentSlot slot,
        string transcript)
        => _dateComponentParser.Parse(requestedIsoCode, slot, transcript);

    public FormTranscriptParseOutcome ParseTime(string requestedIsoCode, string transcript)
        => _temporalParser.ParseTime(requestedIsoCode, transcript);

    public FormTranscriptParseOutcome ParseChoice(
        string? requestedIsoCode,
        string transcript,
        ImmutableArray<FormChoiceOption> options,
        bool allowMultiple)
        => _choiceParser.Parse(requestedIsoCode, transcript, options, allowMultiple);

    public string NormalizeCode(string transcript)
        => _numericParser.NormalizeCode(transcript);
}
