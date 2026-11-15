using System.Collections.Generic;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Führt den Sprachdialog für ein Formular mit Sitzungssprache, Anwendung und
/// wiederhergestellten Variablen aus, damit ein Wiedereintritt den vorherigen Stand erhält.
/// </summary>
public interface IVoiceCommandDialog
{
    /// <summary>
    /// Führt den Sprachbefehlsdialog für ein bestimmtes Formular, damit der Host
    /// nach Abschluss Werte, Abbruch oder Navigation entgegennimmt statt den
    /// Feldablauf selbst zu steuern.
    /// </summary>
    /// <param name="languageCode">ISO-639-1-Sprachcode.</param>
    /// <param name="applicationId">Anwendungs-Id.</param>
    /// <param name="formId">Formular-Id (<c>null</c> bei Einfachformular-Apps oder für den Standard).</param>
    /// <param name="applicationSession">Aktueller Anwendungssitzungsstand (für formularübergreifende geteilte Variablen).</param>
    /// <param name="initialValuesByVariableId">Anfangs-Variablenwerte zum Wiederherstellen (etwa bei Rückkehr in ein Formular).</param>
    /// <param name="cancellationToken">Abbruchtoken.</param>
    Task<VoiceCommandDialogResult> ExecuteAsync(
        string languageCode,
        string applicationId,
        string? formId,
        SpeechAssistantApplicationSessionState applicationSession,
        IReadOnlyDictionary<string, string> initialValuesByVariableId,
        CancellationToken cancellationToken);
}

