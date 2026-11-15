using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.VoiceCommands;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Projiziert die gelieferten wirksamen Befehle in geordnete Kategoriegruppen
/// mit bereinigten, deduplizierten Ansagebezeichnungen. Die Auswahl wirksamer
/// Befehle und deren Kategoriezuordnung bleiben bei ihren Katalogen.
/// </summary>
internal sealed class FormVoiceCommandAvailableCommandsPromptBuilder
{
    private readonly Func<FormContract, string, PolitenessLevel, IEnumerable<(string CommandId, string Label)>> _commandSource;

    public FormVoiceCommandAvailableCommandsPromptBuilder()
        : this(ResolveModeledCommands)
    {
    }

    public FormVoiceCommandAvailableCommandsPromptBuilder(
        Func<FormContract, string, PolitenessLevel, IEnumerable<(string CommandId, string Label)>> commandSource)
    {
        _commandSource = commandSource ?? throw new ArgumentNullException(nameof(commandSource));
    }

    /// <summary>
    /// Gruppiert die nichtleeren Bezeichnungen nach globaler ordinaler Deduplizierung
    /// und begrenzt jede Ausgabegruppe auf die übergebene Anzahl. Gesamtzähler und
    /// Kürzungskennzeichen beschreiben die Bezeichnungen, nicht die Zahl roher Befehlsobjekte.
    /// </summary>
    public CommandListReadback BuildReadback(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        int perGroupCap)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(perGroupCap);

        var labelsByCategory = new Dictionary<VoiceCommandCategory, List<string>>();
        var seenLabels = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (commandId, label) in _commandSource(contract, isoCode, politenessLevel))
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var trimmed = label.Trim();
            if (!seenLabels.Add(trimmed))
            {
                continue;
            }

            var category = VoiceCommandCategoryCatalog.CategoryOf(commandId);
            if (!labelsByCategory.TryGetValue(category, out var labels))
            {
                labels = [];
                labelsByCategory[category] = labels;
            }

            labels.Add(trimmed);
        }

        var groups = new List<CommandListGroup>();
        var totalCount = 0;
        var truncated = false;

        foreach (var category in VoiceCommandCategoryCatalog.RenderOrder)
        {
            if (!labelsByCategory.TryGetValue(category, out var labels) || labels.Count == 0)
            {
                continue;
            }

            IReadOnlyList<string> shown = labels.Count <= perGroupCap ? labels : labels.GetRange(0, perGroupCap);
            if (shown.Count < labels.Count)
            {
                truncated = true;
            }

            totalCount += labels.Count;
            groups.Add(new CommandListGroup(category, shown, labels.Count));
        }

        return new CommandListReadback(groups, totalCount, truncated);
    }

    private static IEnumerable<(string CommandId, string Label)> ResolveModeledCommands(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        foreach (var command in FormVoiceCommandCatalogResolver.ResolveEffectiveCommands(contract))
        {
            if (!command.Enabled)
            {
                continue;
            }

            var label = FormGlobalCommandParser.ResolveCommandLabel(
                contract,
                command.CommandId,
                isoCode,
                politenessLevel);
            if (!string.IsNullOrWhiteSpace(label))
            {
                yield return (command.CommandId, label);
            }
        }
    }
}
