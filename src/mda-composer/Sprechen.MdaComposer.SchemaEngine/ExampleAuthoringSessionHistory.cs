namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Speichert begrenzte Dokumentverläufe zum Rückgängigmachen und Wiederholen von Änderungen.
/// </summary>
internal sealed class ExampleAuthoringSessionHistory
{
    private const int MaxHistorySize = 50;

    private readonly List<string> _undoStack = [];
    private readonly List<string> _redoStack = [];

    public bool CanUndo => _undoStack.Count > 0;

    public bool CanRedo => _redoStack.Count > 0;

    public void Reset()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public void RecordReplacement(string beforeSnapshot, string afterSnapshot)
    {
        if (!string.Equals(afterSnapshot, beforeSnapshot, StringComparison.Ordinal))
        {
            AddToStack(_undoStack, beforeSnapshot);
        }

        _redoStack.Clear();
    }

    public bool TryRecordMutation(string beforeSnapshot, string afterSnapshot)
    {
        if (string.Equals(afterSnapshot, beforeSnapshot, StringComparison.Ordinal))
        {
            return false;
        }

        AddToStack(_undoStack, beforeSnapshot);
        _redoStack.Clear();
        return true;
    }

    public bool TryUndo(string currentSnapshot, out string previousSnapshot)
    {
        if (_undoStack.Count == 0)
        {
            previousSnapshot = string.Empty;
            return false;
        }

        AddToStack(_redoStack, currentSnapshot);
        previousSnapshot = Pop(_undoStack);
        return true;
    }

    public bool TryRedo(string currentSnapshot, out string nextSnapshot)
    {
        if (_redoStack.Count == 0)
        {
            nextSnapshot = string.Empty;
            return false;
        }

        AddToStack(_undoStack, currentSnapshot);
        nextSnapshot = Pop(_redoStack);
        return true;
    }

    private static string Pop(List<string> stack)
    {
        var snapshot = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return snapshot;
    }

    private static void AddToStack(List<string> stack, string snapshot)
    {
        stack.Add(snapshot);
        if (stack.Count > MaxHistorySize)
        {
            stack.RemoveAt(0);
        }
    }
}
