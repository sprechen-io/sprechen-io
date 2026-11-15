using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Fordert die Anwendung eines fertig zusammengesetzten Werts auf das fokussierte Feld an.
/// Die gemeinsame Anwendung normalisiert und prüft den Wert ohne erneutes Lesen durch die
/// Buchstaben- oder Zifferngrammatik. Anschließend folgen Wertänderungsregeln und Feldvalidierung.
/// </summary>
/// <param name="ComposedValue">Der fertig zusammengesetzte Wert.</param>
internal sealed record ApplyComposedValueCommand(string ComposedValue) : ICommand<FormInputResult>;
