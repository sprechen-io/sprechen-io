using Sprechen.Common.Cqrs;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Leitet <see cref="ApplyComposedValueCommand"/> an den gemeinsamen
/// <see cref="FormTranscriptApplicator"/> weiter, der Wertübernahme und Validierung ausführt.
/// </summary>
internal sealed class ApplyComposedValueCommandHandler : ICommandHandler<ApplyComposedValueCommand, FormInputResult>
{
    private readonly FormTranscriptApplicator _transcriptApplicator;

    public ApplyComposedValueCommandHandler(FormTranscriptApplicator transcriptApplicator)
    {
        _transcriptApplicator = transcriptApplicator ?? throw new ArgumentNullException(nameof(transcriptApplicator));
    }

    public Task<FormInputResult> HandleAsync(ApplyComposedValueCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult(_transcriptApplicator.TryApplyComposedValue(command.ComposedValue));
    }
}
