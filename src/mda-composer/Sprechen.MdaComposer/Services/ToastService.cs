namespace Sprechen.MdaComposer.Services;

/// <summary>
/// Verwaltet kurzzeitig sichtbare Meldungen im Composer, um über Ergebnisse und Fehler zu informieren.
/// </summary>
public sealed class ToastService
{
    private readonly List<ToastMessage> _messages = [];
    private int _nextId;

    public IReadOnlyList<ToastMessage> Messages => _messages;
    public event Action? OnChanged;

    public void Success(string text) => Add(text, ToastLevel.Success);
    public void Warning(string text) => Add(text, ToastLevel.Warning);
    public void Error(string text) => Add(text, ToastLevel.Error);
    public void Info(string text) => Add(text, ToastLevel.Info);

    public void Dismiss(int id)
    {
        _messages.RemoveAll(m => m.Id == id);
        OnChanged?.Invoke();
    }

    private void Add(string text, ToastLevel level)
    {
        var msg = new ToastMessage(++_nextId, text, level, DateTimeOffset.UtcNow);
        _messages.Add(msg);

        if (_messages.Count > 5)
        {
            _messages.RemoveAt(0);
        }

        OnChanged?.Invoke();

        _ = AutoDismissAsync(msg.Id, level == ToastLevel.Error ? 8000 : 3500);
    }

    private async Task AutoDismissAsync(int id, int delayMs)
    {
        await Task.Delay(delayMs);
        Dismiss(id);
    }
}

/// <summary>
/// Enthält Text, Kategorie und Identität einer Meldung, damit die Oberfläche sie anzeigen und gezielt entfernen kann.
/// </summary>
public sealed record ToastMessage(int Id, string Text, ToastLevel Level, DateTimeOffset Timestamp);

/// <summary>
/// Unterscheidet Meldungskategorien für die Darstellung und die Dauer ihrer Anzeige.
/// </summary>
public enum ToastLevel
{
    Success,
    Warning,
    Error,
    Info,
}
