using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.ModelRuntime.PromptTemplates;

/// <summary>
/// Verbindet Einstiegsfragen mit der jeweils angebotenen Sprache, sodass Nutzer die Sprachwahl in einer ihnen verständlichen Sprache hören können.
/// </summary>
/// <remarks>
/// Jede Frage bildet ein Segment in der Sprache, die sie anbietet. Der aufgelöste
/// Text wird zusammen mit diesem Sprachcode zurückgegeben. Fehlt eine über den
/// Katalog auflösbare Vorlage, wird der Aufruf abgewiesen, damit eine Sprachoption
/// nicht wegen fehlenden Texts stillschweigend entfällt.
/// </remarks>
public static class SessionLanguageOfferPromptTextResolver
{
    public static SpokenSegment GetKeepQuestionSegment(string isoCode, PolitenessLevel politenessLevel)
        => new(ResolveTemplate(FormDialogPromptIds.SessionEntryKeepQuestion, isoCode, politenessLevel), isoCode);

    public static SpokenSegment GetLanguageQuestionSegment(string isoCode, PolitenessLevel politenessLevel)
        => new(ResolveTemplate(FormDialogPromptIds.SessionEntryLanguageQuestion, isoCode, politenessLevel), isoCode);

    public static SpokenSegment GetAcceptedSegment(string isoCode, PolitenessLevel politenessLevel)
        => new(ResolveTemplate(FormDialogPromptIds.SessionEntryAccepted, isoCode, politenessLevel), isoCode);

    public static SpokenSegment GetCancelledSegment(string isoCode, PolitenessLevel politenessLevel)
        => new(ResolveTemplate(FormDialogPromptIds.SessionEntryCancelled, isoCode, politenessLevel), isoCode);

    private static string ResolveTemplate(string promptId, string isoCode, PolitenessLevel politenessLevel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (!VoiceInterfaceModelCatalog.Resources.TryGetPromptTemplate(
                promptId,
                isoCode,
                politenessLevel,
                out var template))
        {
            // Eine fehlende Vorlage darf die betreffende Sprachoption nicht stillschweigend
            // aus der Aushandlung entfernen. Deshalb wird der Aufruf mit einem Fehler beendet.
            throw new InvalidOperationException(
                $"Prompt template '{promptId}' is missing for '{isoCode}'.");
        }

        return template;
    }
}
