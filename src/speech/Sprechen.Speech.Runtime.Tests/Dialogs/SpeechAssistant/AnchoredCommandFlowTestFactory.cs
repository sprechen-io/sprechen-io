using Moq;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Baut den Kommandopfad für Tests mit echten Feldresolvern, einem Ausführungsdienst
/// und einem Sprachdienst für den Ankerhinweis. Ohne übergebenen Ausführungsdienst
/// weist ein strikter Router unerwartete verankerte Kommandos zurück.
/// </summary>
internal static class AnchoredCommandFlowTestFactory
{
    public static CharacterAccumulationAnchoredCommandFlow Create(
        FormVoiceCommandGlobalCommandExecutionService? executionService = null,
        IFormVoiceCommandSpeechService? speechService = null)
        => new(
            new FormVoiceCommandTranscriptSourceResolver(),
            new FormVoiceCommandParsedCommandResolver(),
            executionService ?? new FormVoiceCommandGlobalCommandExecutionService(
                new Mock<IEvaluationCommandTelemetry>(MockBehavior.Loose).Object,
                new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict).Object),
            speechService ?? new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose).Object);
}
