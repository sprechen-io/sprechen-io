using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Leitet <see cref="ApplyTranscriptCommand"/> an den gemeinsamen
/// <see cref="FormTranscriptApplicator"/> weiter, der Wertübernahme und Validierung ausführt.
/// </summary>
internal sealed class ApplyTranscriptCommandHandler : ICommandHandler<ApplyTranscriptCommand, FormInputResult>
{
    private readonly FormTranscriptApplicator _transcriptApplicator;

    public ApplyTranscriptCommandHandler(FormTranscriptApplicator transcriptApplicator)
    {
        _transcriptApplicator = transcriptApplicator ?? throw new ArgumentNullException(nameof(transcriptApplicator));
    }

    public Task<FormInputResult> HandleAsync(ApplyTranscriptCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(_transcriptApplicator.TryApplyTranscript(command.Transcript));
    }
}
