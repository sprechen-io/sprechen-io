namespace Sprechen.Speech.State;

/// <summary>
/// Hält die Reihenfolge der Dialogmodule, um das aktive und bei einer Rückkehr
/// fortzusetzende Modul aus demselben Stapel zu bestimmen. Die Stapeltiefe wird
/// nach Änderungen an der Anzahl veröffentlicht.
/// </summary>
internal sealed class DialogHostStack<TEvent> where TEvent : notnull
{
    private readonly List<IDialogFlow<TEvent>> _modules = new();

    private int _depth;

    internal int Depth => Volatile.Read(ref _depth);

    internal bool IsEmpty => _modules.Count == 0;

    internal void Add(IDialogFlow<TEvent> module)
    {
        ArgumentNullException.ThrowIfNull(module);

        _modules.Add(module);
        PublishDepth();
    }

    internal IDialogFlow<TEvent> ActiveOrThrow()
    {
        if (_modules.Count == 0)
        {
            throw new InvalidOperationException("Dialog host has not been started.");
        }

        return _modules[^1];
    }

    internal IDialogFlow<TEvent>? ActiveOrDefault()
    {
        return _modules.Count == 0 ? null : _modules[^1];
    }

    internal IDialogFlow<TEvent>? PreviousActiveOrDefault()
    {
        return _modules.Count > 1 ? _modules[^2] : null;
    }

    internal IDialogFlow<TEvent> RemoveTop()
    {
        var module = ActiveOrThrow();
        _modules.RemoveAt(_modules.Count - 1);
        PublishDepth();
        return module;
    }

    internal void ReplaceTop(IDialogFlow<TEvent> module)
    {
        ArgumentNullException.ThrowIfNull(module);
        if (_modules.Count == 0)
        {
            throw new InvalidOperationException("Dialog host has not been started.");
        }

        _modules[^1] = module;
    }

    internal IDialogFlow<TEvent>[] Snapshot()
    {
        return _modules.ToArray();
    }

    internal bool ContainsReference(IDialogFlow<TEvent> module)
    {
        ArgumentNullException.ThrowIfNull(module);

        foreach (var current in _modules)
        {
            if (ReferenceEquals(current, module))
            {
                return true;
            }
        }

        return false;
    }

    internal void Clear()
    {
        _modules.Clear();
        PublishDepth();
    }

    private void PublishDepth() => Volatile.Write(ref _depth, _modules.Count);
}
