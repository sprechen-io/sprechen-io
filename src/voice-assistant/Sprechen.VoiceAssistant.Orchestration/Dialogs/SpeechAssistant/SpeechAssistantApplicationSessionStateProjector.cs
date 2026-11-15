using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Liest und aktualisiert die zwischengespeicherten Formularwerte und Abgabezustände
/// einer Anwendung, damit Dialogwechsel auf den bisherigen Sitzungsstand zugreifen können.
/// </summary>
internal static class SpeechAssistantApplicationSessionStateProjector
{
    private static readonly ImmutableDictionary<string, string> EmptyValuesByVariableId =
        ImmutableDictionary.Create<string, string>(StringComparer.Ordinal);

    private static readonly ImmutableHashSet<string> EmptyCompletedFormIds =
        ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

    internal static ImmutableHashSet<string> GetCompletedFormIds(SpeechAssistantState state, string applicationId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);

        if (!state.ApplicationSessions.TryGetValue(applicationId.Trim(), out var applicationSession))
        {
            return EmptyCompletedFormIds;
        }

        return applicationSession.FormsById
            .Where(pair => pair.Value.IsSubmitted)
            .Select(pair => pair.Key)
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal static ImmutableDictionary<string, string> GetInitialValuesByVariableId(
        SpeechAssistantState state,
        string? applicationId,
        string? formId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(applicationId) || string.IsNullOrWhiteSpace(formId))
        {
            return EmptyValuesByVariableId;
        }

        if (!state.ApplicationSessions.TryGetValue(applicationId.Trim(), out var applicationSession))
        {
            return EmptyValuesByVariableId;
        }

        if (!applicationSession.FormsById.TryGetValue(formId.Trim(), out var formSession))
        {
            return EmptyValuesByVariableId;
        }

        return formSession.ValuesByVariableId;
    }

    internal static SpeechAssistantApplicationSessionState GetApplicationSession(
        SpeechAssistantState state,
        string? applicationId)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (string.IsNullOrWhiteSpace(applicationId))
        {
            return SpeechAssistantApplicationSessionState.Empty;
        }

        return state.ApplicationSessions.TryGetValue(applicationId.Trim(), out var applicationSession)
            ? applicationSession
            : SpeechAssistantApplicationSessionState.Empty;
    }

    internal static ImmutableDictionary<string, SpeechAssistantApplicationSessionState> UpdateApplicationSessions(
        SpeechAssistantState state,
        VoiceCommandDialogResult result)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(result);

        if (string.IsNullOrWhiteSpace(result.ApplicationId) || string.IsNullOrWhiteSpace(result.FormId))
        {
            return state.ApplicationSessions;
        }

        var applicationId = result.ApplicationId.Trim();
        var formId = result.FormId.Trim();

        var applicationSession = state.ApplicationSessions.TryGetValue(applicationId, out var existingApplicationSession)
            ? existingApplicationSession
            : SpeechAssistantApplicationSessionState.Empty;

        var previousFormSession = applicationSession.FormsById.TryGetValue(formId, out var existingFormSession)
            ? existingFormSession
            : SpeechAssistantFormSessionState.Empty;

        var valuesByVariableId = result.ValuesByVariableId ?? EmptyValuesByVariableId;
        var isSubmitted = previousFormSession.IsSubmitted || result.Outcome == EvaluationOutcome.Submitted;

        var updatedFormSession = new SpeechAssistantFormSessionState(valuesByVariableId, isSubmitted);
        var updatedApplicationSession = applicationSession with
        {
            FormsById = applicationSession.FormsById.SetItem(formId, updatedFormSession)
        };

        return state.ApplicationSessions.SetItem(applicationId, updatedApplicationSession);
    }
}
