using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Lädt den aktuellen Anwendungsstand und wendet stille Eingabe über einen Formularlauf an. Das Ergebnis enthält den angepassten Formularstand zur weiteren Übernahme.
/// </summary>
internal sealed class FormVoiceCommandSilentInputTransaction : IFormSilentInputTransaction
{
    private readonly IFormVoiceCommandContextLoader _contextLoader;
    private readonly IFormDialogRuntimeFactory _runtimeFactory;

    public FormVoiceCommandSilentInputTransaction(
        IFormVoiceCommandContextLoader contextLoader,
        IFormDialogRuntimeFactory runtimeFactory)
    {
        _contextLoader = contextLoader ?? throw new ArgumentNullException(nameof(contextLoader));
        _runtimeFactory = runtimeFactory ?? throw new ArgumentNullException(nameof(runtimeFactory));
    }

    public async Task<FormSilentInputTransactionResult> ExecuteAsync(
        FormSilentInputTransactionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var initialValues = request.InitialValuesByVariableId.ToImmutableDictionary(StringComparer.Ordinal);
        var load = await _contextLoader
            .LoadAsync(
                request.LanguageCode,
                request.ApplicationId,
                request.FormId,
                request.ApplicationSession,
                initialValues,
                cancellationToken)
            .ConfigureAwait(false);

        if (load.Context is null
            || !string.Equals(load.Context.Contract.FormId.Value, request.FormId, StringComparison.Ordinal)
            || !string.Equals(load.Context.Contract.ApplicationId, request.ApplicationId, StringComparison.Ordinal))
        {
            return FormSilentInputTransactionResult.StateChanged(EmptyObservation(request), eligibility: null);
        }

        var runtime = _runtimeFactory.Create(
            load.Context,
            sessionId: $"silent-input:{request.FormId}:{request.FieldId}",
            SilentInputRuleLoopGuardFeedback.Instance);

        if (!await runtime.TrySetFocusToFieldIdAsync(request.FieldId).ConfigureAwait(false))
        {
            var unmatched = await runtime.SilentInput
                .ApplySilentInputAsync(request.ToRuntimeRequest(), cancellationToken)
                .ConfigureAwait(false);
            return FormSilentInputTransactionResult.FromRuntime(unmatched);
        }

        var result = await runtime.SilentInput
            .ApplySilentInputAsync(request.ToRuntimeRequest(), cancellationToken)
            .ConfigureAwait(false);
        if (result.Kind != FormSilentInputResultKind.Applied)
        {
            return FormSilentInputTransactionResult.FromRuntime(result);
        }

        var appliedForm = result.ApplicationSession!.FormsById[request.FormId];
        var mergedSession = new SpeechAssistantApplicationSessionState(
            request.ApplicationSession.FormsById.SetItem(request.FormId, appliedForm));
        return FormSilentInputTransactionResult.Applied(
            mergedSession,
            result.CanonicalSame,
            result.Snapshot!,
            result.Eligibility);
    }

    private static FormSurfaceSnapshot EmptyObservation(FormSilentInputTransactionRequest request)
        => new(
            FormSurfaceStage.Active,
            request.LanguageCode,
            request.ApplicationId,
            applicationDisplayName: string.Empty,
            request.FormId,
            formDisplayName: string.Empty,
            new FormSurfaceDefinition(),
            ImmutableDictionary<string, FormSurfaceFieldValue>.Empty,
            focusedFieldId: null,
            ImmutableDictionary<string, FormSurfaceFieldStatus>.Empty,
            ImmutableDictionary<string, FormSurfaceFieldGroupStatus>.Empty);

}
