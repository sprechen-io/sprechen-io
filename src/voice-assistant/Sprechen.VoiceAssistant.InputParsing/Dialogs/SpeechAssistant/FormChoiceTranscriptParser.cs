using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Interpretiert Einzel- und Mehrfachauswahltranskripte als Optionscodes.
/// Einzelauswahl meldet mehrdeutige Treffer. Mehrfachauswahl sammelt vollständig erkannte Segmente.
/// </summary>
internal sealed class FormChoiceTranscriptParser
{
    // „ohne“ und „nein“ ergänzen für die Auswahl die modellierten Negationswörter
    // und sperren ebenfalls die Suche nach enthaltenen Bezeichnungen.
    private static readonly string[] ChoiceLocalVetoTokens = ["ohne", "nein"];

    // Fehlt ein sprachspezifischer Eintrag, trennen „und“ und „and“ die Liste.
    // Kurze romanische Bindewörter gelten nur in ihrer Sprache, da sie auch
    // Bestandteil einer Optionsbezeichnung sein können.
    private static readonly IReadOnlySet<string> FallbackChoiceConnectives =
        new HashSet<string>(["und", "and"], StringComparer.Ordinal);

    private readonly FormTranscriptLexiconIndex _lexicon;
    private readonly FormChoiceOptionMatcher _matcher;
    private readonly INegationParticleResolver _negationParticleResolver;

    public FormChoiceTranscriptParser(
        FormTranscriptLexiconIndex lexicon,
        INegationParticleResolver negationParticleResolver)
    {
        _lexicon = lexicon ?? throw new ArgumentNullException(nameof(lexicon));
        _matcher = new FormChoiceOptionMatcher(lexicon);
        _negationParticleResolver = negationParticleResolver ?? throw new ArgumentNullException(nameof(negationParticleResolver));
    }

    public FormTranscriptParseOutcome Parse(
        string? requestedIsoCode,
        string transcript,
        ImmutableArray<FormChoiceOption> options,
        bool allowMultiple)
    {
        if (options.Length == 0)
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(transcript);
        if (normalized.Length == 0)
        {
            return FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
        }

        return allowMultiple
            ? ParseMultiSelect(requestedIsoCode, transcript, normalized, options)
            : ParseSingleSelect(requestedIsoCode, normalized, options);
    }

    private FormTranscriptParseOutcome ParseSingleSelect(
        string? requestedIsoCode,
        string normalized,
        ImmutableArray<FormChoiceOption> options)
    {
        var vetoTokens = BuildVetoTokens();
        var (code, ambiguousCandidates) = _matcher.ResolveSingle(requestedIsoCode, normalized, options, vetoTokens);
        if (code is not null)
        {
            return FormTranscriptParseOutcome.Succeeded(code);
        }

        return ambiguousCandidates.IsEmpty
            ? FormTranscriptParseOutcome.Failed(new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption))
            : FormTranscriptParseOutcome.Failed(new FormTranscriptParseFailure(
                FormTranscriptParseFailureCategory.AmbiguousChoice,
                Candidates: ambiguousCandidates));
    }

    private FormTranscriptParseOutcome ParseMultiSelect(
        string? requestedIsoCode,
        string rawTranscript,
        string normalized,
        ImmutableArray<FormChoiceOption> options)
    {
        if (_matcher.TryMatchWhole(normalized, options, out var directCode))
        {
            return FormTranscriptParseOutcome.Succeeded(directCode);
        }

        var connectives = _lexicon.TryGetChoiceConnectives(requestedIsoCode, out var languageConnectives)
            ? languageConnectives
            : FallbackChoiceConnectives;

        var selected = new List<string>();
        // Vor der Normalisierung trennen: Komma und Semikolon würden sonst zu Leerzeichen.
        // Jedes Segment wird anschließend einzeln normalisiert, sodass auch mehrteilige
        // Bezeichnungen wie „E-Mail“ als Ganzes vergleichbar bleiben.
        foreach (var segment in FormChoiceTranscriptSplitter.SplitRaw(rawTranscript, connectives))
        {
            var candidate = TextNormalizer.NormalizeForPhraseMatching(segment);
            if (candidate.Length == 0)
            {
                continue;
            }

            // Erkannte Optionen sammeln. Nicht zuordenbare Segmente überspringen.
            // Die Bestätigung der gesammelten Auswahl gehört zum aufrufenden Dialog.
            if (_matcher.TryMatchExact(candidate, options, out var code))
            {
                selected.Add(code);
                continue;
            }

            // Fehlen Satzzeichen, darf das Segment nur bei vollständiger Zerlegung in
            // Optionsbezeichnungen oder Codes übernommen werden. Ein unbekannter Rest
            // verwirft das Segment einschließlich seiner bereits erkannten Teile.
            if (FormChoiceOptionSequenceDecomposer.TryDecompose(candidate, options, out var decomposedCodes))
            {
                selected.AddRange(decomposedCodes);
            }
        }

        if (selected.Count == 0)
        {
            return FormTranscriptParseOutcome.Failed(new FormTranscriptParseFailure(FormTranscriptParseFailureCategory.NoMatchingOption));
        }

        return FormTranscriptParseOutcome.Succeeded(FormChoiceCanonicalValueFormatter.Format(selected, allowMultiple: true));
    }

    private IReadOnlySet<string> BuildVetoTokens()
    {
        var vetoTokens = new HashSet<string>(StringComparer.Ordinal);

        // Die Negationsmenge gilt sprachübergreifend: Eine deutsche Negation soll eine
        // deutsche Bezeichnung auch bei anderssprachig angefordertem Feld vor Teiltreffern sperren.
        foreach (var particle in _negationParticleResolver.ResolveNegationParticles())
        {
            var normalized = TextNormalizer.NormalizeForPhraseMatching(particle);
            if (normalized.Length != 0)
            {
                vetoTokens.Add(normalized);
            }
        }

        foreach (var token in ChoiceLocalVetoTokens)
        {
            vetoTokens.Add(token);
        }

        return vetoTokens;
    }
}
