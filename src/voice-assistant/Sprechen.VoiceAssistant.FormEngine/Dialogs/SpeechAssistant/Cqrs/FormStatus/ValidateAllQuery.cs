using System.Collections.Immutable;
using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Fordert die Validierung der sichtbaren Felder und die daraus entstehenden Verstöße an.
/// </summary>
internal sealed record ValidateAllQuery : IQuery<ImmutableArray<FormConstraintViolation>>;
