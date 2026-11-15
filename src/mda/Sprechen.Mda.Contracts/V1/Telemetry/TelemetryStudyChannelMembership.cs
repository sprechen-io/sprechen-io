namespace Sprechen.Mda.Contracts.V1.Telemetry;

/// <summary>
/// Ordnet Telemetrieereignisse den Studienbedingungen zu, um ihre Erfassung zu vergleichen.
/// A bezeichnet das HTML-Referenzformular, B den Sprachprototyp.
/// </summary>
public enum TelemetryStudyChannelMembership
{
    /// <summary>
    /// Ereignis beider Anwendungen und Teil der zwischen A und B vergleichbaren Erfassung.
    /// </summary>
    BothChannels,

    /// <summary>
    /// Sprachspezifisches Ereignis der Bedingung B ohne HTML-Gegenstück.
    /// </summary>
    VoiceOnly,

    /// <summary>
    /// HTML-spezifisches Ereignis der Bedingung A ohne Gegenstück in der Sprachoberfläche.
    /// </summary>
    HtmlOnly,

    /// <summary>
    /// Manuell beobachteter Übergang in einer der Bedingungen. Die HTML- und Sprachanwendung
    /// leiten dieses Ereignis nicht selbst ab und senden es nicht.
    /// </summary>
    ObserverOnly,

    /// <summary>
    /// Diagnose außerhalb des A/B-Vergleichs, etwa zur Aktivierungswort- oder Spracherkennung.
    /// Der Katalog legt gesondert fest, welche Anwendung solche Diagnosen senden darf.
    /// Diese Zugehörigkeit allein erlaubt das Senden nicht.
    /// </summary>
    NonComparableDiagnostic
}
