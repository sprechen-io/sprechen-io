namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Stellt den Klangführer zusammen und steuert die Wiedergabe seiner Klangbeispiele.
/// </summary>
public interface IEarconGuideWorkflow
{
    EarconGuideState GetEarconGuideState();

    EarconGuideState GetEarconGuideState(string? preferredIsoCode);

    ValueTask PreviewEarconAsync(EarconId earcon, CancellationToken cancellationToken);

    ValueTask StopEarconPreviewsAsync(CancellationToken cancellationToken);
}
