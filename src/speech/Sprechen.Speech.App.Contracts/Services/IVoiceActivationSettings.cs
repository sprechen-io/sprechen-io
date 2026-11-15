namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Verwaltet abweichende Aktivierungsformen, mit denen die konfigurierte Sprachaktivierung angepasst werden kann.
/// </summary>
public interface IVoiceActivationSettings
{
    ValueTask<string[]> GetWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken);

    ValueTask SetWakeWordActivationFormsOverrideAsync(string[] wakeWordActivationForms, CancellationToken cancellationToken);

    ValueTask ClearWakeWordActivationFormsOverrideAsync(CancellationToken cancellationToken);
}
