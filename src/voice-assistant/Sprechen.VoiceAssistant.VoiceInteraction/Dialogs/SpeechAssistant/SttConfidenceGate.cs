namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet eine gemeldete Erkennungssicherheit anhand der begrenzten Vertrags- oder
/// Standardgrenzen ein, damit Aufrufer Einstufung und angewandte Bestätigungsgrenze gemeinsam erhalten.
/// </summary>
internal static class SttConfidenceGate
{
    /// <summary>
    /// Liefert die Vertrauenseinstufung für den übergebenen Sicherheitswert.
    /// </summary>
    /// <param name="relaxBelowConfirmFloor">
    /// Wandelt bei gesetztem Merkmal den unterhalb beider geprüfter Grenzen verbleibenden
    /// Bereich in unsicher statt unzureichend um. Die Berechtigung dieser Abschwächung
    /// entscheidet der Aufrufer. Diese Methode prüft keine Wiedergabe oder Werteingabe.
    /// </param>
    public static SttConfidenceLevel Evaluate(
        FormVoiceCommandContext context,
        int? sttConfidencePercent,
        bool relaxBelowConfirmFloor = false)
        => Assess(context, sttConfidencePercent, relaxBelowConfirmFloor).Level;

    /// <summary>
    /// Liefert die Einstufung zusammen mit der angewandten Bestätigungsgrenze.
    /// Die Annahmegrenze wird zuerst geprüft. Fehlende Sicherheitswerte gelten als unsicher.
    /// </summary>
    public static SttConfidenceEvaluation Assess(
        FormVoiceCommandContext context,
        int? sttConfidencePercent,
        bool relaxBelowConfirmFloor = false)
    {
        ArgumentNullException.ThrowIfNull(context);

        var profile = context.Contract.Projections?.Voice?.FormVoiceProfile;
        var minAccept = Math.Clamp(
            profile?.SttMinConfidenceToAccept ?? CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault, 0, 100);
        var minConfirm = Math.Clamp(
            profile?.SttMinConfidenceToConfirm ?? CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault, 0, 100);

        if (!sttConfidencePercent.HasValue)
        {
            return new SttConfidenceEvaluation(SttConfidenceLevel.Uncertain, minConfirm);
        }

        var confidencePercent = Math.Clamp(sttConfidencePercent.Value, 0, 100);

        if (confidencePercent >= minAccept)
        {
            return new SttConfidenceEvaluation(SttConfidenceLevel.Sufficient, minConfirm);
        }

        if (confidencePercent >= minConfirm)
        {
            return new SttConfidenceEvaluation(SttConfidenceLevel.Uncertain, minConfirm);
        }

        return new SttConfidenceEvaluation(
            relaxBelowConfirmFloor ? SttConfidenceLevel.Uncertain : SttConfidenceLevel.Insufficient,
            minConfirm);
    }
}
