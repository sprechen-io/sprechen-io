using System.Net;
using System.Text;
using Sprechen.Web.HostSupport.Endpoints.Adapter;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Endpoints;

public sealed class ProxyTextResponseReaderTests
{
    [Fact]
    public async Task ReadAsync_JsonPayload_ReturnsBodyText()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"failed\"}", Encoding.UTF8, "application/json")
        };

        var text = await new ProxyTextResponseReader().ReadAsync(response, CancellationToken.None);

        Assert.Equal("{\"error\":\"failed\"}", text);
    }

    [Fact]
    public async Task ReadAsync_BinaryPayload_ReturnsNull()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new ByteArrayContent([0x01, 0x02])
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var text = await new ProxyTextResponseReader().ReadAsync(response, CancellationToken.None);

        Assert.Null(text);
    }
}
