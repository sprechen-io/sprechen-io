namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Ermittelt den Laufzeitwert von <see cref="HtmlProjection.Profile"/>. Fehlende oder
/// leere Angaben ergeben <see cref="HtmlProjectionProfiles.A11yFirst"/>. Unbekannte
/// Werte werden mit E_HTML_PROJECTION_PROFILE_UNSUPPORTED abgelehnt.
/// </summary>
public static class HtmlProjectionProfileResolver
{
    /// <summary>
    /// Stabiler Fehlercode, wenn ein HtmlProjection.profile nicht im kontrollierten Vokabular
    /// von <see cref="HtmlProjectionProfiles"/> liegt.
    /// </summary>
    public const string ErrorCodeUnsupportedProfile = "E_HTML_PROJECTION_PROFILE_UNSUPPORTED";

    /// <summary>
    /// Liefert bei fehlendem oder leerem Profil das Standardprofil. Andere
    /// nicht exakt bekannte Werte lösen eine InvalidOperationException mit Fehlercode aus.
    /// </summary>
    public static string Resolve(HtmlProjection? projection)
    {
        if (projection is null)
        {
            return HtmlProjectionProfiles.A11yFirst;
        }

        var profile = projection.Profile;
        if (string.IsNullOrWhiteSpace(profile))
        {
            return HtmlProjectionProfiles.A11yFirst;
        }

        if (string.Equals(profile, HtmlProjectionProfiles.A11yFirst, StringComparison.Ordinal))
        {
            return HtmlProjectionProfiles.A11yFirst;
        }

        throw new InvalidOperationException(
            $"{ErrorCodeUnsupportedProfile}: HtmlProjection.profile value '{profile}' is not part of the controlled vocabulary (only '{HtmlProjectionProfiles.A11yFirst}').");
    }
}
