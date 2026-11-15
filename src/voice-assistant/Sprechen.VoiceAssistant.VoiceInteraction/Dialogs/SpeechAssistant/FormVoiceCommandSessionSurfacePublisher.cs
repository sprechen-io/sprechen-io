using Sprechen.Contracts.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.FormSurface;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Überträgt die vorgesehenen Sitzungszustände an die Oberfläche. Der Veröffentlichungsweg bleibt beim übergebenen Empfänger.
/// </summary>
internal sealed class FormVoiceCommandSessionSurfacePublisher
{
    private readonly IFormSurfaceDefinitionBuilder _formSurfaceDefinitionBuilder;
    private readonly SpeechAssistantFormSurfacePublication _publication;

    public FormVoiceCommandSessionSurfacePublisher(
        IFormSurfaceDefinitionBuilder formSurfaceDefinitionBuilder,
        SpeechAssistantFormSurfacePublication publication)
    {
        _formSurfaceDefinitionBuilder = formSurfaceDefinitionBuilder ?? throw new ArgumentNullException(nameof(formSurfaceDefinitionBuilder));
        _publication = publication ?? throw new ArgumentNullException(nameof(publication));
    }

    public FormSurfaceDefinition BuildProjection(FormVoiceCommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _formSurfaceDefinitionBuilder.BuildFormSurfaceDefinition(
            context.Contract,
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel());
    }

    public FormSurfacePublicationSession BeginSession()
        => _publication.BeginSession();

    public async Task<bool> TryPublishActiveAsync(
        FormSurfacePublicationSession session,
        FormVoiceCommandContext context,
        IFormDialogSurfaceState runtime,
        FormSurfaceDefinition formProjection)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(formProjection);

        return await _publication
            .TryPublishActiveAsync(session, CreateIdentity(context), runtime, formProjection)
            .ConfigureAwait(false);
    }

    public async Task<bool> TryPublishTerminalAsync(
        FormSurfacePublicationSession session,
        FormVoiceCommandContext context,
        IFormDialogSurfaceState runtime,
        FormSurfaceDefinition formProjection,
        FormSurfaceStage stage)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(formProjection);

        return await _publication
            .TryPublishTerminalAsync(session, CreateIdentity(context), runtime, formProjection, stage)
            .ConfigureAwait(false);
    }

    public void EndSession(FormSurfacePublicationSession session)
        => _publication.EndSession(session);

    public void Stop()
        => _publication.Stop();

    private static FormSurfacePublicationIdentity CreateIdentity(FormVoiceCommandContext context)
    {
        var resolver = new FormsCatalogTextResolver();
        return new FormSurfacePublicationIdentity(
            context.IsoCode,
            context.ApplicationId,
            resolver.ResolveApplicationDisplayName(context.Application, context.IsoCode),
            context.Form.FormId.Value,
            resolver.ResolveFormDisplayName(context.Application, context.Form, context.IsoCode));
    }
}
