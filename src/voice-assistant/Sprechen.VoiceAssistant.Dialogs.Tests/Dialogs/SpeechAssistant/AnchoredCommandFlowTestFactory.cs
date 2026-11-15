using Moq;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt den Ablauf für Befehle während einer zusammengesetzten Eingabe.
/// Verwendet die produktiven Resolver für Transkriptquelle und Befehlsauswertung.
/// Ohne übergebene Dienste ersetzen Mocks die Befehlsweiterleitung und Sprachausgabe;
/// unerwartete Aufrufe der Befehlsweiterleitung lassen den Test fehlschlagen.
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
