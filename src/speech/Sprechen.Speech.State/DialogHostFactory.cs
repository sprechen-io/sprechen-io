namespace Sprechen.Speech.State;

/// <summary>
/// Gibt jedem neuen Dialoghost eine eigene Sitzung und einen eigenen Modulstapel, um die Dialogverwaltung seiner Aufrufer getrennt zu halten.
/// </summary>
public static class DialogHostFactory
{
    public static IDialogHost<TEvent> Create<TEvent>() where TEvent : notnull
    {
        return new DialogHost<TEvent>();
    }
}


