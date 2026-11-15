using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogHostExamples;

internal sealed class LanguageSelectionFlow : IDialogFlow<WizardEvent>
{
    private readonly IDialogHost<WizardEvent> _host;
    private readonly WizardContext _context;
    private readonly WizardFormsCatalog _catalog;

    public LanguageSelectionFlow(
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
        var languageOptions = string.Join(", ", _context.SupportedLanguages.Select(lang => lang.DisplayName));
        _context.Prompts.Add($"Bitte Sprache wählen: {languageOptions}.");
        return ValueTask.CompletedTask;
    }

    public ValueTask OnDeactivatedAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public async ValueTask DispatchAsync(WizardEvent @event, DispatchMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(@event);

        if (@event is not WizardLanguageSelectedEvent languageSelected)
        {
            return;
        }

        _context.SetSelectedLanguage(languageSelected.LanguageCode);

        var chosen = _context.SupportedLanguages.First(lang => lang.Code.Equals(languageSelected.LanguageCode, StringComparison.OrdinalIgnoreCase));
        _context.Prompts.Add($"Sprache gesetzt: {chosen.DisplayName}.");

        await _host.ReplaceTopAsync(new ApplicationSearchFlow(_host, _context, _catalog), cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        IsDisposed = true;
        return ValueTask.CompletedTask;
    }
}



