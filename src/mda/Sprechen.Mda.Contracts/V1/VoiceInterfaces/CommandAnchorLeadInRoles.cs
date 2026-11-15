namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Gemeinsame Rollenwerte für commandAnchorLeadIns[].role in Laufzeit und Vorprüfung.
/// </summary>
public static class CommandAnchorLeadInRoles
{
    /// <summary>Die zusammengesetzte Form wird als reguläre Aktivierung angeboten.</summary>
    public const string Activation = "activation";

    /// <summary>Die zusammengesetzte Form wird zusätzlich erkannt, aber nicht als reguläre Aktivierung angeboten.</summary>
    public const string RecognitionOnly = "recognitionOnly";
}
