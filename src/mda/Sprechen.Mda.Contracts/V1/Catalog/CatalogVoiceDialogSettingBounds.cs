namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Zahlengrenzen und Standardwerte für Sprachdialoge bei der Auswahl.
/// Schemaabgleich und Vorprüfung verwenden dieselben Werte.
/// </summary>
public static class CatalogVoiceDialogSettingBounds
{
    /// <summary>Kleinste Wartezeit ohne Eingabe in Millisekunden.</summary>
    public const int NoInputTimeoutMsMin = 1000;

    /// <summary>Größte Wartezeit ohne Eingabe in Millisekunden.</summary>
    public const int NoInputTimeoutMsMax = 60000;

    /// <summary>Kleinste zulässige Zahl der Wiederholungen.</summary>
    public const int MaxRetriesMin = 0;

    /// <summary>Größte zulässige Zahl der Wiederholungen.</summary>
    public const int MaxRetriesMax = 10;

    /// <summary>Kleinster STT-Konfidenzwert in Prozent für die Schwellen des Auswahldialogs.</summary>
    public const int SttConfidencePercentMin = 0;

    /// <summary>Größter STT-Konfidenzwert in Prozent für die Schwellen des Auswahldialogs.</summary>
    public const int SttConfidencePercentMax = 100;

    /// <summary>Standard-Konfidenzschwelle zur Annahme einer erkannten Auswahl.</summary>
    public const int SttMinConfidenceToAcceptDefault = 85;

    /// <summary>Untere Standard-Konfidenzschwelle für eine Rückfrage zur Bestätigung.</summary>
    public const int SttMinConfidenceToConfirmDefault = 65;

    /// <summary>Standardwartezeit ohne Eingabe in Millisekunden.</summary>
    public const int NoInputTimeoutMsDefault = 10000;

    /// <summary>Standardzahl der Wiederholungen.</summary>
    public const int MaxRetriesDefault = 3;

    /// <summary>Standardregel für Bestätigungen.</summary>
    public const ConfirmationPolicy ConfirmationPolicyDefault = ConfirmationPolicy.Always;

    /// <summary>Standardfaktor der Sprechgeschwindigkeit.</summary>
    public const double SpeechRateDefault = 1.0;

    /// <summary>Standardpause zwischen angesagten Einträgen in Millisekunden.</summary>
    public const int PauseBetweenItemsMsDefault = 500;

    /// <summary>Standardsperrzeit für wiederholte Abbruchbefehle in Millisekunden.</summary>
    public const int CancelRefractoryMsDefault = 2000;

    /// <summary>Standardeinstellung für das Unterbrechen einer Sprachausgabe.</summary>
    public const bool AllowBargeInDefault = false;

    /// <summary>Standardeinstellung für das automatische Weitergehen nach der Übernahme.</summary>
    public const bool AutoAdvanceAfterCommitDefault = true;

    /// <summary>Standardhöflichkeitsstufe der Ansprache.</summary>
    public const PolitenessLevel PolitenessLevelDefault = PolitenessLevel.Formal;

    /// <summary>Standardgrundlage der Fortschrittsberechnung.</summary>
    public const ProgressCalculationBasis ProgressCalculationBasisDefault = ProgressCalculationBasis.AllFields;

    /// <summary>Kleinster Faktor der Sprechgeschwindigkeit.</summary>
    public const double SpeechRateMin = 0.5;

    /// <summary>Größter Faktor der Sprechgeschwindigkeit.</summary>
    public const double SpeechRateMax = 2.0;

    /// <summary>Kleinste Pause zwischen angesagten Einträgen in Millisekunden.</summary>
    public const int PauseBetweenItemsMsMin = 0;

    /// <summary>Größte Pause zwischen angesagten Einträgen in Millisekunden.</summary>
    public const int PauseBetweenItemsMsMax = 5000;

    /// <summary>Kleinste Sperrzeit für wiederholte Abbruchbefehle in Millisekunden.</summary>
    public const int CancelRefractoryMsMin = 0;

    /// <summary>Größte Sperrzeit für wiederholte Abbruchbefehle in Millisekunden.</summary>
    public const int CancelRefractoryMsMax = 10000;
}
