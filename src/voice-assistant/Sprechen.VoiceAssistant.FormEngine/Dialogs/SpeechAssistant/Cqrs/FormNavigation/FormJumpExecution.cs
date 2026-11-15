namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Hält das Ergebnis eines Sprungversuchs fest. Bei einem nur vorlesbaren Ziel bleibt dessen Auflösung für die anschließende Ansage erhalten.
/// </summary>
internal sealed record FormJumpExecution(FormJumpExecutionKind Kind, FormJumpTargetResolution? Resolution)
{
    internal static FormJumpExecution Focused()
        => new(FormJumpExecutionKind.Focused, Resolution: null);

    internal static FormJumpExecution NotFound()
        => new(FormJumpExecutionKind.NotFound, Resolution: null);

    internal static FormJumpExecution AudibleOnly(FormJumpTargetResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(resolution);
        return new(FormJumpExecutionKind.AudibleOnlyTarget, resolution);
    }
}
