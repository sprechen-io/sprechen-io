using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet der modellierten stillen Eingabealternative den Ansageschlüssel für Tastatur oder
/// Punktschrift zu. Unbekannte Alternativen erhalten keine erfundene Beschriftung.
/// </summary>
internal sealed class FormSilentInputFallbackLabelPromptIdResolver
{
    public string? Resolve(string silentInputFallback)
    {
        if (string.Equals(silentInputFallback, SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard, StringComparison.Ordinal))
        {
            return FormDialogPromptIds.SilentInputLabelKeyboard;
        }

        if (string.Equals(silentInputFallback, SensitiveDataPolicyDefaults.SilentInputFallbackBraille, StringComparison.Ordinal))
        {
            return FormDialogPromptIds.SilentInputLabelBraille;
        }

        return null;
    }
}
