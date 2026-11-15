using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed class ApplicationSearchFlow : IDialogFlow<WizardEvent>
{
    private readonly IDialogHost<WizardEvent> _host;
    private readonly WizardContext _context;
    private readonly WizardFormsCatalog _catalog;

    private HashSet<string> _lastResultIds = new(StringComparer.OrdinalIgnoreCase);

    public ApplicationSearchFlow(
        IDialogHost<WizardEvent> host,
        WizardContext context,
        WizardFormsCatalog catalog)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    public bool IsDisposed { get; private set; }

    public ValueTask OnActivatedAsync(CancellationToken cancellationToken)
    {
        var prompt = WizardLocalization.Pick(
            _context.SelectedLanguageCode,
            german: "Welche Anwendung möchten Sie öffnen? Bitte sagen Sie den Namen.",
            english: "Which application do you want to open? Please say its name.");

        _context.Prompts.Add(prompt);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public async ValueTask DispatchAsync(WizardEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is WizardApplicationSearchRequestedEvent searchRequested)
        {
            var language = _context.SelectedLanguageCode ?? "en";
            var results = _catalog.Search(language, searchRequested.Query);
            _lastResultIds = results.Select(result => result.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (results.Count == 0)
            {
                _context.Prompts.Add(WizardLocalization.Pick(language, "Keine Anwendung gefunden.", "No application found."));
                return;
            }

            var foundList = string.Join(", ", results.Select(result => result.DisplayName));
            _context.Prompts.Add(WizardLocalization.Pick(language, $"Gefunden: {foundList}.", $"Found: {foundList}."));
            return;
        }

        if (@event is WizardApplicationSelectedEvent selectedEvent)
        {
            if (_lastResultIds.Count == 0)
            {
                _context.Prompts.Add(WizardLocalization.Pick(_context.SelectedLanguageCode, "Bitte zuerst suchen.", "Please search first."));
                return;
            }

            if (!_lastResultIds.Contains(selectedEvent.ApplicationId))
            {
                _context.Prompts.Add(WizardLocalization.Pick(_context.SelectedLanguageCode, "Diese Anwendung wurde nicht gefunden.", "That application was not found."));
                return;
            }

            var app = _catalog.GetById(selectedEvent.ApplicationId);
            _context.SetSelectedApplication(app.Id);

            var appName = app.Names.TryGetValue(_context.SelectedLanguageCode ?? "en", out var localized) ? localized : app.Id;
            _context.Prompts.Add(WizardLocalization.Pick(_context.SelectedLanguageCode, $"Anwendung geladen: {appName}.", $"Application loaded: {appName}."));

            await _host.ReplaceTopAsync(new FormFillFlow(_context, app), cancellationToken);
        }
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}


