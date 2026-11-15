using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Verwaltet die Zulassung und Aufgabenbindung eines Prüflaufs getrennt vom Teilnehmendenkontext.
/// </summary>
public sealed class OperationalProbeSession : IOperationalProbeSession
{
    private string? _applicationId;
    private string? _taskId;

    public bool IsRequested { get; private set; }

    public OperationalProbeContext? Context { get; private set; }

    public void Request()
    {
        Reset();
        IsRequested = true;
    }

    public void AcceptContext(OperationalProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!IsRequested || Context is not null)
        {
            throw new InvalidOperationException("No operational probe is pending.");
        }

        Context = context;
    }

    public void BindStudy(OperationalProbeContext context, string applicationId, string taskId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        if (!IsRequested || !ReferenceEquals(Context, context)
            || (_applicationId is not null && (_applicationId != applicationId || _taskId != taskId)))
        {
            throw new InvalidOperationException("The operational catalog binding is no longer current.");
        }

        _applicationId = applicationId;
        _taskId = taskId;
    }

    public bool Matches(string probeId, string condition, string applicationId, string taskId)
        => IsRequested && Context is not null
            && Context.ProbeId == probeId && Context.Condition == condition
            && _applicationId is not null && _taskId is not null
            && _applicationId == applicationId && _taskId == taskId;

    public bool OwnsTaskStorageKey(string storageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        return EvaluationTelemetryOutboxKeys.TryReadTaskIdentity(storageKey, out var identity, out _)
            && Matches(identity!.ParticipantId, identity.Condition, identity.ApplicationId, identity.TaskId);
    }

    public void Reset()
    {
        IsRequested = false;
        Context = null;
        _applicationId = null;
        _taskId = null;
    }
}
