using System.Collections.Concurrent;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Merkt Formularbeobachtungen und Aufnahmeversuche je Sitzung im Arbeitsspeicher,
/// damit Folgeereignisse ihren vorherigen Fokus, Status und Fehlerbezug kennen.
/// </summary>
internal sealed class EvaluationTelemetrySessionStateStore
{
    private readonly ConcurrentDictionary<string, EvaluationDetailedFormStatus> _lastFieldAndGroupStatusBySession = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, EvaluationTelemetryErrorState>> _activeValidationErrorsBySession = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _lastFocusedFieldBySession = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, EvaluationStatusNavigationSnapshot> _lastNavigationBySession = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _listeningAttemptNumbersBySession = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> _currentListeningAttemptBySession = new(StringComparer.Ordinal);

    public void ClearSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        _lastFieldAndGroupStatusBySession.TryRemove(sessionId, out _);
        _activeValidationErrorsBySession.TryRemove(sessionId, out _);
        _lastFocusedFieldBySession.TryRemove(sessionId, out _);
        _lastNavigationBySession.TryRemove(sessionId, out _);
        _listeningAttemptNumbersBySession.TryRemove(sessionId, out _);
        _currentListeningAttemptBySession.TryRemove(sessionId, out _);
    }

    public bool TryUpdateFocusedField(string sessionId, string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (_lastFocusedFieldBySession.TryGetValue(sessionId, out var previousFieldId)
            && string.Equals(previousFieldId, fieldId, StringComparison.Ordinal))
        {
            return false;
        }

        _lastFocusedFieldBySession[sessionId] = fieldId;
        ResetListeningAttempts(sessionId);
        return true;
    }

    public bool TryGetFocusedField(string sessionId, out string? fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _lastFocusedFieldBySession.TryGetValue(sessionId, out fieldId);
    }

    public bool TryUpdateNavigation(string sessionId, EvaluationStatusNavigationSnapshot navigationState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(navigationState);

        if (_lastNavigationBySession.TryGetValue(sessionId, out var previousNavigation)
            && Equals(previousNavigation, navigationState))
        {
            return false;
        }

        _lastNavigationBySession[sessionId] = navigationState;
        return true;
    }

    public EvaluationDetailedFormStatus? GetLastDetailedStatus(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _lastFieldAndGroupStatusBySession.TryGetValue(sessionId, out var snapshot) ? snapshot : null;
    }

    public void SetLastDetailedStatus(string sessionId, EvaluationDetailedFormStatus snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(snapshot);
        _lastFieldAndGroupStatusBySession[sessionId] = snapshot;
    }

    public void RememberValidationError(string sessionId, EvaluationTelemetryErrorState errorState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(errorState);

        var activeErrors = _activeValidationErrorsBySession.GetOrAdd(
            sessionId,
            static _ => new ConcurrentDictionary<string, EvaluationTelemetryErrorState>(StringComparer.Ordinal));

        activeErrors[BuildValidationErrorKey(
            errorState.ErrorCode,
            errorState.Scope,
            errorState.FieldId,
            errorState.FieldGroupId)] = errorState;
    }

    public IReadOnlyList<EvaluationTelemetryErrorState> RemoveValidationErrorsForField(string sessionId, string fieldId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldId);

        if (!_activeValidationErrorsBySession.TryGetValue(sessionId, out var activeErrors))
        {
            return [];
        }

        var removedErrors = new List<EvaluationTelemetryErrorState>();
        var keysToClear = activeErrors
            .Where(pair => string.Equals(pair.Value.FieldId, fieldId, StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var key in keysToClear)
        {
            if (activeErrors.TryRemove(key, out var removed))
            {
                removedErrors.Add(removed);
            }
        }

        return removedErrors;
    }

    public int RecordListeningStarted(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var attemptNumber = _listeningAttemptNumbersBySession.AddOrUpdate(
            sessionId,
            1,
            static (_, current) => current == int.MaxValue ? int.MaxValue : current + 1);
        _currentListeningAttemptBySession[sessionId] = attemptNumber;
        return attemptNumber;
    }

    public bool TryConsumeListeningAttempt(
        string sessionId,
        out int attemptNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_currentListeningAttemptBySession.TryRemove(sessionId, out attemptNumber))
        {
            attemptNumber = 0;
            return false;
        }

        return true;
    }

    public void ResetListeningAttempts(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        _listeningAttemptNumbersBySession.TryRemove(sessionId, out _);
        _currentListeningAttemptBySession.TryRemove(sessionId, out _);
    }

    private static string BuildValidationErrorKey(string errorCode, string scope, string fieldId, string? fieldGroupId)
    {
        return $"{errorCode}|{scope}|{fieldId}|{fieldGroupId ?? string.Empty}";
    }
}
