namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Verteilt Katalogeingaben auf Suche, Beschreibung, Sprachwechsel und gemeinsame Auswahlbefehle.
/// Ein noch fehlendes Sprachziel wird dabei im Kontext der vorherigen Rückfrage behandelt.
/// </summary>
internal sealed class FormsCatalogDialogIntentRouter
{
    private readonly IFormsCatalogIntentParser _intentParser;
    private readonly Func<FormsCatalogDialogTurnContext, string?, CancellationToken, Task<FormsCatalogIntentHandlingResult>> _handleLanguageChangeAsync;
    private readonly FormsCatalogSearchIntentRoute _searchRoute;
    private readonly FormsCatalogDescribeIntentRoute _describeRoute;
    private readonly FormsCatalogCommonIntentRoute _commonRoute;

    public FormsCatalogDialogIntentRouter(
        IFormsCatalogIntentParser intentParser,
        Func<FormsCatalogDialogTurnContext, string?, CancellationToken, Task<FormsCatalogIntentHandlingResult>> handleLanguageChangeAsync)
        : this(
            intentParser,
            handleLanguageChangeAsync,
            new FormsCatalogSearchIntentRoute(new ApplicationMatcher()),
            new FormsCatalogDescribeIntentRoute(new ApplicationMatcher()),
            new FormsCatalogCommonIntentRoute())
    {
    }

    public FormsCatalogDialogIntentRouter(
        IFormsCatalogIntentParser intentParser,
        Func<FormsCatalogDialogTurnContext, string?, CancellationToken, Task<FormsCatalogIntentHandlingResult>> handleLanguageChangeAsync,
        FormsCatalogSearchIntentRoute searchRoute,
        FormsCatalogDescribeIntentRoute describeRoute,
        FormsCatalogCommonIntentRoute commonRoute)
    {
        _intentParser = intentParser ?? throw new ArgumentNullException(nameof(intentParser));
        _handleLanguageChangeAsync = handleLanguageChangeAsync ?? throw new ArgumentNullException(nameof(handleLanguageChangeAsync));
        _searchRoute = searchRoute ?? throw new ArgumentNullException(nameof(searchRoute));
        _describeRoute = describeRoute ?? throw new ArgumentNullException(nameof(describeRoute));
        _commonRoute = commonRoute ?? throw new ArgumentNullException(nameof(commonRoute));
    }

    public async Task<FormsCatalogIntentHandlingResult> ProcessPendingLanguageTargetAsync(
        FormsCatalogDialogTurnContext turnContext,
        string transcript,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        var intentResult = _intentParser.Parse(
            turnContext.Catalog,
            transcript,
            turnContext.IsoCode,
            turnContext.PolitenessLevel);
        if (intentResult.Intent == FormsCatalogIntent.Unknown)
        {
            return await _handleLanguageChangeAsync(turnContext, transcript, cancellationToken).ConfigureAwait(false);
        }

        if (intentResult.Intent is FormsCatalogIntent.Help or FormsCatalogIntent.Repeat or FormsCatalogIntent.List)
        {
            return await _handleLanguageChangeAsync(turnContext, null, cancellationToken)
                .ConfigureAwait(false);
        }

        return await ProcessIntentAsync(turnContext, intentResult, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FormsCatalogIntentHandlingResult> ProcessIntentAsync(
        FormsCatalogDialogTurnContext turnContext,
        FormsCatalogIntentResult intentResult,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(intentResult);

        if (intentResult.Intent == FormsCatalogIntent.Search)
        {
            return _searchRoute.Route(turnContext, intentResult);
        }

        if (intentResult.Intent == FormsCatalogIntent.Describe)
        {
            return _describeRoute.Route(turnContext, intentResult);
        }

        if (intentResult.Intent == FormsCatalogIntent.ChangeLanguage)
        {
            return await _handleLanguageChangeAsync(
                    turnContext,
                    intentResult.LanguageArgument,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return await _commonRoute.RouteAsync(turnContext, intentResult, cancellationToken).ConfigureAwait(false);
    }

    public FormsCatalogIntentHandlingResult CompletePendingDescription(
        FormsCatalogDialogTurnContext turnContext,
        SelectionCandidate<ApplicationInfo> selected)
    {
        return _describeRoute.CompletePendingDescription(turnContext, selected);
    }
}
