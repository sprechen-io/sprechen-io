namespace Sprechen.Service.Abstractions;

/// <summary>
/// Kombiniert Transkription, Gesprächserkennung und Sprachsynthese für Aufrufer, die alle drei benötigen.
/// Für einzelne Fähigkeiten stehen <see cref="ISpeechToTextService"/>,
/// <see cref="IConversationalSpeechToTextService"/> und <see cref="ITextToSpeechService"/> zur Verfügung.
/// </summary>
public interface ISpeechService : ISpeechToTextService, IConversationalSpeechToTextService, ITextToSpeechService
{
}
