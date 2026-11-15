using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Sprechen.Forms.Routing;

/// <summary>
/// Schreibt Studienpfade intern auf die Index-Seite um und erhält die Browser-URL
/// für die clientseitige Auswertung der Kennung. Ungültige Kennungen und zusätzliche
/// Pfadteile werden als abgelehnte Studienlinks weitergegeben. /umfrage/probe
/// kennzeichnet einen Testlauf ohne Teilnehmerkennung.
/// </summary>
internal sealed partial class UmfrageStudyLinkRewriter
{
    public bool TryRewriteToIndex(PathString path, out PathString rewritten, out StudyLinkIdentity? identity)
    {
        rewritten = default;
        identity = null;

        if (!path.HasValue)
        {
            return false;
        }

        var segments = path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0
            || !segments[0].Equals("umfrage", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        rewritten = "/";
        if (string.Equals(path.Value, "/umfrage/probe", StringComparison.Ordinal))
        {
            identity = StudyLinkIdentity.OperationalProbe();
            return true;
        }

        if (segments.Length == 2 && TokenPattern().IsMatch(segments[1]))
        {
            identity = StudyLinkIdentity.Accepted(segments[1]);
            return true;
        }

        identity = StudyLinkIdentity.Rejected(segments.Length > 2 ? "segments" : "token");
        return true;
    }

    [GeneratedRegex("^[a-z0-9]{6,16}$")]
    private static partial Regex TokenPattern();
}
