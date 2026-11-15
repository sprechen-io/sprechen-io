namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Bildet Speicherschlüssel je Durchlauf und gemeinsame Sperrschlüssel je Identität. Dadurch konkurrieren Durchläufe derselben Identität um dieselbe exklusive Sperre.
/// </summary>
internal static class EvaluationTelemetryOutboxKeys
{
    internal const string StorageKeyPrefix = EvaluationTelemetryOutboxStorageContract.StorageKeyPrefix;

    internal static string BuildTaskStorageKey(
        string sessionId,
        EvaluationTelemetryEnvelopeIdentity identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(identity);

        return string.Join(
            ':',
            StorageKeyPrefix.TrimEnd(':'),
            "task",
            Escape(identity.ParticipantId),
            Escape(identity.Condition),
            Escape(identity.ApplicationId),
            Escape(identity.TaskId),
            Escape(identity.RecordProvenance),
            Escape(sessionId));
    }

    internal static string BuildOutsideTaskStorageKey(string eventId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);
        return $"{StorageKeyPrefix}outside:{Escape(eventId)}";
    }

    internal static string BuildIdentityLeaseKey(EvaluationTelemetryEnvelopeIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return string.Join(
            ':',
            "sprechen.evaluation.identity.v3",
            Escape(identity.ParticipantId),
            Escape(identity.Condition),
            Escape(identity.ApplicationId),
            Escape(identity.TaskId),
            Escape(identity.RecordProvenance));
    }

    internal static bool TryReadTaskIdentity(
        string storageKey,
        out EvaluationTelemetryEnvelopeIdentity? identity,
        out string? sessionId)
    {
        identity = null;
        sessionId = null;
        var taskPrefix = $"{StorageKeyPrefix}task:";
        if (!storageKey.StartsWith(taskPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var members = storageKey[taskPrefix.Length..].Split(':');
        if (members.Length != 6)
        {
            return false;
        }

        try
        {
            identity = new EvaluationTelemetryEnvelopeIdentity(
                Uri.UnescapeDataString(members[0]),
                Uri.UnescapeDataString(members[1]),
                Uri.UnescapeDataString(members[2]),
                Uri.UnescapeDataString(members[3]),
                Uri.UnescapeDataString(members[4]));
            sessionId = Uri.UnescapeDataString(members[5]);
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    internal static string BuildOutsideTaskLeaseKey(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        return $"sprechen.evaluation.outside.v1:{Escape(storageKey)}";
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
}
