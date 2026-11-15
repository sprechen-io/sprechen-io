namespace Sprechen.Forms.Pages;

/// <summary>
/// Übermittelt Teilnehmerkennung, Aufgabenkennung und Studienbedingung
/// vom Ladeablauf an das Seitenmodell.
/// </summary>
internal sealed class IndexPageStudyParameters
{
    public IndexPageStudyParameters(string participantId, string taskId, string condition)
    {
        ParticipantId = participantId;
        TaskId = taskId;
        Condition = condition;
    }

    public string ParticipantId { get; }

    public string TaskId { get; }

    public string Condition { get; }
}
