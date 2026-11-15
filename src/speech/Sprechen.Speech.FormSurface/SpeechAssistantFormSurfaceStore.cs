using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.FormSurface;

/// <summary>
/// Verwaltet die veröffentlichte Formularansicht und ihre gültige Sitzung,
/// damit verspätete Veröffentlichungen abgelöster Sitzungen die aktuelle Ansicht nicht überschreiben.
/// </summary>
public sealed class SpeechAssistantFormSurfaceStore :
    ISpeechAssistantFormSurfaceState,
    ISpeechAssistantFormSurfaceController
{
    private readonly object _gate = new();
    private SpeechAssistantFormSurfaceView? _current;
    private FormSurfacePublicationSession? _active;
    private long _generation;

    public event Action? Changed;

    public SpeechAssistantFormSurfaceView? Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public FormSurfacePublicationSession BeginSession()
    {
        var notify = false;
        FormSurfacePublicationSession session;
        lock (_gate)
        {
            _generation++;
            _active = new FormSurfacePublicationSession(_generation, Guid.NewGuid());
            if (_current is not null)
            {
                _current = null;
                notify = true;
            }

            session = _active;
        }

        if (notify)
        {
            Changed?.Invoke();
        }

        return session;
    }

    public bool TryPublishActive(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(view);

        if (view.Snapshot.Stage != FormSurfaceStage.Active)
        {
            throw new InvalidOperationException("An active surface publication required an active stage.");
        }

        return TryWrite(session, view);
    }

    public bool TryPublishTerminal(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(view);

        if (view.Snapshot.Stage is not (FormSurfaceStage.Submitted or FormSurfaceStage.Cancelled or FormSurfaceStage.Abandoned))
        {
            throw new InvalidOperationException("A terminal surface publication required a terminal stage.");
        }

        if (view.Snapshot.FieldValuesById.Count > 0)
        {
            throw new InvalidOperationException("A terminal surface publication cannot carry field values.");
        }

        return TryWrite(session, view);
    }

    public void EndSession(FormSurfacePublicationSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        lock (_gate)
        {
            if (_active is not null && session.Generation == _active.Generation)
            {
                _active = null;
            }
        }
    }

    public void Stop()
    {
        var notify = false;
        lock (_gate)
        {
            _generation++;
            _active = null;
            if (_current is null || _current.Snapshot.Stage == FormSurfaceStage.Active)
            {
                if (_current is not null)
                {
                    _current = null;
                    notify = true;
                }
            }
        }

        if (notify)
        {
            Changed?.Invoke();
        }
    }

    private bool TryWrite(FormSurfacePublicationSession session, SpeechAssistantFormSurfaceView view)
    {
        var notify = false;
        lock (_gate)
        {
            if (_active is null
                || session.Generation != _active.Generation
                || session.CorrelationId != _active.CorrelationId)
            {
                return false;
            }

            if (!Equals(_current, view))
            {
                _current = view;
                notify = true;
            }
        }

        if (notify)
        {
            Changed?.Invoke();
        }

        return true;
    }
}
