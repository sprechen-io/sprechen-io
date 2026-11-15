using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut ein lokalisiertes Eingabebeispiel für 14:30 Uhr. Englisch verwendet
/// die Zwölfstundenform mit Tageshälfte, Deutsch, Französisch, Spanisch und Italienisch
/// die Vierundzwanzigstundenform. Katalogvorlagen verwenden denselben Beispielsatz
/// für einen kurzen Hinweis oder die Einführung in diese Uhrzeitkonvention.
/// </summary>
public static class FormTimeEntryExamplePromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    /// <summary>
    /// Liefert das Uhrzeitbeispiel oder <c>null</c> für eine Sprache ohne Beispiel.
    /// Englisch verwendet „2:30 PM“, die vier anderen unterstützten Sprachen „14:30“.
    /// </summary>
    public static string? GetTimeEntryExamplePhrase(string isoCode)
        => LanguagePrefix(isoCode) switch
        {
            "en" => "2:30 PM",
            "de" => "14:30",
            "fr" => "14:30",
            "es" => "14:30",
            "it" => "14:30",
            _ => null
        };

    /// <summary>
    /// Rahmt das vorhandene Uhrzeitbeispiel als lokalisierte Ansage ein. Ohne Sprachbeispiel bleibt
    /// der Rückgabewert <c>null</c>.
    /// </summary>
    public static string? GetTimeEntryExamplePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var phrase = GetTimeEntryExamplePhrase(isoCode);
        if (phrase is null)
        {
            return null;
        }

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.Example] = phrase
        };

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.TimeFieldEntryExample,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.TimeFieldEntryExample, parameters),
            parameters);
    }

    /// <summary>
    /// Formatiert die Einführung in die gewählte Uhrzeitkonvention und setzt den gemeinsamen
    /// Beispielsatz über <c>{{example}}</c> ein. Die einmalige Einführung am ersten Uhrzeitfeld
    /// steuert der Aufrufer. Ohne Sprachbeispiel wird <c>null</c> geliefert.
    /// </summary>
    public static string? GetTimeEntryStructureHintPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var phrase = GetTimeEntryExamplePhrase(isoCode);
        if (phrase is null)
        {
            return null;
        }

        var parameters = new Dictionary<string, string?>
        {
            [FormPromptTemplateParameterNames.Example] = phrase
        };

        return PromptComposition.ResolvePromptOrFallback(
            contract,
            FormDialogPromptIds.TimeFieldEntryStructureHint,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.TimeFieldEntryStructureHint, parameters),
            parameters);
    }

    private static string LanguagePrefix(string isoCode)
    {
        if (string.IsNullOrWhiteSpace(isoCode))
        {
            return string.Empty;
        }

        var trimmed = isoCode.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var prefix = separator >= 0 ? trimmed[..separator] : trimmed;
        return prefix.ToLowerInvariant();
    }
}
