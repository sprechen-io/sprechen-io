using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.EvaluationTelemetry;

/// <summary>
/// Verwaltet gespeicherte Ereignisse und ihren Versand unter exklusiven Identitätssperren. Laufzeitzustand und Signale koordinieren Aufzeichnung, Empfangsbestätigung und lokalen Abschluss eines Durchlaufs.
/// </summary>
internal sealed partial class EvaluationTelemetryTaskOutbox : IDisposable
{
    private static readonly JsonSerializerOptions CheckpointSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private readonly object _gate = new();
    private readonly Dictionary<string, EvaluationTelemetryOutboxCheckpoint> _checkpoints = new(StringComparer.Ordinal);
    private readonly Dictionary<string, EvaluationTelemetryTaskOwner> _taskOwners = new(StringComparer.Ordinal);
    private readonly Dictionary<string, InvalidDataException> _identityQuarantineFailures = new(StringComparer.Ordinal);
    private readonly HashSet<string> _activeOutsideTaskPumps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _suppressedRuns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TaskCompletionSource> _checkpointSignals = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly IOperationalProbeSession _operationalProbeSession;
    private readonly IStudyTelemetryContext _studyTelemetryContext;
    private readonly IEvaluationTelemetryOutboxStore _store;
    private readonly IEvaluationTelemetryDeliveryTransport _transport;
    private readonly IEvaluationTelemetryExclusiveLease _exclusiveLease;
    private readonly IEvaluationTelemetryRetryDelay _retryDelay;
    private readonly EvaluationTelemetryStoredEventFactory _eventFactory;
    private readonly EvaluationTelemetryCheckpointValidator _validator;
    private readonly EvaluationTelemetryOptions _options;
    private readonly ILogger<EvaluationTelemetry> _logger;
    private Task? _initializationTask;

    public EvaluationTelemetryTaskOutbox(
        IStudyTelemetryContext studyTelemetryContext,
        EvaluationTelemetryOutboxInfrastructure infrastructure,
        EvaluationTelemetryStoredEventFactory eventFactory,
        EvaluationTelemetryCheckpointValidator validator,
        IOperationalProbeSession operationalProbeSession)
    {
        _operationalProbeSession = operationalProbeSession ?? throw new ArgumentNullException(nameof(operationalProbeSession));
        _studyTelemetryContext = studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));
        ArgumentNullException.ThrowIfNull(infrastructure);
        _store = infrastructure.Store;
        _transport = infrastructure.Transport;
        _exclusiveLease = infrastructure.ExclusiveLease;
        _retryDelay = infrastructure.RetryDelay;
        _eventFactory = eventFactory ?? throw new ArgumentNullException(nameof(eventFactory));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _options = infrastructure.Options;
        _logger = infrastructure.Logger;
    }

    public void Dispose()
    {
        _shutdown.Cancel();
        foreach (var owner in SnapshotTaskOwners())
        {
            var exception = new ObjectDisposedException(nameof(EvaluationTelemetryTaskOutbox));
            owner.TerminalAcceptance.TrySetException(exception);
            owner.LocalCommit.TrySetException(exception);
        }

        _shutdown.Dispose();
    }

    private bool ShouldCaptureCurrentRun(EvaluationTelemetryEnvelopeIdentity identity)
    {
        if (!_options.IsEnabled || !CanRestoreIdentity(identity))
        {
            return false;
        }

        return identity.RecordProvenance == TelemetryRecordProvenances.OperationalProbe
            || !_options.RequireStudyContext
            || !string.IsNullOrWhiteSpace(_studyTelemetryContext.ParticipantId);
    }

    private bool CanRestoreIdentity(EvaluationTelemetryEnvelopeIdentity identity)
    {
        if (_operationalProbeSession.IsRequested)
        {
            return string.IsNullOrWhiteSpace(_studyTelemetryContext.ParticipantId)
                && identity.RecordProvenance == TelemetryRecordProvenances.OperationalProbe
                && _operationalProbeSession.Matches(
                    identity.ParticipantId, identity.Condition, identity.ApplicationId, identity.TaskId);
        }

        return identity.RecordProvenance != TelemetryRecordProvenances.OperationalProbe;
    }
}
