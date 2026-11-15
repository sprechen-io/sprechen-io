namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet einen Fehler in der Definition einer Formularbedingung mit der vom prüfenden Aufrufer gelieferten Beschreibung.
/// </summary>
internal sealed class FormConstraintAuthoringException(string message) : Exception(message);
