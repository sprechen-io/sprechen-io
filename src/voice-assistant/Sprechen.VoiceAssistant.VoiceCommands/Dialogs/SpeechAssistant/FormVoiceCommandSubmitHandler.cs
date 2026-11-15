using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Versucht beim Abgabebefehl die Formularübermittlung und liefert
/// nur nach erfolgreicher Abgabe das entsprechende Dialogergebnis.
/// </summary>
internal sealed class FormVoiceCommandSubmitHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IFormVoiceCommandSubmissionService _submissionService;

    public FormVoiceCommandSubmitHandler(IFormVoiceCommandSubmissionService submissionService)
    {
        _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.Submit;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await _submissionService.TrySubmitAsync(
                context.Interpreter,
                context.Contract,
                context.Session,
                context.IsoCode,
                context.VoiceSettings,
                context.StateTracker,
                cancellationToken)
            .ConfigureAwait(false) == FormSubmissionResult.Submitted)
        {
            context.StateTracker.RecordSubmitted();
            return VoiceCommandDialogResult.Submitted(
                context.Contract.ApplicationId,
                context.Contract.FormId,
                (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                    .ToImmutableDictionary(StringComparer.Ordinal));
        }

        return null;
    }
}
