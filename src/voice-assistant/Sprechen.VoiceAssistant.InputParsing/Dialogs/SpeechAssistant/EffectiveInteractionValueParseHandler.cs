using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

internal delegate FormTranscriptParseOutcome EffectiveInteractionValueParseHandler(
    string transcript,
    ImmutableArray<FormChoiceOption> choiceOptions);
