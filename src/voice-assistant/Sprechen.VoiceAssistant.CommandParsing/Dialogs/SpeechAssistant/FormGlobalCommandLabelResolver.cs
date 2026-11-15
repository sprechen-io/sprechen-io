using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt die modellierte Vorlesebezeichnung eines Befehls für hörbare Ansagen.
/// </summary>
/// <remarks>
/// Die Befehlsliste verwendet diese Bezeichnung für die Ansage.
/// Die Erkennung verwendet weiterhin die Befehlsäußerungen und wird hier nicht verändert.
/// </remarks>
internal sealed class FormGlobalCommandLabelResolver
{
    public string? ResolveCommandLabel(
        FormContract contract,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var normalizedCommandId = commandId.Trim();

        foreach (var command in FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract))
        {
            if (!string.Equals(command.CommandId, normalizedCommandId, StringComparison.Ordinal))
            {
                continue;
            }

            // Nur die modellierte Vorleseform bestimmt die Ansage. Eine Umordnung der
            // Erkennungsäußerungen verändert die Ansage nicht.
            if (StatesASpokenFormFor(command.SpokenForm, isoCode)
                && FormGlobalCommandUtteranceResolver.TryResolve(command, command.SpokenForm!, isoCode, politenessLevel, out var spoken)
                && !string.IsNullOrWhiteSpace(spoken))
            {
                return spoken.Trim();
            }

            return null;
        }

        return null;
    }

    /// <summary>
    /// Prüft, ob eine Vorleseform für die angeforderte Basissprache modelliert ist.
    /// </summary>
    /// <remarks>
    /// Für Ansagen genügt der Rückfall auf die Katalogstandardsprache nicht: Ohne eigene
    /// Vorleseform bleibt die Bezeichnung aus, statt eine andere Sprache oder eine
    /// Erkennungsäußerung vorzulesen.
    /// </remarks>
    private static bool StatesASpokenFormFor(LocalizedText? spokenForm, string isoCode)
    {
        if (spokenForm is null)
        {
            return false;
        }

        var baseLanguage = BaseLanguageOf(isoCode);
        foreach (var key in spokenForm.Keys)
        {
            if (string.Equals(BaseLanguageOf(key), baseLanguage, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string BaseLanguageOf(string languageTag)
    {
        var trimmed = languageTag.Trim();
        var cut = trimmed.IndexOfAny(['@', '-']);
        return cut < 0 ? trimmed : trimmed[..cut];
    }
}
