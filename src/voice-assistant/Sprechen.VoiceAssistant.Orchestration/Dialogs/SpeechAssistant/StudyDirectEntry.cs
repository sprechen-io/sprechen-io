using Sprechen.Contracts.MdaFormAccess;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet einem angeforderten Studieneinstieg die eindeutig markierte Studienanwendung
/// und ihr erstes geordnetes Formular zu. Fehlende oder mehrdeutige Voraussetzungen
/// führen zu einem Fehler. Die Teilnehmerkennung wird hier nicht authentifiziert.
/// </summary>
internal sealed class StudyDirectEntry
{
    private readonly IFormsCatalogDataClient _formsCatalogDataClient;
    private readonly IOperationalProbeSession _operationalProbeSession;
    private readonly IStudyTelemetryContext _studyTelemetryContext;

    public StudyDirectEntry(
        IFormsCatalogDataClient formsCatalogDataClient,
        IOperationalProbeSession operationalProbeSession,
        IStudyTelemetryContext studyTelemetryContext)
    {
        _formsCatalogDataClient = formsCatalogDataClient ?? throw new ArgumentNullException(nameof(formsCatalogDataClient));
        _operationalProbeSession = operationalProbeSession ?? throw new ArgumentNullException(nameof(operationalProbeSession));
        _studyTelemetryContext = studyTelemetryContext ?? throw new ArgumentNullException(nameof(studyTelemetryContext));
    }

    // Wurde ein technischer Testlauf angefordert, muss ein Testkontext vorliegen und
    // die Teilnehmerkennung leer sein. Andernfalls den Einstieg mit einem Fehler abbrechen.
    internal OperationalProbeContext? CurrentProbeContext()
    {
        var probeContext = _operationalProbeSession.Context;
        if (_operationalProbeSession.IsRequested
            && (probeContext is null || !string.IsNullOrWhiteSpace(_studyTelemetryContext.ParticipantId)))
        {
            throw new OperationalProbeContextException();
        }

        return probeContext;
    }

    public async Task<StudyBindResult?> TryBindAsync(
        bool requiresStudyBinding,
        OperationalProbeContext? probeContext,
        string isoCode,
        CancellationToken cancellationToken)
    {
        if (!requiresStudyBinding)
        {
            return null;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        var catalog = await _formsCatalogDataClient.GetFormsCatalogAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new StudyBindFailedException("The forms catalog could not be loaded for a study link.");

        var studyApplications = catalog.Applications
            .Where(application => application.IsStudyApplication == true)
            .ToArray();
        if (studyApplications.Length != 1)
        {
            throw new StudyBindFailedException(
                "A study link requires exactly one application marked as the study application.");
        }

        var application = studyApplications[0];
        if (application.Forms.Count == 0)
        {
            throw new StudyBindFailedException(
                "The study application does not declare a form.");
        }

        var form = application.Forms
            .OrderBy(item => item.Sequence)
            .ThenBy(item => item.FormId.ToString(), StringComparer.Ordinal)
            .First();
        var displayName = ResolveDisplayName(application, isoCode);
        var result = new StudyBindResult(
            new StudySession(application.ApplicationId, form.FormId.ToString()!),
            displayName);
        if (probeContext is not null)
        {
            _operationalProbeSession.BindStudy(probeContext, result.Session.ApplicationId, result.Session.FormId);
        }

        return result;
    }

    private static string ResolveDisplayName(ApplicationDescriptor application, string isoCode)
    {
        if (application.DisplayName.TryGetValue(isoCode, out var name) && !string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        if (application.DisplayName.TryGetValue(application.DefaultLanguage, out var fallback)
            && !string.IsNullOrWhiteSpace(fallback))
        {
            return fallback;
        }

        throw new StudyBindFailedException(
            "The study application does not carry a display name for the session language.");
    }
}
