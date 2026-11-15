using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;
using Sprechen.Mda.Contracts.V1.VoiceCommands;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erschließt den modellierten Befehlskatalog für die Bearbeitung diktierter Ziffern.
/// </summary>
public static class DigitsWorkflowCommandCatalogResolver
{
    private const string CommandModelId = "voice.digits.workflow.v1";
    private const string RelativePath = "navigationsmodell/reference-contracts/voice-commands/voice-commands.digits-workflow.reference.json";

    private static readonly Lazy<VoiceCommandCatalog> Catalog = new(LoadCatalog);

    public static IReadOnlyList<ResolvedGlobalCommandDefinition> ResolveCommands()
    {
        var catalog = Catalog.Value;
        return catalog.Commands
            .Where(command => command.CommandId != default && command.Utterances.Count > 0)
            .Select(command => new ResolvedGlobalCommandDefinition(
                command.CommandId.Value.Trim(),
                command.Utterances,
                command.Enabled is not false,
                catalog.SupportedLanguages,
                catalog.DefaultLanguage,
                catalog.StrictLocalization))
            .ToArray();
    }

    private static VoiceCommandCatalog LoadCatalog()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(RelativePath);
        var catalog = JsonSerializer.Deserialize<VoiceCommandCatalog>(json)
            ?? throw new InvalidOperationException($"Embedded command catalog '{RelativePath}' could not be deserialized.");
        if (!string.Equals(catalog.CommandModelId, CommandModelId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Embedded command catalog model mismatch for '{RelativePath}'. Expected '{CommandModelId}', got '{catalog.CommandModelId}'.");
        }

        return catalog;
    }
}
