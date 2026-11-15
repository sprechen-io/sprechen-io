using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Baut ein lokalisiertes Eingabebeispiel für den 8. Januar 1980. Deutsch und Englisch
/// verwenden ausgeschriebene Jahresformen aus <see cref="FormSpokenYearRenderer"/>,
/// Französisch, Spanisch und Italienisch ein Ziffernjahr. Derselbe Renderer wird beim
/// Vorlesen von Datumswerten verwendet. Die Katalogvorlage ergänzt den einleitenden Text.
/// </summary>
public static class FormDateEntryExamplePromptBuilder
{
    private static readonly FormPromptComposition PromptComposition = FormPromptComposition.Default;

    // Alle unterstützten Sprachen verwenden dasselbe Beispieldatum. Die Jahresform kommt aus
    // demselben Renderer wie die Datumsvorlesung.
    private const int ExampleYear = 1980;

    /// <summary>
    /// Liefert den Beispielsatz für den 8. Januar 1980 oder <c>null</c> für eine Sprache
    /// ohne Beispiel. Deutsch und Englisch erhalten eine ausgeschriebene Jahresform,
    /// Französisch, Spanisch und Italienisch ein Ziffernjahr.
    /// </summary>
    public static string? GetDateEntryExamplePhrase(string isoCode)
    {
        var year = FormSpokenYearRenderer.RenderSpokenYear(ExampleYear, isoCode);
        return LanguagePrefix(isoCode) switch
        {
            "de" => $"achter Januar {year}",
            "en" => $"January eighth {year}",
            "fr" => $"le 8 janvier {year}",
            "es" => $"el 8 de enero de {year}",
            "it" => $"l'8 gennaio {year}",
            _ => null
        };
    }

    /// <summary>
    /// Rahmt den Beispielsatz als lokalisierte Ansage für das Datumsfeld ein. Ohne Sprachbeispiel
    /// wird <c>null</c> zurückgegeben.
    /// </summary>
    public static string? GetDateEntryExamplePrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var phrase = GetDateEntryExamplePhrase(isoCode);
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
            FormDialogPromptIds.DateFieldEntryExample,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DateFieldEntryExample, parameters),
            parameters);
    }

    /// <summary>
    /// Formatiert den Hinweis auf ein vollständiges Datum mit demselben Satz im Parameter
    /// <c>{{example}}</c>, ohne selbst eine Reihenfolge der Datumsteile vorzuschreiben. Der
    /// Aufrufer steuert die einmalige Einführung beim ersten Datumsfeld. Ohne Sprachbeispiel bleibt
    /// der Rückgabewert <c>null</c>.
    /// </summary>
    public static string? GetDateEntryDecompositionHintPrompt(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var phrase = GetDateEntryExamplePhrase(isoCode);
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
            FormDialogPromptIds.DateFieldEntryDecompositionHint,
            isoCode,
            politenessLevel,
            PromptComposition.ResolveFallbackPrompt(isoCode, FormDialogPromptIds.DateFieldEntryDecompositionHint, parameters),
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
