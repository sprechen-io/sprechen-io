using System.Net;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Zählt die beim Testhandler eingegangenen HTTP-Anfragen des Reporters.
/// </summary>
internal sealed class CountingHttpMessageHandler : HttpMessageHandler
{
    public int Requests { get; private set; }

    /// <summary>
    /// Inhalt der letzten Anfrage, damit Tests neben der Anzahl auch die übertragenen Felder prüfen können.
    /// </summary>
    public string? LastBody { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests++;
        LastBody = request.Content?.ReadAsStringAsync(cancellationToken).GetAwaiter().GetResult();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
    }
}
