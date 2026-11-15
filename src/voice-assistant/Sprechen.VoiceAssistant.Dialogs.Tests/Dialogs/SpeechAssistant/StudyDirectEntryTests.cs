using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.TestDoubles;
using Sprechen.VoiceAssistant.EvaluationTelemetry;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant.Exceptions;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class StudyDirectEntryTests
{
    [Fact]
    public async Task TryBindAsync_WithoutStudyEntry_ReturnsNull()
    {
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()));

        var bound = await entry.TryBindAsync(false, null, "de", CancellationToken.None);

        Assert.Null(bound);
    }

    [Fact]
    public async Task TryBindAsync_WithStudyEntry_ReturnsStudyApplicationAndFirstForm()
    {
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()));

        var bound = await entry.TryBindAsync(true, null, "de", CancellationToken.None);

        Assert.NotNull(bound);
        Assert.Equal("study.umfrage", bound.Session.ApplicationId);
        Assert.Equal("hintergrund-und-setup", bound.Session.FormId);
        Assert.Equal("Umfrage", bound.ApplicationDisplayName);
    }

    [Fact]
    public async Task TryBindAsync_ResolvesDisplayNameInSessionLanguage()
    {
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()));

        var bound = await entry.TryBindAsync(true, null, "en", CancellationToken.None);

        Assert.Equal("Survey", bound!.ApplicationDisplayName);
    }

    [Fact]
    public async Task TryBindAsync_WhenNoStudyApplication_Throws()
    {
        var entry = CreateEntry(new FixedFormsCatalogDataClient(new FormsCatalog()));

        await Assert.ThrowsAsync<StudyBindFailedException>(
            () => entry.TryBindAsync(true, null, "de", CancellationToken.None));
    }

    [Fact]
    public async Task TryBindAsync_WhenMultipleStudyApplications_Throws()
    {
        var catalog = CreateStudyCatalog();
        catalog.Applications = [catalog.Applications[0], catalog.Applications[0]];
        var entry = CreateEntry(new FixedFormsCatalogDataClient(catalog));
        await Assert.ThrowsAsync<StudyBindFailedException>(() => entry.TryBindAsync(true, null, "de", CancellationToken.None));
    }

    [Fact]
    public async Task TryBindAsync_WhenStudyHasNoForms_Throws()
    {
        var catalog = CreateStudyCatalog();
        catalog.Applications[0].Forms = [];
        var entry = CreateEntry(new FixedFormsCatalogDataClient(catalog));
        await Assert.ThrowsAsync<StudyBindFailedException>(() => entry.TryBindAsync(true, null, "de", CancellationToken.None));
    }

    [Fact]
    public void CurrentProbeContext_WhenContextMissing_ThrowsDomainException()
    {
        var probe = new OperationalProbeSession();
        probe.Request();
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()), probe);

        Assert.Throws<OperationalProbeContextException>(() => entry.CurrentProbeContext());
    }

    [Fact]
    public void CurrentProbeContext_WhenParticipantIdentityPresent_ThrowsDomainException()
    {
        var probe = new OperationalProbeSession();
        var context = new OperationalProbeContext("probe-0123456789abcdef0123456789abcdef", "B");
        probe.Request();
        probe.AcceptContext(context);
        var participant = new StudyTelemetryContext();
        participant.SetStudyContext("xyz001");
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()), probe, participant);

        Assert.Throws<OperationalProbeContextException>(() => entry.CurrentProbeContext());
    }

    [Fact]
    public void CurrentProbeContext_WhenContextAcceptedWithoutParticipant_PassesThrough()
    {
        var probe = new OperationalProbeSession();
        var context = new OperationalProbeContext("probe-0123456789abcdef0123456789abcdef", "B");
        probe.Request();
        probe.AcceptContext(context);
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()), probe);

        Assert.Same(context, entry.CurrentProbeContext());
    }

    [Fact]
    public async Task TryBindAsync_WithProbeContext_BindsTheStudySessionToTheProbe()
    {
        var probe = new OperationalProbeSession();
        var context = new OperationalProbeContext("probe-0123456789abcdef0123456789abcdef", "B");
        probe.Request();
        probe.AcceptContext(context);
        var entry = CreateEntry(new FixedFormsCatalogDataClient(CreateStudyCatalog()), probe);

        var bound = await entry.TryBindAsync(true, context, "de", CancellationToken.None);

        Assert.NotNull(bound);
        Assert.True(probe.Matches("probe-0123456789abcdef0123456789abcdef", "B", "study.umfrage", "hintergrund-und-setup"));
    }

    private static StudyDirectEntry CreateEntry(
        FixedFormsCatalogDataClient catalogClient,
        OperationalProbeSession? probe = null,
        StudyTelemetryContext? participant = null)
        => new(
            catalogClient,
            probe ?? new OperationalProbeSession(),
            participant ?? new StudyTelemetryContext());

    private static FormsCatalog CreateStudyCatalog()
    {
        return new FormsCatalog
        {
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "study.umfrage",
                    DisplayName = new LocalizedText { ["de"] = "Umfrage", ["en"] = "Survey" },
                    Description = new LocalizedText { ["de"] = "Studie" },
                    SupportedLanguages = ["de", "en"],
                    DefaultLanguage = "de",
                    IsStudyApplication = true,
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("hintergrund-und-setup"),
                            DisplayName = new LocalizedText { ["de"] = "Quizabend" },
                            Description = new LocalizedText { ["de"] = "Form" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"],
                            Sequence = 1
                        }
                    ]
                }
            ]
        };
    }
}
