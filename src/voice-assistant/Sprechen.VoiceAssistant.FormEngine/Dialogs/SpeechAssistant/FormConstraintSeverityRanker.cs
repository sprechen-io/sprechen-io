using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Fehlern, Warnungen und Informationen die Ränge 3, 2 und 1 zu. Unbekannte Schweregrade erhalten den Rang 0.
/// </summary>
internal sealed class FormConstraintSeverityRanker
{
    public int Rank(string severity)
    {
        if (string.Equals(severity, SeverityLevels.Error, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (string.Equals(severity, SeverityLevels.Warning, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (string.Equals(severity, SeverityLevels.Info, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        return 0;
    }
}
