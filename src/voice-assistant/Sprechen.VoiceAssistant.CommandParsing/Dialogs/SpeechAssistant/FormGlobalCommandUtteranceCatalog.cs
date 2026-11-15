using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die lokalisierten Äußerungsvarianten globaler Befehle aus dem Katalog des Parsers bereit.
/// </summary>
/// <remarks>
/// Die Vorlesebezeichnung aus <see cref="FormGlobalCommandLabelResolver"/> benennt einen Befehl einmal.
/// Für Erkennungshinweise werden dagegen alle Katalogvarianten benötigt, damit auch
/// die vom Parser akzeptierten Synonyme verstärkt werden können.
/// </remarks>
public static class FormGlobalCommandUtteranceCatalog
{
    /// <summary>
    /// Liefert unterschiedliche Äußerungen zu <paramref name="commandId"/> für <paramref name="isoCode"/>
    /// in Katalogreihenfolge. Das Ergebnis bleibt leer, wenn der Befehl fehlt oder keine Variante
    /// für Sprache und Höflichkeitsstufe aufgelöst werden kann.
    /// </summary>
    public static IReadOnlyList<string> GetUtterances(
        FormContract contract,
        string commandId,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (string.IsNullOrWhiteSpace(commandId))
        {
            return [];
        }

        var normalizedCommandId = commandId.Trim();
        var resolvedUtterances = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var command in FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract))
        {
            if (!string.Equals(command.CommandId, normalizedCommandId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var catalogUtterance in command.Utterances)
            {
                if (FormGlobalCommandUtteranceResolver.TryResolve(command, catalogUtterance, isoCode, politenessLevel, out var resolved)
                    && !string.IsNullOrWhiteSpace(resolved)
                    && seen.Add(resolved.Trim()))
                {
                    resolvedUtterances.Add(resolved.Trim());
                }
            }
        }

        return resolvedUtterances;
    }

    /// <summary>
    /// Liefert die unterschiedlichen, für <paramref name="isoCode"/> auflösbaren Äußerungen
    /// aller Befehle in <paramref name="commands"/> in Katalogreihenfolge.
    /// </summary>
    /// <remarks>
    /// Ablaufspezifische und globale Kataloge verwenden dieselbe aufgelöste Befehlsdefinition.
    /// So bleiben Standardsprache, unterstützte Sprachen, strikte Lokalisierung und Höflichkeitsstufe
    /// beim gemeinsamen Auflöser dieser Assembly und stimmen mit dem Parser überein.
    /// </remarks>
    public static IReadOnlyList<string> GetUtterances(
        IReadOnlyList<ResolvedGlobalCommandDefinition> commands,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var resolvedUtterances = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var command in commands)
        {
            foreach (var catalogUtterance in command.Utterances)
            {
                if (FormGlobalCommandUtteranceResolver.TryResolve(command, catalogUtterance, isoCode, politenessLevel, out var resolved)
                    && !string.IsNullOrWhiteSpace(resolved)
                    && seen.Add(resolved.Trim()))
                {
                    resolvedUtterances.Add(resolved.Trim());
                }
            }
        }

        return resolvedUtterances;
    }
}
