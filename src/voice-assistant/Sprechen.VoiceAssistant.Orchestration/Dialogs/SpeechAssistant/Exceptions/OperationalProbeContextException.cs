namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

/// <summary>
/// Kennzeichnet einen technischen Testlauf ohne gültigen Testkontext oder mit
/// Teilnehmeridentität, damit der Start mit dieser unzulässigen Zuordnung abbricht.
/// </summary>
public sealed class OperationalProbeContextException : SpeechAssistantDomainException
{
    public OperationalProbeContextException()
        : base("The operational entry has no valid probe context or has a participant identity.")
    {
    }
}
