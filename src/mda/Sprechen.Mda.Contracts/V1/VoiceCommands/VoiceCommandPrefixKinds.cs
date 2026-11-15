namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Übertragungswerte für VoiceCommandPrefixDefinition.Kind zur eindeutigen Unterscheidung der Präfixkategorien.
/// </summary>
public static class VoiceCommandPrefixKinds
{
    /// <summary>
    /// Präfix für ausdrücklich angekündigte Befehle, beispielsweise „Befehl:“.
    /// </summary>
    public const string Command = "command";

    /// <summary>
    /// Reservierte frühere Kategorie für Werteingaben. Inaktiv und nicht erneut zu vergeben.
    /// </summary>
    public const string Input = "input";
}
