using Sprechen.Mda.Contracts.V1.FormVoice;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung eines eigenständigen Sprachdialogprofils für Formulare.
/// </summary>
public static class FormVoiceProfilePreflight
{
    /// <summary>
    /// Prüft die angegebenen Einstellungen eines Formularsprachdialogprofils.
    /// </summary>
    public static PreflightResult Validate(FormVoiceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new FormVoiceProfilePreflightValidator().Validate(profile);
    }
}
