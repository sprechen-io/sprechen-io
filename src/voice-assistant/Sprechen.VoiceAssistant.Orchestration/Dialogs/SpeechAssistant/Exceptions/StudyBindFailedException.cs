namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

/// <summary>
/// Kennzeichnet eine fehlgeschlagene Zuordnung zur Studiensitzung, damit der Startablauf
/// einen unvollständigen Studienkontext nicht weiterverwendet.
/// </summary>
public sealed class StudyBindFailedException : SpeechAssistantDomainException
{
    public StudyBindFailedException(string message)
        : base(message)
    {
    }
}
