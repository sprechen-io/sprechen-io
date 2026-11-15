using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Spricht eine Ansage und erfasst ihre Art, damit etwa Hinweise auf fehlende Eingabe
/// oder technische Fehler später in der Sitzung nachvollzogen werden können.
/// </summary>
/// <remarks>
/// Die getrennte Schnittstelle stellt diese Fähigkeit bereit, ohne alle Verbraucher
/// des allgemeinen Formulardialog-Sprachdiensts davon abhängig zu machen.
/// </remarks>
public interface IDeclaredAnnouncementSpeaker
{
    Task SpeakAnnouncementAsync(
        string text,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        SpokenAnnouncementKind announcementKind,
        CancellationToken cancellationToken);
}
