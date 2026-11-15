using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.FormSurface;
using Xunit;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class FormSurfaceSnapshotAssemblerTests
{
    [Fact]
    public async Task CreateAsync_WithoutFocusedField_KeepsDetailedStatuses()
    {
        var runtime = new Mock<IFormDialogSurfaceState>(MockBehavior.Strict);
        runtime
            .Setup(item => item.GetSurfaceFieldValuesAsync())
            .ReturnsAsync(ImmutableDictionary<string, FormSurfaceFieldValue>.Empty);
        runtime
            .Setup(item => item.GetFocusedFieldIdAsync())
            .ReturnsAsync((string?)null);
        runtime
            .Setup(item => item.GetDetailedStatusSnapshotAsync())
            .ReturnsAsync(new FormDetailedStatusSnapshot(
                [
                    new FormFieldStatusSnapshot(
                        "nameField",
                        "main",
                        Relevant: true,
                        Required: true,
                        Completed: false,
                        BlockingErrorCodes: [])
                ],
                [
                    new FormFieldGroupStatusSnapshot(
                        "main",
                        Relevant: true,
                        Completed: false,
                        OpenGroupErrorCodes: [])
                ]));

        var snapshot = await new FormSurfaceSnapshotAssembler()
            .CreateAsync(CreateIdentity(), runtime.Object, new FormSurfaceDefinition(), FormSurfaceStage.Active);

        Assert.Null(snapshot.FocusedFieldId);
        Assert.True(snapshot.FieldStatusesById.ContainsKey("nameField"));
        Assert.True(snapshot.FieldGroupStatusesById.ContainsKey("main"));
        Assert.Equal("de", snapshot.IsoCode);
        Assert.Equal("app", snapshot.ApplicationId);
        Assert.Equal("App", snapshot.ApplicationDisplayName);
        Assert.Equal("Teil 1", snapshot.FormDisplayName);
    }

    [Fact]
    public async Task CreateAsync_WhenDetailedStatusFaults_DoesNotTranslateToEmptyMaps()
    {
        var runtime = new Mock<IFormDialogSurfaceState>(MockBehavior.Strict);
        runtime
            .Setup(item => item.GetSurfaceFieldValuesAsync())
            .ReturnsAsync(ImmutableDictionary<string, FormSurfaceFieldValue>.Empty);
        runtime
            .Setup(item => item.GetFocusedFieldIdAsync())
            .ReturnsAsync("nameField");
        runtime
            .Setup(item => item.GetDetailedStatusSnapshotAsync())
            .ThrowsAsync(new InvalidOperationException("status projection failed"));

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new FormSurfaceSnapshotAssembler()
                .CreateAsync(CreateIdentity(), runtime.Object, new FormSurfaceDefinition(), FormSurfaceStage.Active));

        Assert.Equal("status projection failed", thrown.Message);
    }

    private static FormSurfacePublicationIdentity CreateIdentity()
        => new("de", "app", "App", "current", "Teil 1");
}
