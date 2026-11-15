namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die Entscheidung über ein mehrdeutiges Befehlssignal mit der
/// gewählten Auflösungsstrategie für den nachfolgenden Ablauf.
/// </summary>
public readonly record struct AmbiguityResolutionResult(
    bool ExecuteCommand,
    bool EmitAcceptedTelemetry,
    string ResolutionStrategy)
{
    public static AmbiguityResolutionResult None()
        => new(
            ExecuteCommand: false,
            EmitAcceptedTelemetry: false,
            ResolutionStrategy: "none");
}

