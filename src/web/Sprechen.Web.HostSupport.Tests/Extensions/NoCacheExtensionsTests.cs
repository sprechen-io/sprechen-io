using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sprechen.Web.HostSupport.Extensions;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Extensions;

public sealed class NoCacheExtensionsTests
{
    [Fact]
    public async Task UseNoCacheForHtmlResponses_WhenHtml_AddsNoStoreHeaders()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseNoCacheForHtmlResponses();
        app.Run(async context =>
        {
            context.Response.ContentType = "text/html; charset=utf-8";
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
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Accept = "text/html";

        await pipeline(context);

        Assert.Equal("no-store, no-cache, must-revalidate, max-age=0", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());
        Assert.Equal("0", context.Response.Headers["Expires"].ToString());
    }

    [Fact]
    public async Task UseNoCacheForHtmlResponses_WhenNotHtml_DoesNotAddHeaders()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var app = new ApplicationBuilder(services);
        app.UseNoCacheForHtmlResponses();
        app.Run(async context =>
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"ok\":true}");
        });

        var pipeline = app.Build();

        var context = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Headers.Accept = "application/json";

        await pipeline(context);

        Assert.False(context.Response.Headers.ContainsKey("Cache-Control"));
        Assert.False(context.Response.Headers.ContainsKey("Pragma"));
        Assert.False(context.Response.Headers.ContainsKey("Expires"));
    }
}


