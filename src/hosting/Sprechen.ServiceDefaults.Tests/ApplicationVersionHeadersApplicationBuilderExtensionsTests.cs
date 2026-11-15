using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.ServiceDefaults;
using Xunit;

namespace Sprechen.ServiceDefaults.Tests;

public sealed class ApplicationVersionHeadersApplicationBuilderExtensionsTests
{
    [Fact]
    public async Task UseSprechenApplicationVersionHeaders_AddsVersionAndRevisionHeaders()
    {
        var services = new ServiceCollection()
            .AddSingleton<IApplicationVersionInfoProvider>(
                new FakeApplicationVersionInfoProvider(
                    new ApplicationVersionInfo(
                        "2026.04.03.1",
                        "2026.04.03.1+ad0f8bc5deadbeef",
                        "1.0.0.0",
                        "ad0f8bc5deadbeef",
                        "ad0f8bc5")))
            .BuildServiceProvider();

        var app = new ApplicationBuilder(services);
        app.UseSprechenApplicationVersionHeaders();
        app.Run(async context =>
        {
            await context.Response.WriteAsync("ok");
        });

        var pipeline = app.Build();
        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };

        await pipeline(context);

        Assert.Equal("2026.04.03.1", context.Response.Headers["X-Sprechen-Version"].ToString());
        Assert.Equal("ad0f8bc5deadbeef", context.Response.Headers["X-Sprechen-Revision"].ToString());
    }
}
