using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bindet ein Paar aus Interaktionsart und Wertart an den zuständigen
/// Parseraufruf für einen Formularwert.
/// </summary>
internal sealed class EffectiveInteractionValueParseRoute
{
    private readonly EffectiveInteractionValueParseHandler _parse;

    public EffectiveInteractionValueParseRoute(
        string interactionType,
        string valueTypeKind,
        EffectiveInteractionValueParseHandler parse)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interactionType);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueTypeKind);
        ArgumentNullException.ThrowIfNull(parse);

        InteractionType = interactionType;
        ValueTypeKind = valueTypeKind;
        _parse = parse;
    }

    public string InteractionType { get; }

    public string ValueTypeKind { get; }

    public bool Matches(string interactionType, string valueTypeKind)
        => string.Equals(interactionType, InteractionType, StringComparison.OrdinalIgnoreCase)
            && string.Equals(valueTypeKind, ValueTypeKind, StringComparison.OrdinalIgnoreCase);

    public FormTranscriptParseOutcome Parse(
        string transcript,
        ImmutableArray<FormChoiceOption> choiceOptions)
        => _parse(transcript, choiceOptions);
}
