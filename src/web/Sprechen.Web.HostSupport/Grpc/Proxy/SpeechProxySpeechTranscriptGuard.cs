using Sprechen.Proto;

namespace Sprechen.Web.HostSupport.Grpc.Proxy;

/// <summary>
/// Verlangt ein vorhandenes Antwortobjekt. Ein Transkript mit leerem Text bleibt zulässig.
/// </summary>
internal sealed class SpeechProxySpeechTranscriptGuard
{
    public SpeechTranscript RequireResponse(SpeechTranscript response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response;
    }
}
