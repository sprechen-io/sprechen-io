namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Zugang zur Browserkonsole, ihrer Aktivierung und der tablokalen Diagnosesitzung.
/// </summary>
public interface IBrowserDebugConsole
{
    ValueTask<bool> GetEnabledAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Prüft die Freigabe für strukturierte Diagnoseberichte. Anders als die
    /// Konsolenausgabe benötigt sie kein ausdrücklich eingeschaltetes Konsolenflag.
    /// Ein ausdrücklich ausgeschaltetes Flag verhindert die Berichte.
    /// </summary>
    ValueTask<bool> GetDiagnosticsAdmittedAsync(CancellationToken cancellationToken);

    ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken);

    /// <summary>
    /// Setzt den speicherinternen Diagnosezustand für den aktuellen Tab.
    /// Im Unterschied zu <see cref="SetEnabledAsync"/> wird kein dauerhaftes Geräteflag geändert.
    /// </summary>
    ValueTask SetDiagnosticSessionAsync(DiagnosticSessionState state, CancellationToken cancellationToken);

    ValueTask WriteAsync(string scope, string message, CancellationToken cancellationToken);
}
