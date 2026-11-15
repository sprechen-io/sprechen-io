namespace Sprechen.Forms.Pages;

/// <summary>
/// Übermittelt die optionale Studienkennung und die Kennzeichnung eines Testlaufs
/// an den Seitenablauf. Die Zulässigkeit der Kombination wird dort geprüft.
/// </summary>
internal sealed record IndexPageEntryAdmission(string? StudyToken = null, bool IsOperationalProbe = false);
