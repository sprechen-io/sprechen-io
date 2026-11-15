using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.VoiceAssistant.WakeRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt Schlüsselbegriffe für die Aktivierungserkennung aus vollständigen
/// Aktivierungsformen, zusätzlichen Erkennungsformen und deren letztem Namensteil.
/// </summary>
/// <remarks>
/// <para>Der einzelne Namensteil ergänzt die vollständige Aktivierungsform,
/// damit auch eine Äußerung ohne Einleitung einen passenden Schlüsselbegriff hat.
/// Die Vokabularbildung priorisiert die Aktivierung, deren Erkennung den Zugang
/// zum weiteren Dialog öffnet.</para>
/// <para><see cref="KeytermComposition.WithoutServerBase"/> hält das allgemeine
/// Servervokabular aus dieser Erkennungsrunde heraus. Damit erhalten Befehle,
/// die im selben Atemzug gesprochen werden, hier keinen zusätzlichen
/// Schlüsselbegriff aus diesem Grundvokabular. Auswahlrunden bauen ihr eigenes
/// Vokabular. Daraus folgt keine allgemeine Genauigkeitsgarantie für den Erkenner.</para>
/// </remarks>
internal sealed class WakeKeytermVocabulary
{
    private readonly SpeechKeytermProjectionPolicy _projectionPolicy = new();

    public SpeechKeyterms? Build(WakeWordInventory wakeWordInventory)
    {
        ArgumentNullException.ThrowIfNull(wakeWordInventory);

        var forms = wakeWordInventory.ActivationForms
            .Concat(wakeWordInventory.RecognitionOnlyActivationForms)
            .Where(form => !string.IsNullOrWhiteSpace(form))
            .Select(form => form.Trim())
            .ToArray();

        if (forms.Length == 0)
        {
            // Ohne Aktivierungsinventar fehlt der optionale Wert. Eine leere Liste mit
            // WithoutServerBase würde dagegen ausdrücklich das Server-Grundvokabular ausschließen.
            return null;
        }

        var terms = new List<string>();
        foreach (var term in forms.Concat(forms.Select(WakeWordNameOf)))
        {
            if (!string.IsNullOrWhiteSpace(term)
                && !terms.Contains(term, StringComparer.OrdinalIgnoreCase))
            {
                terms.Add(term);
            }
        }

        // Die gemeinsame Projektion hält auch überschriebene Aktivierungsformen
        // innerhalb des Service-Budgets. Eine ungeprüfte Liste könnte dessen
        // Zulassungsgrenzen überschreiten und zur Ablehnung der Anfrage führen.
        var projected = _projectionPolicy.Project(terms, KeytermComposition.WithoutServerBase);
        return new SpeechKeyterms(projected, KeytermComposition.WithoutServerBase);
    }

    /// <summary>
    /// Liefert den Namensteil nach dem letzten gewöhnlichen Leerzeichen einer
    /// Aktivierungsform. Eine Form ohne dieses Trennzeichen bleibt vollständig erhalten.
    /// </summary>
    private static string WakeWordNameOf(string activationForm)
    {
        var tokens = activationForm.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length == 0 ? string.Empty : tokens[^1];
    }
}
