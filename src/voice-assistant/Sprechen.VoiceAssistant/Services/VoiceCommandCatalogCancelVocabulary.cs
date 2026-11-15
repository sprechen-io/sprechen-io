using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Services;

/// <summary>
/// Liefert Abbruchäußerungen für den Sitzungseinstieg aus dem modellierten
/// Sprachbefehlskatalog, damit dort kein eigener Wortbestand gepflegt werden muss.
/// </summary>
public sealed class VoiceCommandCatalogCancelVocabulary : ISessionEntryCancelVocabulary
{
    /// <summary>
    /// Abbruchbefehle einmal aus dem Standardkatalog auflösen und für weitere Abfragen halten.
    /// </summary>
    private static readonly IReadOnlyList<ResolvedGlobalCommandDefinition> CancelCommands =
        [.. FormVoiceCommandCatalogResolver
            .ResolveDefaultCommands()
            .Where(command => string.Equals(command.CommandId, GlobalCommandIds.Cancel, StringComparison.Ordinal))];

    /// <inheritdoc />
    public IReadOnlyList<string> GetCancelUtterances(string isoCode, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        return FormGlobalCommandUtteranceCatalog.GetUtterances(CancelCommands, isoCode, politenessLevel);
    }
}
