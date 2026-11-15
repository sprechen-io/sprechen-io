using Sprechen.Mda.Contracts.V1.VoiceInterfaces;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Vorprüfung des aufgelösten Modells der Sprachoberfläche.
/// </summary>
public static class VoiceInterfacePreflight
{
    /// <summary>
    /// Prüft die unterstützten Modellregeln für eine aufgelöste Sprachoberfläche.
    /// </summary>
    public static PreflightResult Validate(VoiceInterface model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new VoiceInterfacePreflightValidator().Validate(model);
    }
}
