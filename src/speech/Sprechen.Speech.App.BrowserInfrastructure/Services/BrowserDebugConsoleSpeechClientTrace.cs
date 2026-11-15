using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Verteilt korrelierte Clientspuren an Logger und Browserkonsole,
/// damit Diagnosefehler die Sprachverarbeitung nicht unterbrechen.
/// </summary>
internal sealed class BrowserDebugConsoleSpeechClientTrace(
    IBrowserDebugConsole browserDebugConsole,
    ILogger<BrowserDebugConsoleSpeechClientTrace> logger) : ISpeechClientTrace
{
    private readonly IBrowserDebugConsole _browserDebugConsole =
        browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));

    private readonly ILogger<BrowserDebugConsoleSpeechClientTrace> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask WriteAsync(
        string scope,
        LogLevel level,
        string? correlationId,
        string message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var normalizedScope = scope.Trim();
        var normalizedCorrelationId = string.IsNullOrWhiteSpace(correlationId)
            ? null
            : correlationId.Trim();
        var normalizedMessage = message.Trim();
        var browserMessage = normalizedCorrelationId is null
            ? normalizedMessage
            : $"cid={normalizedCorrelationId} {normalizedMessage}";

        LogIfPossible(normalizedScope, level, normalizedCorrelationId, normalizedMessage, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _browserDebugConsole
                .WriteAsync(
                    normalizedScope,
                    browserMessage,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Ein Fehler der Browserspur darf die Sprachverarbeitung nicht unterbrechen.
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private void LogIfPossible(
        string scope,
        LogLevel level,
        string? correlationId,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            Log(scope, level, correlationId, message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Ein Loggerfehler darf die Sprachverarbeitung nicht unterbrechen.
        }
    }

    private void Log(
        string scope,
        LogLevel level,
        string? correlationId,
        string message)
    {
        const string Template = "scope={Scope} cid={CorrelationId} {Message}";

        switch (level)
        {
            case LogLevel.Trace:
                _logger.LogTrace(Template, scope, correlationId ?? "<none>", message);
                break;
            case LogLevel.Debug:
                _logger.LogDebug(Template, scope, correlationId ?? "<none>", message);
                break;
            case LogLevel.Information:
                _logger.LogInformation(Template, scope, correlationId ?? "<none>", message);
                break;
            case LogLevel.Warning:
                _logger.LogWarning(Template, scope, correlationId ?? "<none>", message);
                break;
            case LogLevel.Error:
                _logger.LogError(Template, scope, correlationId ?? "<none>", message);
                break;
            case LogLevel.Critical:
                _logger.LogCritical(Template, scope, correlationId ?? "<none>", message);
                break;
            default:
                _logger.LogInformation(Template, scope, correlationId ?? "<none>", message);
                break;
        }
    }
}
