using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

/// <summary>
/// Bietet eine gemeinsame Schnittstelle für Formularwerte, Navigationsübersicht, Status, Zusammenfassung und Validierung.
/// </summary>
internal interface ICqrsFormStatusExecutor
{
    Task<ImmutableDictionary<string, string>> GetValuesSnapshotAsync();

    Task<FormDiscoverySnapshot> GetDiscoverySnapshotAsync();

    Task<FormStatusSnapshot> GetStatusSnapshotAsync();

    Task<FormDetailedStatusSnapshot> GetDetailedStatusSnapshotAsync();

    Task<ImmutableArray<FormSummaryItem>> BuildSummaryItemsAsync();

    Task<ImmutableArray<FormConstraintViolation>> ValidateAllAsync();
}
