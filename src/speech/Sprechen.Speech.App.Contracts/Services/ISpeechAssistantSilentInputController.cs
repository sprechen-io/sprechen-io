namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Steuert stille Eingabeaktionen mit der erwarteten Ansichtsidentität zur Zuordnung zum aktuellen Formularzustand.
/// </summary>
public interface ISpeechAssistantSilentInputController
{
    ValueTask EnterAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken);

    ValueTask SubmitAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        string value,
        CancellationToken cancellationToken);

    ValueTask ClearAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken);

    ValueTask CancelAsync(
        SpeechAssistantSilentInputSnapshotIdentity expected,
        CancellationToken cancellationToken);
}
