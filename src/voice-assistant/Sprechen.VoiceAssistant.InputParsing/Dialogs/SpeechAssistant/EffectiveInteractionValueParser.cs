using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet effektive Feldinteraktionen und Wertarten den Transkriptparsern zu.
/// Für nicht zugeordnete Kombinationen wird Text ohne äußere Leerzeichen übernommen, sofern er nicht leer ist.
/// </summary>
internal sealed class EffectiveInteractionValueParser
{
    private readonly string _requestedIsoCode;
    private readonly ImmutableArray<EffectiveInteractionValueParseRoute> _routes;
    private readonly IFormTranscriptParser _transcriptParser;

    public EffectiveInteractionValueParser(
        string requestedIsoCode,
        IFormTranscriptParser transcriptParser)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedIsoCode);
        _requestedIsoCode = requestedIsoCode.Trim();
        _transcriptParser = transcriptParser ?? throw new ArgumentNullException(nameof(transcriptParser));
        _routes =
        [
            new(EffectiveFieldInteractionTypes.Text, ValueTypeKinds.String, ParseText),
            new(EffectiveFieldInteractionTypes.Boolean, ValueTypeKinds.Boolean, ParseBoolean),
            new(EffectiveFieldInteractionTypes.Number, ValueTypeKinds.Integer, ParseInteger),
            new(EffectiveFieldInteractionTypes.Code, ValueTypeKinds.String, ParseCode),
            new(EffectiveFieldInteractionTypes.SingleSelect, ValueTypeKinds.Enum, ParseSingleSelect),
            new(EffectiveFieldInteractionTypes.MultiSelect, ValueTypeKinds.Set, ParseMultiSelect),
            new(EffectiveFieldInteractionTypes.Date, ValueTypeKinds.Date, ParseDate),
            new(EffectiveFieldInteractionTypes.Time, ValueTypeKinds.Time, ParseTime)
        ];
    }

    public FormTranscriptParseOutcome Parse(
        string effectiveInteractionType,
        string normalizedValueTypeKind,
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        foreach (var route in _routes)
        {
            if (!route.Matches(effectiveInteractionType, normalizedValueTypeKind))
            {
                continue;
            }

            return route.Parse(transcript, choiceOptions);
        }

        var fallback = transcript.Trim();
        return fallback.Length > 0
            ? FormTranscriptParseOutcome.Succeeded(fallback)
            : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }

    private static FormTranscriptParseOutcome ParseText(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        var value = transcript.Trim();
        return value.Length > 0
            ? FormTranscriptParseOutcome.Succeeded(value)
            : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }

    private FormTranscriptParseOutcome ParseBoolean(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        return _transcriptParser.ParseYesNo(_requestedIsoCode, transcript);
    }

    private FormTranscriptParseOutcome ParseInteger(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        return _transcriptParser.ParseInteger(_requestedIsoCode, transcript);
    }

    private FormTranscriptParseOutcome ParseCode(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        var value = _transcriptParser.NormalizeCode(transcript);
        return value.Length > 0
            ? FormTranscriptParseOutcome.Succeeded(value)
            : FormTranscriptParseOutcome.Failed(FormTranscriptParseFailure.NotRecognized);
    }

    private FormTranscriptParseOutcome ParseSingleSelect(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
        => _transcriptParser.ParseChoice(_requestedIsoCode, transcript, choiceOptions, allowMultiple: false);

    private FormTranscriptParseOutcome ParseMultiSelect(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
        => _transcriptParser.ParseChoice(_requestedIsoCode, transcript, choiceOptions, allowMultiple: true);

    private FormTranscriptParseOutcome ParseDate(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        return _transcriptParser.ParseDate(_requestedIsoCode, transcript);
    }

    private FormTranscriptParseOutcome ParseTime(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
    {
        _ = choiceOptions;
        return _transcriptParser.ParseTime(_requestedIsoCode, transcript);
    }
}
