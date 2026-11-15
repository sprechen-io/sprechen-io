using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.ServiceDefaults;
using Xunit;

namespace Sprechen.ServiceDefaults.Tests;

public sealed class ApplicationVersionEndpointHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMachineReadableVersionPayload()
    {
        var provider = new FakeApplicationVersionInfoProvider(
            new ApplicationVersionInfo(
                "2026.04.03.1",
                "2026.04.03.1+ad0f8bc5deadbeef",
                "1.0.0.0",
                "ad0f8bc5deadbeef",
                "ad0f8bc5"));
        var result = ApplicationVersionEndpointHandler.Handle(provider);
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddLogging()
                .ConfigureHttpJsonOptions(_ => { })
                .BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };

        await result.ExecuteAsync(context);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApplicationVersionResponse>(
            context.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(payload);
        Assert.Equal("2026.04.03.1", payload.DisplayVersion);
        Assert.Equal("2026.04.03.1+ad0f8bc5deadbeef", payload.InformationalVersion);
        Assert.Equal("1.0.0.0", payload.AssemblyVersion);
        Assert.Equal("ad0f8bc5deadbeef", payload.SourceRevision);
        Assert.Equal("ad0f8bc5", payload.ShortSourceRevision);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
    }
}
