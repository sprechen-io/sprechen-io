using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.VoiceCommands;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Ermittelt wirksame Formularbefehle aus modellierten Katalogvorgaben und formularbezogenen Überschreibungen.
/// </summary>
public static class FormVoiceCommandCatalogResolver
{
    private static readonly IReadOnlyDictionary<string, string> CatalogPathsByModelId =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [FormVoiceCommandModelIds.Default] = "navigationsmodell/reference-contracts/voice-commands/voice-commands.default.reference.json",
        };

    private static readonly Lazy<IReadOnlyDictionary<string, VoiceCommandCatalog>> CatalogsByModelId = new(LoadCatalogs);

    public static VoiceCommandCatalog ResolveCatalog(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var requestedModelId = contract.Projections?.Voice?.Hints?.CommandModelId?.Trim();
        if (!string.IsNullOrWhiteSpace(requestedModelId)
            && CatalogsByModelId.Value.TryGetValue(requestedModelId, out var catalog))
        {
            return catalog;
        }

        return CatalogsByModelId.Value[FormVoiceCommandModelIds.Default];
    }

    public static IReadOnlyList<ResolvedGlobalCommandDefinition> ResolveEffectiveCommands(FormContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);

        var catalog = ResolveCatalog(contract);
        var effectiveCommands = new Dictionary<string, ResolvedGlobalCommandDefinition>(StringComparer.Ordinal);

        AddCatalogCommands(effectiveCommands, catalog);
        AddContractOverrides(effectiveCommands, contract, catalog);

        return effectiveCommands.Values.ToArray();
    }

    public static IReadOnlyList<ResolvedGlobalCommandDefinition> ResolveDefaultCommands()
    {
        var catalog = CatalogsByModelId.Value[FormVoiceCommandModelIds.Default];
        var commands = new Dictionary<string, ResolvedGlobalCommandDefinition>(StringComparer.Ordinal);
        AddCatalogCommands(commands, catalog);
        return commands.Values.ToArray();
    }

    public static IReadOnlyList<ResolvedGlobalCommandPrefixDefinition> ResolvePrefixes(FormContract contract, string kind)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        return ResolvePrefixes(ResolveCatalog(contract), kind);
    }

    public static IReadOnlyList<ResolvedGlobalCommandPrefixDefinition> ResolveDefaultPrefixes(string kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        return ResolvePrefixes(CatalogsByModelId.Value[FormVoiceCommandModelIds.Default], kind);
    }

    private static IReadOnlyList<ResolvedGlobalCommandPrefixDefinition> ResolvePrefixes(VoiceCommandCatalog catalog, string kind)
    {
        var prefixes = new List<ResolvedGlobalCommandPrefixDefinition>();
        var normalizedKind = kind.Trim();

        foreach (var prefix in catalog.Prefixes)
        {
            if (!string.Equals(prefix.Kind, normalizedKind, StringComparison.Ordinal)
                || prefix.Utterances.Count == 0)
            {
                continue;
            }

            prefixes.Add(
                new ResolvedGlobalCommandPrefixDefinition(
                    prefix.Kind,
                    prefix.Utterances,
                    catalog.SupportedLanguages,
                    catalog.DefaultLanguage,
                    catalog.StrictLocalization));
        }

        return prefixes;
    }

    private static void AddCatalogCommands(
        IDictionary<string, ResolvedGlobalCommandDefinition> effectiveCommands,
        VoiceCommandCatalog catalog)
    {
        foreach (var command in catalog.Commands)
        {
            if (command.CommandId == default
                || command.Utterances.Count == 0)
            {
                continue;
            }

            var commandId = command.CommandId.Value.Trim();
            effectiveCommands[commandId] = new ResolvedGlobalCommandDefinition(
                commandId,
                command.Utterances,
                command.Enabled is not false,
                catalog.SupportedLanguages,
                catalog.DefaultLanguage,
                catalog.StrictLocalization,
                command.SpokenForm);
        }
    }

    private static void AddContractOverrides(
        IDictionary<string, ResolvedGlobalCommandDefinition> effectiveCommands,
        FormContract contract,
        VoiceCommandCatalog fallbackCatalog)
    {
        var overrides = contract.Projections?.Voice?.Hints?.GlobalCommands;
        if (overrides is not { Count: > 0 })
        {
            return;
        }

        var supportedLanguages = contract.SupportedLanguages.Count > 0
            ? contract.SupportedLanguages
            : fallbackCatalog.SupportedLanguages;
        var defaultLanguage = contract.DefaultLanguage ?? fallbackCatalog.DefaultLanguage;

        foreach (var command in overrides)
        {
            if (command.CommandId == default
                || command.Utterances.Count == 0)
            {
                continue;
            }

            var commandId = command.CommandId.Value.Trim();

            // Formularbezogene Überschreibungen übernehmen die Vorleseform des Katalogbefehls,
            // da ihre Vertragsdefinition keine eigene Vorleseform enthält. So bleibt die hörbare
            // Bezeichnung erhalten, auch wenn Erkennungsäußerungen ersetzt werden.
            var inheritedSpokenForm = effectiveCommands.TryGetValue(commandId, out var baseline)
                ? baseline.SpokenForm
                : null;

            effectiveCommands[commandId] = new ResolvedGlobalCommandDefinition(
                commandId,
                command.Utterances,
                command.Enabled is not false,
                supportedLanguages,
                defaultLanguage,
                contract.StrictLocalization,
                inheritedSpokenForm);
        }
    }

    private static IReadOnlyDictionary<string, VoiceCommandCatalog> LoadCatalogs()
    {
        var catalogs = new Dictionary<string, VoiceCommandCatalog>(StringComparer.Ordinal);
        foreach (var item in CatalogPathsByModelId)
        {
            catalogs[item.Key] = LoadCatalog(item.Value, item.Key);
        }

        return catalogs;
    }

    private static VoiceCommandCatalog LoadCatalog(string relativePath, string expectedModelId)
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(relativePath);
        var catalog = JsonSerializer.Deserialize<VoiceCommandCatalog>(json)
            ?? throw new InvalidOperationException($"Embedded command catalog '{relativePath}' could not be deserialized.");
        if (!string.Equals(catalog.CommandModelId, expectedModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Embedded command catalog model mismatch for '{relativePath}'. Expected '{expectedModelId}', got '{catalog.CommandModelId}'.");
        }

        return catalog;
    }
}

