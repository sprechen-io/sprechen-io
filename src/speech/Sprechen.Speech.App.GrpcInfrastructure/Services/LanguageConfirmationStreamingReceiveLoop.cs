using Grpc.Core;
using Sprechen.Proto;
using GrpcSpeechConversationEvent = Sprechen.Proto.SpeechConversationEventMessage;

namespace Sprechen.Speech.App.GrpcInfrastructure.Services;

/// <summary>
/// Überführt gRPC-Ereignisse in den Bestätigungszustand, damit das Sprachergebnis aus dem Gesprächsstrom entsteht.
/// Nach vollständig gelesenem Antwortstream wird ein gespeicherter Gesprächsfehler weitergegeben.
/// Bei vorherigem <c>Cancelled</c> oder <see cref="OperationCanceledException"/> entfällt diese Weitergabe.
/// </summary>
internal sealed class LanguageConfirmationStreamingReceiveLoop
{
    private readonly LanguageConfirmationConversationState _conversationState;
    private readonly CancellationToken _sessionCancellationToken;

    public LanguageConfirmationStreamingReceiveLoop(
        LanguageConfirmationConversationState conversationState,
        CancellationToken sessionCancellationToken)
    {
        _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
        _sessionCancellationToken = sessionCancellationToken;
    }

    public async Task RunAsync(
        IAsyncStreamReader<GrpcSpeechConversationEvent> responseStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(responseStream);

        Exception? terminalFailure = null;
        try
        {
            while (await responseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var conversationEvent = SpeechConversationEventGrpcMapper.FromProto(responseStream.Current);
                _conversationState.Apply(conversationEvent);

                if (conversationEvent is SpeechConversationFailedEvent failed)
                {
                    terminalFailure = CreateTerminalFailure(failed);
                }
            }

            if (terminalFailure is not null)
            {
                throw terminalFailure;
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            // Der Abbruch wird hier als reguläres Ende der Empfangsschleife behandelt.
        }
        catch (RpcException ex)
        {
            throw SpeechErrorExceptionFactory.Create(ex, _sessionCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Der Abbruch wird hier als reguläres Ende der Empfangsschleife behandelt.
        }
    }

    private static Exception CreateTerminalFailure(SpeechConversationFailedEvent failed)
        => new InvalidOperationException(
            string.IsNullOrWhiteSpace(failed.Detail)
                ? "Language confirmation stream failed."
                : failed.Detail);
}
