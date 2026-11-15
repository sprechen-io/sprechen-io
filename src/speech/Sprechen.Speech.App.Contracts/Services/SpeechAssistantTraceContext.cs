namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Bündelt Diagnosesenke, Korrelationskennung und Bereich einer Dialogausführung.
/// Die Kennung wird getrimmt. Ein leerer Wert wird zu null.
/// </summary>
public sealed class SpeechAssistantTraceContext
{
    public SpeechAssistantTraceContext(
        ISpeechAssistantTrace? trace,
        string? correlationId,
        SpeechAssistantTraceScope scope)
    {
        Trace = trace;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim();
        Scope = scope;
    }

    public ISpeechAssistantTrace? Trace { get; }

    public string? CorrelationId { get; }

    public SpeechAssistantTraceScope Scope { get; }
}
