using System.Collections.Immutable;
using Sprechen.Common.Cqrs;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormRules;

/// <summary>
/// Fordert die Vertragsregeln beim Absendeversuch an und gibt die Kennungen der
/// von ihren Aktionen gesammelten Ansagen zurück.
/// </summary>
internal sealed record ApplySubmitAttemptRulesCommand : ICommand<ImmutableArray<string>>;
