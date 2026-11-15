namespace Sprechen.Forms.Routing;

/// <summary>
/// Übermittelt die Teilnehmerkennung, einen Testlauf oder den Ablehnungsgrund
/// des Studienlinks innerhalb der aktuellen HTTP-Anfrage.
/// </summary>
internal sealed class StudyLinkIdentity
{
    private StudyLinkIdentity(string? token, string? rejectionReason, bool operationalProbe)
    {
        Token = token;
        RejectionReason = rejectionReason;
        IsOperationalProbe = operationalProbe;
    }

    public string? Token { get; }

    public string? RejectionReason { get; }

    public bool IsRejected => RejectionReason is not null;

    public bool IsOperationalProbe { get; }

    public static StudyLinkIdentity Accepted(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return new StudyLinkIdentity(token, null, false);
    }

    public static StudyLinkIdentity OperationalProbe() => new(null, null, true);

    public static StudyLinkIdentity Rejected(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new StudyLinkIdentity(null, reason, false);
    }
}
