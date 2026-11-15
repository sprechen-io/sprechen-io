using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.Speech.FormSurface;

/// <summary>
/// Fügt Oberflächenzustand und Formularprojektion zum Beobachtersnapshot zusammen,
/// damit aktive Ansichten freigegebene Feldwerte zeigen und Endzustände diese Werte auslassen.
/// </summary>
public sealed class FormSurfaceSnapshotAssembler
{
    public async Task<FormSurfaceSnapshot> CreateAsync(
        FormSurfacePublicationIdentity identity,
        IFormDialogSurfaceState surfaceState,
        FormSurfaceDefinition formProjection,
        FormSurfaceStage stage)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(surfaceState);
        ArgumentNullException.ThrowIfNull(formProjection);

        var fieldValuesById = stage == FormSurfaceStage.Active
            ? await surfaceState.GetSurfaceFieldValuesAsync().ConfigureAwait(false)
            : ImmutableDictionary<string, FormSurfaceFieldValue>.Empty;

        var focusedFieldId = await surfaceState.GetFocusedFieldIdAsync().ConfigureAwait(false);
        var detailedStatus = await surfaceState.GetDetailedStatusSnapshotAsync().ConfigureAwait(false);

        var fieldStatusesById = detailedStatus.Fields.ToImmutableDictionary(
            field => field.FieldId,
            field => new FormSurfaceFieldStatus(
                field.FieldId,
                field.FieldGroupId,
                field.Relevant,
                field.Required,
                field.Completed,
                field.BlockingErrorCodes),
            StringComparer.Ordinal);

        var fieldGroupStatusesById = detailedStatus.FieldGroups.ToImmutableDictionary(
            fieldGroup => fieldGroup.FieldGroupId,
            fieldGroup => new FormSurfaceFieldGroupStatus(
                fieldGroup.FieldGroupId,
                fieldGroup.Relevant,
                fieldGroup.Completed,
                fieldGroup.OpenGroupErrorCodes),
            StringComparer.Ordinal);

        return new FormSurfaceSnapshot(
            stage,
            identity.IsoCode,
            identity.ApplicationId,
            identity.ApplicationDisplayName,
            identity.FormId,
            identity.FormDisplayName,
            formProjection,
            fieldValuesById,
            focusedFieldId,
            fieldStatusesById,
            fieldGroupStatusesById);
    }
}
