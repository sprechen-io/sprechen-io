
namespace Sprechen.Voice.App.Tests.TestDoubles;

internal sealed class InMemoryVoiceDialogSettingsOverrideStore : IVoiceDialogSettingsOverrideStore
{
    private readonly object _gate = new();
    private bool? _deviceAllowBargeInOverride;
    private bool? _deviceAutoAdvanceAfterCommitOverride;
    private readonly Dictionary<string, bool> _applicationAllowBargeInOverrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _applicationAutoAdvanceAfterCommitOverrides = new(StringComparer.OrdinalIgnoreCase);

    public bool? DeviceAllowBargeInOverride
    {
        get
        {
            lock (_gate)
            {
                return _deviceAllowBargeInOverride;
            }
        }
    }

    public bool? DeviceAutoAdvanceAfterCommitOverride
    {
        get
        {
            lock (_gate)
            {
                return _deviceAutoAdvanceAfterCommitOverride;
            }
        }
    }

    public IReadOnlyDictionary<string, bool> ApplicationAllowBargeInOverrides
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, bool>(_applicationAllowBargeInOverrides, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public IReadOnlyDictionary<string, bool> ApplicationAutoAdvanceAfterCommitOverrides
    {
        get
        {
            lock (_gate)
            {
                return new Dictionary<string, bool>(_applicationAutoAdvanceAfterCommitOverrides, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    public ValueTask<bool?> GetDeviceAllowBargeInOverrideAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_deviceAllowBargeInOverride);
        }
    }

    public ValueTask SetDeviceAllowBargeInOverrideAsync(bool? allowBargeIn, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _deviceAllowBargeInOverride = allowBargeIn;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAllowBargeInOverridesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(_applicationAllowBargeInOverrides, StringComparer.OrdinalIgnoreCase));
        }
    }

    public ValueTask<bool?> GetApplicationAllowBargeInOverrideAsync(string applicationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(
                _applicationAllowBargeInOverrides.TryGetValue(applicationId.Trim(), out var allowBargeIn)
                    ? (bool?)allowBargeIn
                    : null);
        }
    }

    public ValueTask SetApplicationAllowBargeInOverrideAsync(
        string applicationId,
        bool? allowBargeIn,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var normalizedApplicationId = applicationId.Trim();
            if (allowBargeIn is null)
            {
                _applicationAllowBargeInOverrides.Remove(normalizedApplicationId);
            }
            else
            {
                _applicationAllowBargeInOverrides[normalizedApplicationId] = allowBargeIn.Value;
            }
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<bool?> GetDeviceAutoAdvanceAfterCommitOverrideAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(_deviceAutoAdvanceAfterCommitOverride);
        }
    }

    public ValueTask SetDeviceAutoAdvanceAfterCommitOverrideAsync(bool? autoAdvanceAfterCommit, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            _deviceAutoAdvanceAfterCommitOverride = autoAdvanceAfterCommit;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAutoAdvanceAfterCommitOverridesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult<IReadOnlyDictionary<string, bool>>(
                new Dictionary<string, bool>(_applicationAutoAdvanceAfterCommitOverrides, StringComparer.OrdinalIgnoreCase));
        }
    }

    public ValueTask<bool?> GetApplicationAutoAdvanceAfterCommitOverrideAsync(string applicationId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            return ValueTask.FromResult(
                _applicationAutoAdvanceAfterCommitOverrides.TryGetValue(applicationId.Trim(), out var autoAdvanceAfterCommit)
                    ? (bool?)autoAdvanceAfterCommit
                    : null);
        }
    }

    public ValueTask SetApplicationAutoAdvanceAfterCommitOverrideAsync(
        string applicationId,
        bool? autoAdvanceAfterCommit,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_gate)
        {
            var normalizedApplicationId = applicationId.Trim();
            if (autoAdvanceAfterCommit is null)
            {
                _applicationAutoAdvanceAfterCommitOverrides.Remove(normalizedApplicationId);
            }
            else
            {
                _applicationAutoAdvanceAfterCommitOverrides[normalizedApplicationId] = autoAdvanceAfterCommit.Value;
            }
        }

        return ValueTask.CompletedTask;
    }
}
