using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bündelt Anwendung, ausgewähltes Formular und Modelleinstellungen für die Befehlsbearbeitung.
/// </summary>
public sealed record FormVoiceCommandSelectionContext(
    VoiceDialogSettings VoiceSettings,
    ApplicationDescriptor Application,
    ImmutableHashSet<string> CompletedFormIds,
    FormDescriptor Form,
    FormContract Contract);
