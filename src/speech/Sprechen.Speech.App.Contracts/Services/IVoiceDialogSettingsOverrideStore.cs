namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verwaltet geräteweite und anwendungsspezifische Abweichungen für Sprachunterbrechung und automatisches Fortschreiten.
/// </summary>
public interface IVoiceDialogSettingsOverrideStore
{
    ValueTask<bool?> GetDeviceAllowBargeInOverrideAsync(CancellationToken cancellationToken);

    ValueTask SetDeviceAllowBargeInOverrideAsync(bool? allowBargeIn, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAllowBargeInOverridesAsync(CancellationToken cancellationToken);

    ValueTask<bool?> GetApplicationAllowBargeInOverrideAsync(string applicationId, CancellationToken cancellationToken);

    ValueTask SetApplicationAllowBargeInOverrideAsync(string applicationId, bool? allowBargeIn, CancellationToken cancellationToken);

    ValueTask<bool?> GetDeviceAutoAdvanceAfterCommitOverrideAsync(CancellationToken cancellationToken);

    ValueTask SetDeviceAutoAdvanceAfterCommitOverrideAsync(bool? autoAdvanceAfterCommit, CancellationToken cancellationToken);

    ValueTask<IReadOnlyDictionary<string, bool>> GetApplicationAutoAdvanceAfterCommitOverridesAsync(CancellationToken cancellationToken);

    ValueTask<bool?> GetApplicationAutoAdvanceAfterCommitOverrideAsync(string applicationId, CancellationToken cancellationToken);

    ValueTask SetApplicationAutoAdvanceAfterCommitOverrideAsync(string applicationId, bool? autoAdvanceAfterCommit, CancellationToken cancellationToken);
}
