using Sprechen.Common.Cqrs;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormFocus;

/// <summary>
/// Fordert einen Fokuswechsel zum benannten Feld über die Navigationsprüfung an.
/// </summary>
/// <param name="FieldId">Die typisierte Kennung des gewünschten Fokusziels.</param>
internal sealed record SetFocusToFieldIdCommand(FieldId FieldId) : ICommand<bool>;
