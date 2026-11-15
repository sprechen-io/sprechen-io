using System.Collections.Generic;
using System.Collections.Immutable;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class NoOpVoiceCommandDialog : IVoiceCommandDialog
{
    public Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(
            VoiceCommandDialogResult.Abandoned(
                applicationId,
                formId,
                initialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal)));
    }
}



