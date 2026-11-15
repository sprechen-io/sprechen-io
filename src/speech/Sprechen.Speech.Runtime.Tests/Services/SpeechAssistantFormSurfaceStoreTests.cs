using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.FormSurface;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechAssistantFormSurfaceStoreTests
{
    [Fact]
    public void TryPublishActive_WhenGenerationIsStale_RejectsTheWrite()
    {
        var store = new SpeechAssistantFormSurfaceStore();
        var first = store.BeginSession();
        _ = store.BeginSession();

        var accepted = store.TryPublishActive(first, CreateView(FormSurfaceStage.Active));

        Assert.False(accepted);
        Assert.Null(store.Current);
    }

    [Fact]
    public void TryPublishTerminal_WhenValuesArePresent_FailsBeforeMutation()
    {
        var store = new SpeechAssistantFormSurfaceStore();
        var session = store.BeginSession();
        var view = CreateView(
            FormSurfaceStage.Submitted,
            new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal)
            {
                ["name"] = new(FormSurfaceFieldValueKind.PublicValue, "Ada")
            });

        Assert.Throws<InvalidOperationException>(() => store.TryPublishTerminal(session, view));
        Assert.Null(store.Current);
    }

    [Fact]
    public void BeginSession_ClearsAPreviousSurface()
    {
        var store = new SpeechAssistantFormSurfaceStore();
        var first = store.BeginSession();
        Assert.True(store.TryPublishActive(first, CreateView(FormSurfaceStage.Active)));
        Assert.NotNull(store.Current);

        var second = store.BeginSession();

        Assert.Null(store.Current);
        Assert.False(store.TryPublishActive(first, CreateView(FormSurfaceStage.Active)));
        Assert.True(store.TryPublishActive(second, CreateView(FormSurfaceStage.Active)));
    }

    [Fact]
    public void TryPublishActive_WhenCorrelationIdDoesNotMatch_RejectsTheWrite()
    {
        var store = new SpeechAssistantFormSurfaceStore();
        var session = store.BeginSession();
        var forged = new FormSurfacePublicationSession(session.Generation, Guid.NewGuid());

        Assert.False(store.TryPublishActive(forged, CreateView(FormSurfaceStage.Active)));
        Assert.Null(store.Current);
    }

    [Fact]
    public void Stop_ClearsAnActiveSurfaceAndRejectsLaterWrites()
    {
        var store = new SpeechAssistantFormSurfaceStore();
        var session = store.BeginSession();
        Assert.True(store.TryPublishActive(session, CreateView(FormSurfaceStage.Active)));

        store.Stop();

        Assert.Null(store.Current);
        Assert.False(store.TryPublishActive(session, CreateView(FormSurfaceStage.Active)));
    }

    private static SpeechAssistantFormSurfaceView CreateView(
        FormSurfaceStage stage,
        IReadOnlyDictionary<string, FormSurfaceFieldValue>? values = null)
        => SpeechAssistantFormSurfaceView.FromSafeSnapshot(new FormSurfaceSnapshot(
            stage,
            "de",
            "app",
            "App",
            "form",
            "Form",
            new FormSurfaceDefinition(),
            values ?? new Dictionary<string, FormSurfaceFieldValue>(StringComparer.Ordinal),
            focusedFieldId: null,
            new Dictionary<string, FormSurfaceFieldStatus>(StringComparer.Ordinal),
            new Dictionary<string, FormSurfaceFieldGroupStatus>(StringComparer.Ordinal)));
}
