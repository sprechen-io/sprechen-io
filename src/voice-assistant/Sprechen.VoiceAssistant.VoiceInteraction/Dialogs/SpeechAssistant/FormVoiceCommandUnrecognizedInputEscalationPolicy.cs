namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Wählt feldbezogen zunehmend ausführliche Rückmeldungen nach fehlgeschlagener Eingabe.
/// Transkripte werden nur bei erlaubtem Vorlesen und ohne verworfene Konfidenz oder
/// fehlende Aufnahme zitiert. Geschlossene Optionsfelder erhalten bei den ersten beiden
/// primären Eingabeversuchen Hinweise auf ihre Optionen. Bekanntes, hier unzulässiges
/// Vokabular kann davon abweichend genannt werden. Unmittelbar derselbe Ausdruck
/// wird nicht erneut genannt.
/// Die allgemeine zweite Rückmeldung kann ein Angebot für einen anderen Eingabemodus
/// enthalten. Ab dem dritten Versuch wird Hilfe angekündigt oder zum Hilfebefehl
/// eingeladen. Der Aufrufer muss angekündigte Hilfe anschließend tatsächlich auslösen.
/// Ein Feldwechsel setzt den Zähler zurück. Für weitere Rücksetzungen dient Reset.
/// </summary>
internal sealed class FormVoiceCommandUnrecognizedInputEscalationPolicy
{
    private string? _fieldId;
    private int _attemptCount;
    private string? _namedToken;

    public void Reset()
    {
        _fieldId = null;
        _attemptCount = 0;
        _namedToken = null;
    }

    public string SelectUnrecognizedInputPrompt(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        string? recognizedTranscript,
        FormRepairReason repairReason,
        bool offerConstructiveFallback,
        bool isPrimaryFieldValueEntry,
        bool isAutoHelpDue)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentException.ThrowIfNullOrWhiteSpace(field.FieldId);

        if (!string.Equals(_fieldId, field.FieldId, StringComparison.Ordinal))
        {
            _fieldId = field.FieldId;
            _attemptCount = 0;
            _namedToken = null;
        }

        _attemptCount++;

        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        // Ein Transkript nur bei erlaubtem Vorlesen zitieren. Bei verworfener Konfidenz
        // oder fehlender Aufnahme fehlt eine für diese Rückmeldung zugelassene Hypothese.
        var quotable = field.SpeakValueInConfirmation
            && repairReason is not (FormRepairReason.ConfidenceRejected or FormRepairReason.NothingCaptured);
        var trimmedTranscript = quotable
            ? (recognizedTranscript ?? string.Empty).Trim()
            : string.Empty;

        // Primäre Eingabeversuche an geschlossenen Optionsfeldern erhalten zunächst
        // Optionshinweise. Bestätigungs- und Befehlsfehler verwenden andere Rückmeldungen.

        // Bekanntes, hier unzulässiges Vokabular einmal nennen, solange sich der zuletzt
        // genannte Ausdruck nicht ändert. Die Einordnung stammt vom Aufrufer.
        if (isPrimaryFieldValueEntry
            && repairReason == FormRepairReason.KnownVocabularyInvalidHere
            && trimmedTranscript.Length > 0
            && !string.Equals(_namedToken, trimmedTranscript, StringComparison.OrdinalIgnoreCase))
        {
            _namedToken = trimmedTranscript;
            return FormInputFeedbackPromptBuilder.GetUnrecognizedInputSecondPrompt(
                context.Contract,
                context.IsoCode,
                trimmedTranscript,
                politenessLevel);
        }

        if (isPrimaryFieldValueEntry
            && _attemptCount <= 2
            && FormConstrainedOptionRecoveryPromptBuilder.TryBuildPrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel,
                field,
                out var constrainedOptionRecovery))
        {
            return constrainedOptionRecovery;
        }

        return _attemptCount switch
        {
            // Beim ersten Versuch ein zulässiges, nicht leeres Transkript nennen.
            // Sonst die Rückfrage ohne Zitat verwenden, damit keine leeren Anführungszeichen entstehen.
            1 when trimmedTranscript.Length > 0 => FormInputFeedbackPromptBuilder.GetUnrecognizedInputWithTranscriptPrompt(
                context.Contract,
                context.IsoCode,
                trimmedTranscript,
                politenessLevel),
            1 => FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel),
            // Beim zweiten allgemeinen Versuch nur vorhandenen Text zitieren und, falls
            // angefordert, ein Angebot für einen anderen Eingabemodus ergänzen.
            2 when trimmedTranscript.Length > 0 => AppendConstructiveFallbackOffer(
                context,
                field,
                politenessLevel,
                offerConstructiveFallback,
                FormInputFeedbackPromptBuilder.GetUnrecognizedInputSecondPrompt(
                    context.Contract,
                    context.IsoCode,
                    trimmedTranscript,
                    politenessLevel)),
            2 => AppendConstructiveFallbackOffer(
                context,
                field,
                politenessLevel,
                offerConstructiveFallback,
                FormInputFeedbackPromptBuilder.GetUnrecognizedInputPrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel)),
            _ => SelectThirdRungPrompt(context, politenessLevel, isAutoHelpDue),
        };
    }

    // Hilfe nur ankündigen, wenn der Aufrufer sie anschließend auslöst.
    // Sonst zum Hilfebefehl mit Aktivierungswort einladen.
    private static string SelectThirdRungPrompt(
        FormVoiceCommandContext context,
        PolitenessLevel politenessLevel,
        bool isAutoHelpDue)
        => isAutoHelpDue
            ? FormInputFeedbackPromptBuilder.GetUnrecognizedInputThirdBeforeHelpPrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel)
            : FormInputFeedbackPromptBuilder.GetUnrecognizedInputThirdPrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel);

    private static string AppendConstructiveFallbackOffer(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        PolitenessLevel politenessLevel,
        bool offerConstructiveFallback,
        string rungPrompt)
    {
        if (!offerConstructiveFallback)
        {
            return rungPrompt;
        }

        var offer = FormFieldConstructiveFallbackOfferBuilder.TryBuildOffer(
            context.Contract,
            context.IsoCode,
            politenessLevel,
            field);

        return string.IsNullOrWhiteSpace(offer)
            ? rungPrompt
            : $"{rungPrompt.Trim()} {offer.Trim()}";
    }
}
