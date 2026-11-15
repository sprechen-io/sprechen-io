using Microsoft.AspNetCore.Http;
using Sprechen.Forms.Routing;
using Xunit;

namespace Sprechen.Forms.Tests.Routing;

public sealed class UmfrageStudyLinkRewriterTests
{
    [Theory]
    [InlineData("/umfrage/probe", true)]
    [InlineData("/umfrage/Probe", false)]
    [InlineData("/Umfrage/probe", false)]
    [InlineData("/umfrage/probe/", false)]
    [InlineData("/umfrage/probe/extra", false)]
    [InlineData("/umfrage/xyz001", false)]
    public void OperationalProbeEntry_IsExactAndNeverUsesAParticipantToken(string url, bool expectedProbe)
    {
        var rewriter = new UmfrageStudyLinkRewriter();
        Assert.True(rewriter.TryRewriteToIndex(url, out _, out var identity));
        Assert.NotNull(identity);
        Assert.Equal(expectedProbe, identity.IsOperationalProbe);
        if (expectedProbe)
        {
            Assert.Null(identity.Token);
            Assert.False(identity.IsRejected);
        }
    }

    [Fact]
    public void TryRewriteToIndex_RejectsASegmentThatUsedToBeValid()
    {
        var rewriter = new UmfrageStudyLinkRewriter();
        Assert.True(rewriter.TryRewriteToIndex("/umfrage/x9jdln/variante/X", out _, out var variant));
        Assert.Equal("segments", variant!.RejectionReason);
        Assert.True(rewriter.TryRewriteToIndex("/umfrage/x9jdln/aufgabe/T01", out _, out var task));
        Assert.Equal("segments", task!.RejectionReason);
    }
}
