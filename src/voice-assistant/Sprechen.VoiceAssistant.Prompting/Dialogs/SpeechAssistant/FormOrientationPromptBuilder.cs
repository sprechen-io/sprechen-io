using System.Linq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.App.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.App.Contracts.PromptTemplates;

namespace Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

/// <summary>
/// Setzt die Orientierung beim Formulareinstieg aus Name, Feldgruppenübersicht, Fortschritt und
/// Hinweis auf die Befehlsliste zusammen. Der Fortschritt verwendet ausdrücklich <see
/// cref="ProgressCalculationBasis.AllFields"/>, damit auch Arbeit an optionalen Feldern hörbar
/// beschrieben wird. Die gesonderte Statusansage behält ihre konfigurierte Grundlage. Der
/// ausführliche Befehlsankerhinweis gehört zur Feldhilfe und wird hier nicht angehängt. Der
/// Einstieg nennt den Hilfe-Befehl selbst und danach den Hinweis auf die Befehlsliste. Zwei
/// getrennte Textsegmente erlauben dem Aufrufer, Zustand und Handlungshinweis einzeln auszugeben.
/// Aufruffrequenz und Wiedergabeabschluss liegen außerhalb des Builders.
/// </summary>
public static class FormOrientationPromptBuilder
{
    private static readonly FormsCatalogTextResolver TextResolver = new();

    public static string BuildOrientationPrompt(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract? contract,
        FormDiscoverySnapshot discovery,
        FormStatusSnapshot status,
        string isoCode,
        VoiceDialogSettings voiceSettings)
        => string.Join(
            " ",
            BuildOrientationSegments(application, currentForm, contract, discovery, status, isoCode, voiceSettings));

    /// <summary>
    /// Liefert zuerst das Zustandssegment mit Formularname, Gruppen und Fortschritt, danach den
    /// Hinweis auf die Befehlsliste. Leere Segmente entfallen. Bei unbrauchbaren Eingaben bleibt
    /// die Segmentliste leer.
    /// </summary>
    public static IReadOnlyList<string> BuildOrientationSegments(
        ApplicationDescriptor application,
        FormDescriptor currentForm,
        FormContract? contract,
        FormDiscoverySnapshot discovery,
        FormStatusSnapshot status,
        string isoCode,
        VoiceDialogSettings voiceSettings)
    {
        if (application is null || currentForm is null || discovery is null || status is null
            || voiceSettings is null || string.IsNullOrWhiteSpace(isoCode))
        {
            return [];
        }

        var iso = FormStatusPromptResolver.NormalizeIsoCode(isoCode);
        var politeness = voiceSettings.GetPolitenessLevel();

        var formName = TextResolver.ResolveFormDisplayName(application, currentForm, iso, politeness);
        var frame = FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.FormOrientation,
            iso,
            politeness,
            new Dictionary<string, string?>
            {
                [FormPromptTemplateParameterNames.FormName] = formName
            });

        var fieldGroups = FormStatusPromptBuilder.BuildFieldGroupListPrompt(discovery, contract, iso, voiceSettings);
        var progressLead = FormStatusPromptTextFormatter.FormatProgressText(
            contract, status, ProgressCalculationBasis.AllFields, iso, politeness);
        var helpExample = FormStatusPromptResolver.ResolveOrFallback(
            contract,
            FormDialogPromptIds.OrientationHelpExample,
            iso,
            politeness);
        var commandsPointer = FormHelpCommandsPointerPromptBuilder.BuildPrompt(contract, iso, politeness);

        var orientationSegment = string.Join(
            " ",
            new[] { frame, fieldGroups, progressLead }
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(EnsureSentenceTerminator));
        var commandsSegment = string.Join(
            " ",
            new[] { helpExample, commandsPointer }
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .Select(EnsureSentenceTerminator));

        return new[] { orientationSegment, commandsSegment }
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }

    // Der wiederverwendete Fortschrittstext ist sonst in eine Vorlage mit abschließendem Punkt
    // eingebettet. Hier erhalten die zusammengesetzten Sätze bei Bedarf ein eigenes Satzende, damit
    // ihre Grenzen bei der Ausgabe erhalten bleiben.
    private static string EnsureSentenceTerminator(string segment)
    {
        var trimmed = segment.TrimEnd();
        return trimmed.Length > 0 && ".!?".IndexOf(trimmed[^1]) < 0 ? trimmed + "." : trimmed;
    }
}
