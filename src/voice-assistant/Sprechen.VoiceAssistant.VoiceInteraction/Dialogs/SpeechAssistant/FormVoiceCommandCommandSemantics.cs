using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt, wann ein geparster Befehl gegenüber Feldinhalt Vorrang hat, und hält die kontextbezogenen Ausnahmen für Moduswechsel ausdrücklich zusammen.
/// </summary>
internal static class FormVoiceCommandCommandSemantics
{
    public static bool ShouldExecuteParsedCommand(
        FormContract contract,
        string isoCode,
        PolitenessLevel politenessLevel,
        bool hasExplicitCommandPrefix,
        bool isLiteralMode,
        string trimmedTranscript,
        string parsedCommandId,
        IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(trimmedTranscript);
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedCommandId);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        if (hasExplicitCommandPrefix)
        {
            return true;
        }

        if (IsAnchorFreeModeSwitchCommand(parsedCommandId, isLiteralMode))
        {
            // Isolierte Moduswechsel dürfen in den vorgesehenen Kontexten ohne
            // Aktivierungswort ausgeführt werden. Eingebettete Wörter erhalten
            // diese Ausnahme nicht.
            return HasClearIsolatedCommandSignal(
                contract,
                trimmedTranscript,
                parsedCommandId,
                isoCode,
                politenessLevel,
                acceptedAnchorForms);
        }

        // Ohne explizites Präfix oder Modusausnahme ein akzeptiertes führendes
        // Aktivierungswort verlangen. Seine Formen liefert der Sitzungskontext.
        return FormGlobalCommandParser.HasCommandLeadIn(trimmedTranscript, acceptedAnchorForms);
    }

    // Außerhalb wörtlicher Erfassung die aufgezählten Moduseinstiege ohne
    // Aktivierungswort zulassen. Innerhalb gilt die Ausnahme nur für den
    // Ausstieg aus der wörtlichen Erfassung.
    private static bool IsAnchorFreeModeSwitchCommand(string commandId, bool isLiteralMode)
    {
        if (commandId is GlobalCommandIds.InputModeLiteralOff)
        {
            return isLiteralMode;
        }

        if (isLiteralMode)
        {
            return false;
        }

        return commandId is GlobalCommandIds.InputModeSpelling
            or GlobalCommandIds.InputModeVoice
            or GlobalCommandIds.InputModeDigits
            or GlobalCommandIds.InputModeKeyboard
            or GlobalCommandIds.InputModeLiteral;
    }

    public static bool ShouldTreatRepeatAsInput(
        FormContract contract,
        string transcript,
        string parsedCommandId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedCommandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        return string.Equals(parsedCommandId, GlobalCommandIds.Repeat, StringComparison.Ordinal)
            && !HasClearRepeatSignal(
                contract,
                transcript,
                parsedCommandId,
                isoCode,
                politenessLevel,
                acceptedAnchorForms);
    }

    public static bool HasClearRepeatSignal(
        FormContract contract,
        string transcript,
        string parsedCommandId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedCommandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        if (!string.Equals(parsedCommandId, GlobalCommandIds.Repeat, StringComparison.Ordinal))
        {
            return false;
        }

        return FormGlobalCommandParser.HasCommandLeadIn(transcript, acceptedAnchorForms)
            || FormGlobalCommandParser.IsIsolatedCommandSignal(
                contract,
                transcript,
                parsedCommandId,
                isoCode,
                politenessLevel);
    }

    // Die Zulässigkeit isolierter Signale hier gesondert bestimmen.
    // Ob der Befehl ausgeführt wird, entscheidet die übergeordnete Befehlsprüfung.
    public static bool HasClearIsolatedCommandSignal(
        FormContract contract,
        string transcript,
        string parsedCommandId,
        string isoCode,
        PolitenessLevel politenessLevel,
        IReadOnlyCollection<string> acceptedAnchorForms)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);
        ArgumentException.ThrowIfNullOrWhiteSpace(parsedCommandId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(acceptedAnchorForms);

        if (!IsIsolationEligibleCommand(parsedCommandId))
        {
            return false;
        }

        return FormGlobalCommandParser.HasCommandLeadIn(transcript, acceptedAnchorForms)
            || FormGlobalCommandParser.IsIsolatedCommandSignal(
                contract,
                transcript,
                parsedCommandId,
                isoCode,
                politenessLevel);
    }

    // Wertannahme, Wertbearbeitung sowie Sprach- und Höflichkeitswechsel
    // von dieser Einstufung als isolierter Befehl ausnehmen.
    private static bool IsIsolationEligibleCommand(string commandId)
        => commandId is not (
            GlobalCommandIds.AcceptValue
            or GlobalCommandIds.EditValue
            or GlobalCommandIds.ChangeLanguage
            or GlobalCommandIds.ChangePoliteness);
}
