using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Modell, Fokus und Orientierungsdaten für Verbraucher bereit, die den aktuellen Formulardialog abfragen.
/// </summary>
public interface IFormDialogRuntimeState
{
    FormContract Contract { get; }

    PolitenessLevel PolitenessLevel { get; }

    Task<FormDiscoverySnapshot> GetDiscoverySnapshotAsync();

    Task<FormFieldSnapshot> GetFocusedFieldAsync();

    Task<FormStatusSnapshot> GetStatusSnapshotAsync();

    Task<ImmutableArray<FormSummaryItem>> BuildSummaryItemsAsync();
}
