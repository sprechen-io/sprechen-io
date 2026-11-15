using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed class FormFillFlow : IDialogFlow<WizardEvent>
{
    private readonly WizardContext _context;
    private readonly WizardApplicationDefinition _application;

    private int _focusIndex;

    public FormFillFlow(
        WizardContext context,
        WizardApplicationDefinition application)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public bool IsDisposed { get; private set; }

    public ValueTask OnActivatedAsync(CancellationToken cancellationToken)
    {
        _focusIndex = 0;
        PromptCurrentField();
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask DispatchAsync(WizardEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is WizardUserTextEvent userTextEvent)
        {
            var focusField = GetFocusField();
            var value = userTextEvent.Text?.Trim() ?? string.Empty;
            if (value.Length == 0)
            {
                return ValueTask.CompletedTask;
            }

            _context.SetFieldValue(focusField, value);
            _context.Prompts.Add(WizardLocalization.Pick(_context.SelectedLanguageCode, $"Übernommen: {value}.", $"Captured: {value}."));
            return ValueTask.CompletedTask;
        }

        if (@event is WizardNavigateNextEvent)
        {
            _focusIndex++;
            if (_focusIndex >= _application.Fields.Count)
            {
                _context.FocusField = null;
                _context.Prompts.Add(WizardLocalization.Pick(_context.SelectedLanguageCode, "Fertig.", "Done."));
                return ValueTask.CompletedTask;
            }

            PromptCurrentField();
        }

        return ValueTask.CompletedTask;
    }

    private void PromptCurrentField()
    {
        var field = GetFocusField();
        _context.FocusField = field;

        var language = _context.SelectedLanguageCode ?? "en";
        var definition = _application.Fields[_focusIndex];
        var label = definition.Labels.TryGetValue(language, out var localized) ? localized : definition.FieldId.ToString();
        var prompt = WizardLocalization.Pick(language, $"{label}: Bitte eingeben.", $"{label}: Please provide a value.");
        _context.Prompts.Add(prompt);
    }

    private WizardFieldId GetFocusField()
    {
        if (_focusIndex < 0 || _focusIndex >= _application.Fields.Count)
        {
            throw new InvalidOperationException("Form flow focus index is out of range.");
        }

        return _application.Fields[_focusIndex].FieldId;
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}

