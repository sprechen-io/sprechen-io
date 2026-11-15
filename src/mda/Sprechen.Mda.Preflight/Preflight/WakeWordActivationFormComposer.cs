using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Verbindet Einleitungswörter der gewünschten Rolle mit dem Aktivierungsnamen
/// über ein Leerzeichen. Die gemeinsame Zusammensetzung vermeidet getrennt gepflegte
/// Formlisten. Die aufrufende Stelle übernimmt die Normalisierung.
/// </summary>
public sealed class WakeWordActivationFormComposer
{
    public IReadOnlyList<string> ComposeActivationForms(WakeWordCatalog catalog, WakeWordLanguageProfile profile)
        => Compose(catalog, profile, CommandAnchorLeadInRoles.Activation);

    public IReadOnlyList<string> ComposeRecognitionOnlyActivationForms(WakeWordCatalog catalog, WakeWordLanguageProfile profile)
        => Compose(catalog, profile, CommandAnchorLeadInRoles.RecognitionOnly);

    private static IReadOnlyList<string> Compose(WakeWordCatalog catalog, WakeWordLanguageProfile profile, string role)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(profile);

        var name = profile.WakeWordName ?? string.Empty;
        var composed = new List<string>();

        foreach (var leadIn in catalog.CommandAnchorLeadIns)
        {
            if (leadIn is null)
            {
                continue;
            }

            if (!string.Equals(leadIn.Role, role, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            composed.Add($"{leadIn.LeadIn} {name}");
        }

        return composed;
    }
}
