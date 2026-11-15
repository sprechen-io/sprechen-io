using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.FormSurface;

/// <summary>
/// Verbindet Snapshotaufbau mit der Veröffentlichungskennung einer Formularsitzung,
/// damit auch asynchron erstellte Ansichten gegen ihre Sitzung geprüft werden können.
/// </summary>
public sealed class SpeechAssistantFormSurfacePublication
{
    private readonly FormSurfaceSnapshotAssembler _assembler;
    private readonly ISpeechAssistantFormSurfaceController _controller;

    public SpeechAssistantFormSurfacePublication(
        FormSurfaceSnapshotAssembler assembler,
        ISpeechAssistantFormSurfaceController controller)
    {
        _assembler = assembler ?? throw new ArgumentNullException(nameof(assembler));
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    }

    public FormSurfacePublicationSession BeginSession()
        => _controller.BeginSession();

    public async Task<bool> TryPublishActiveAsync(
        FormSurfacePublicationSession session,
        FormSurfacePublicationIdentity identity,
        IFormDialogSurfaceState surfaceState,
        FormSurfaceDefinition formProjection)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(surfaceState);
        ArgumentNullException.ThrowIfNull(formProjection);

        var snapshot = await _assembler
            .CreateAsync(identity, surfaceState, formProjection, FormSurfaceStage.Active)
            .ConfigureAwait(false);
        return _controller.TryPublishActive(session, SpeechAssistantFormSurfaceView.FromSafeSnapshot(snapshot));
    }

    public async Task<bool> TryPublishTerminalAsync(
        FormSurfacePublicationSession session,
        FormSurfacePublicationIdentity identity,
        IFormDialogSurfaceState surfaceState,
        FormSurfaceDefinition formProjection,
        FormSurfaceStage stage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(surfaceState);
        ArgumentNullException.ThrowIfNull(formProjection);

        var snapshot = await _assembler
            .CreateAsync(identity, surfaceState, formProjection, stage)
            .ConfigureAwait(false);
        return _controller.TryPublishTerminal(session, SpeechAssistantFormSurfaceView.FromSafeSnapshot(snapshot));
    }

    public void EndSession(FormSurfacePublicationSession session)
        => _controller.EndSession(session);

    public void Stop()
        => _controller.Stop();
}
