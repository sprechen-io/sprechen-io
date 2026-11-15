using Sprechen.Voice.EvaluationTelemetry.BrowserInfrastructure;

namespace Sprechen.Voice.App.Tests.EvaluationTelemetry;

public sealed class BrowserEvaluationTelemetryOutboxStoreTests
{
    [Fact]
    public void ListKeys_SuppliesTheExclusiveV1PrefixToBrowserEnumeration()
    {
        var javascript = new RecordingInProcessJavascript();
        var store = new BrowserEvaluationTelemetryOutboxStore(javascript);

        var keys = store.ListKeys();

        Assert.Empty(keys);
        Assert.Equal("evaluationTelemetryOutbox.keys", javascript.Identifier);
        Assert.Equal(["sprechen.evaluation.outbox.v1:"], javascript.Arguments);
    }
}
