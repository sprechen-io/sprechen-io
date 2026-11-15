namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Ermittelt Dialogeinstellungen mit ausdrücklich festgelegten Standardwerten.
/// </summary>
public static class VoiceDialogSettingsExtensions
{
    /// <summary>
    /// Liefert die Wartezeit ohne Eingabe in Millisekunden oder den angegebenen Standardwert.
    /// </summary>
    public static int GetNoInputTimeoutMs(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.NoInputTimeoutMsDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.NoInputTimeoutMs ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Höchstzahl der Wiederholungen oder den angegebenen Standardwert.
    /// </summary>
    public static int GetMaxRetries(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.MaxRetriesDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.MaxRetries ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Bestätigungsregel oder den angegebenen Standardwert.
    /// </summary>
    public static ConfirmationPolicy GetConfirmationPolicy(
        this VoiceDialogSettings settings,
        ConfirmationPolicy defaultValue = CatalogVoiceDialogSettingBounds.ConfirmationPolicyDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.ConfirmationPolicy ?? defaultValue;
    }

    /// <summary>
    /// Liefert den Faktor der Sprechgeschwindigkeit oder den angegebenen Standardwert.
    /// </summary>
    public static double GetSpeechRate(
        this VoiceDialogSettings settings,
        double defaultValue = CatalogVoiceDialogSettingBounds.SpeechRateDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.SpeechRate ?? defaultValue;
    }

    /// <summary>
    /// Liefert die STT-Annahmeschwelle in Prozent oder den angegebenen Standardwert.
    /// </summary>
    public static int GetSttMinConfidenceToAccept(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.SttMinConfidenceToAcceptDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.SttMinConfidenceToAccept ?? defaultValue;
    }

    /// <summary>
    /// Liefert die STT-Schwelle für Rückfragen in Prozent oder den angegebenen Standardwert.
    /// </summary>
    public static int GetSttMinConfidenceToConfirm(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.SttMinConfidenceToConfirmDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.SttMinConfidenceToConfirm ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Pause zwischen Einträgen in Millisekunden oder den angegebenen Standardwert.
    /// </summary>
    public static int GetPauseBetweenItemsMs(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.PauseBetweenItemsMsDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.PauseBetweenItemsMs ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Sperrzeit nach einem Abbruch in Millisekunden oder den angegebenen Standardwert.
    /// </summary>
    public static int GetCancelRefractoryMs(
        this VoiceDialogSettings settings,
        int defaultValue = CatalogVoiceDialogSettingBounds.CancelRefractoryMsDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.CancelRefractoryMs ?? defaultValue;
    }

    /// <summary>
    /// Liefert die wirksame Sperrzeit nach einem Abbruch als TimeSpan.
    /// </summary>
    public static TimeSpan GetCancelRefractoryDuration(this VoiceDialogSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return TimeSpan.FromMilliseconds(settings.GetCancelRefractoryMs());
    }

    /// <summary>
    /// Liefert, ob Sprachausgaben unterbrochen werden dürfen. Ohne Angabe gilt false.
    /// </summary>
    public static bool GetAllowBargeIn(this VoiceDialogSettings settings)
        => GetAllowBargeIn(settings, defaultValue: CatalogVoiceDialogSettingBounds.AllowBargeInDefault);

    /// <summary>
    /// Liefert, ob Sprachausgaben unterbrochen werden dürfen, oder den angegebenen Standardwert.
    /// </summary>
    public static bool GetAllowBargeIn(this VoiceDialogSettings settings, bool defaultValue)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.AllowBargeIn ?? defaultValue;
    }

    /// <summary>
    /// Liefert, ob nach der Wertübernahme automatisch weitergegangen wird. Ohne Angabe gilt true.
    /// </summary>
    public static bool GetAutoAdvanceAfterCommit(this VoiceDialogSettings settings)
        => GetAutoAdvanceAfterCommit(settings, defaultValue: CatalogVoiceDialogSettingBounds.AutoAdvanceAfterCommitDefault);

    /// <summary>
    /// Liefert, ob nach der Wertübernahme automatisch weitergegangen wird, oder den angegebenen Standardwert.
    /// </summary>
    public static bool GetAutoAdvanceAfterCommit(this VoiceDialogSettings settings, bool defaultValue)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.AutoAdvanceAfterCommit ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Höflichkeitsstufe oder den angegebenen Standardwert.
    /// </summary>
    public static PolitenessLevel GetPolitenessLevel(
        this VoiceDialogSettings settings,
        PolitenessLevel defaultValue = CatalogVoiceDialogSettingBounds.PolitenessLevelDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.PolitenessLevel ?? defaultValue;
    }

    /// <summary>
    /// Liefert die Berechnungsgrundlage für Fortschritt oder den angegebenen Standardwert.
    /// </summary>
    public static ProgressCalculationBasis GetProgressCalculationBasis(
        this VoiceDialogSettings settings,
        ProgressCalculationBasis defaultValue = CatalogVoiceDialogSettingBounds.ProgressCalculationBasisDefault)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return settings.ProgressCalculationBasis ?? defaultValue;
    }

    /// <summary>
    /// Ergänzt nicht gesetzte Einstellungen aus fallback. Fehlt auch dort ein Wert, bleibt er null.
    /// </summary>
    public static VoiceDialogSettings MergeWith(this VoiceDialogSettings settings, VoiceDialogSettings? fallback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (fallback is null)
        {
            return settings;
        }

        return new VoiceDialogSettings
        {
            NoInputTimeoutMs = settings.NoInputTimeoutMs ?? fallback.NoInputTimeoutMs,
            MaxRetries = settings.MaxRetries ?? fallback.MaxRetries,
            ConfirmationPolicy = settings.ConfirmationPolicy ?? fallback.ConfirmationPolicy,
            SttMinConfidenceToAccept = settings.SttMinConfidenceToAccept ?? fallback.SttMinConfidenceToAccept,
            SttMinConfidenceToConfirm = settings.SttMinConfidenceToConfirm ?? fallback.SttMinConfidenceToConfirm,
            SpeechRate = settings.SpeechRate ?? fallback.SpeechRate,
            PauseBetweenItemsMs = settings.PauseBetweenItemsMs ?? fallback.PauseBetweenItemsMs,
            CancelRefractoryMs = settings.CancelRefractoryMs ?? fallback.CancelRefractoryMs,
            AllowBargeIn = settings.AllowBargeIn ?? fallback.AllowBargeIn,
            AutoAdvanceAfterCommit = settings.AutoAdvanceAfterCommit ?? fallback.AutoAdvanceAfterCommit,
            PolitenessLevel = settings.PolitenessLevel ?? fallback.PolitenessLevel,
            ProgressCalculationBasis = settings.ProgressCalculationBasis ?? fallback.ProgressCalculationBasis
        };
    }
}
