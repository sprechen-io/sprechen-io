using System.Collections.Generic;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Entkoppelt die Auswahl eines Formulars in einer Anwendung mit mehreren Formularen
/// von der übergeordneten Dialogsteuerung.
/// </summary>
public interface IFormSelectionDialog
{
    /// <summary>
    /// Führt die Formularauswahl unter Berücksichtigung bereits abgeschlossener Formulare aus.
    /// </summary>
    Task<FormSelectionDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string correlationId,
        IReadOnlyCollection<string> completedFormIds,
        CancellationToken cancellationToken);
}

