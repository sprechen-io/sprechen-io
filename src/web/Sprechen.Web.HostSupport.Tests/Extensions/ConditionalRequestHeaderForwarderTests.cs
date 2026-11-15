using Microsoft.AspNetCore.Http;
using Sprechen.Web.HostSupport.Extensions;
using Xunit;

namespace Sprechen.Web.HostSupport.Tests.Extensions;

public sealed class ConditionalRequestHeaderForwarderTests
{
    [Fact]
    public void CopyIfNoneMatch_WhenHeaderIsValid_ForwardsEntityTag()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = "\"catalog-etag\"";

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://catalog.test/contracts/forms-catalog");

        ConditionalRequestHeaderForwarder.CopyIfNoneMatch(httpContext, request);

        var entityTag = Assert.Single(request.Headers.IfNoneMatch);
        Assert.Equal("\"catalog-etag\"", entityTag.ToString());
    }

    [Fact]
    public void CopyIfNoneMatch_WhenHeaderIsMalformed_DoesNotForwardIt()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.IfNoneMatch = "catalog-etag";

        using var request = new HttpRequestMessage(HttpMethod.Get, "http://catalog.test/contracts/forms-catalog");

        ConditionalRequestHeaderForwarder.CopyIfNoneMatch(httpContext, request);

        Assert.Empty(request.Headers.IfNoneMatch);
    }
}


