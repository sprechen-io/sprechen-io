using Microsoft.Extensions.Logging;

namespace Sprechen.Speech.App.BrowserInfrastructure.Services;

/// <summary>
/// Verteilt korrelierte Assistentenspuren an Logger und Browserkonsole,
/// damit Diagnosefehler den Ablauf des Sprachassistenten nicht unterbrechen.
/// </summary>
internal sealed class BrowserDebugConsoleSpeechAssistantTrace(
    IBrowserDebugConsole browserDebugConsole,
    ILogger<BrowserDebugConsoleSpeechAssistantTrace> logger) : ISpeechAssistantTrace
{
    private const string BrowserScope = "speech-assistant";

    private readonly IBrowserDebugConsole _browserDebugConsole =
        browserDebugConsole ?? throw new ArgumentNullException(nameof(browserDebugConsole));

    private readonly ILogger<BrowserDebugConsoleSpeechAssistantTrace> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    public async ValueTask WriteAsync(
        string correlationId,
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var normalizedCorrelationId = correlationId.Trim();
        var normalizedMessage = message.Trim();
        var browserMessage = $"cid={normalizedCorrelationId} level={FormatLevel(level)} scope={scope} {normalizedMessage}";

        LogIfPossible(scope, level, normalizedCorrelationId, normalizedMessage, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _browserDebugConsole
                .WriteAsync(
                    BrowserScope,
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
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string correlationId,
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
        SpeechAssistantTraceScope scope,
        SpeechAssistantTraceLevel level,
        string correlationId,
        string message)
    {
        var formattedMessage = "cid={CorrelationId} scope={Scope} {Message}";

        switch (level)
        {
            case SpeechAssistantTraceLevel.Debug:
                _logger.LogDebug(formattedMessage, correlationId, scope, message);
                break;
            case SpeechAssistantTraceLevel.Information:
                _logger.LogInformation(formattedMessage, correlationId, scope, message);
                break;
            case SpeechAssistantTraceLevel.Warning:
                _logger.LogWarning(formattedMessage, correlationId, scope, message);
                break;
            case SpeechAssistantTraceLevel.Error:
                _logger.LogError(formattedMessage, correlationId, scope, message);
                break;
            default:
                _logger.LogDebug(formattedMessage, correlationId, scope, message);
                break;
        }
    }

    private static string FormatLevel(SpeechAssistantTraceLevel level)
        => level switch
        {
            SpeechAssistantTraceLevel.Debug => "debug",
            SpeechAssistantTraceLevel.Information => "info",
            SpeechAssistantTraceLevel.Warning => "warn",
            SpeechAssistantTraceLevel.Error => "error",
            _ => "debug"
        };
}
