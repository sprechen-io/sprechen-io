using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

/// <summary>
/// Bündelt die Bearbeitung des fokussierten Felds für freie Transkripte und bereits zusammengesetzte Werte.
/// </summary>
internal sealed class CqrsFormEditingExecutor : ICqrsFormEditingExecutor
{
    private readonly ApplyTranscriptCommandHandler _applyTranscriptHandler;
    private readonly ApplyComposedValueCommandHandler _applyComposedValueHandler;

    public CqrsFormEditingExecutor(FormTranscriptApplicator transcriptApplicator)
    {
        ArgumentNullException.ThrowIfNull(transcriptApplicator);
        _applyTranscriptHandler = new ApplyTranscriptCommandHandler(transcriptApplicator);
        _applyComposedValueHandler = new ApplyComposedValueCommandHandler(transcriptApplicator);
    }

    public Task<FormInputResult> ApplyTranscriptAsync(string transcript)
        => _applyTranscriptHandler.HandleAsync(new ApplyTranscriptCommand(transcript));

    public Task<FormInputResult> ApplyComposedValueAsync(string composedValue)
        => _applyComposedValueHandler.HandleAsync(new ApplyComposedValueCommand(composedValue));
}
