using System.Collections.Immutable;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.WakeWord.Contracts;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft eine einmalige verkürzte Bestätigungsrückfrage nach fehlender oder
/// nicht deutbarer Antwort. Der bestehende Wert und Bestätigungszähler
/// bleiben erhalten; die Folgeantwort wird erneut ausgewertet.
/// Die Tests unterscheiden die protokollierten Korrekturgründe.
/// </summary>
public sealed class FormVoiceCommandConfirmationReaskTests
{
    private const string Silence = FormVoiceCommandConfirmationReaskFixture.Silence;
    private const string TransientFault = FormVoiceCommandConfirmationReaskFixture.TransientFault;

    [Fact]
    public async Task Silence_ThenAccept_ReasksOnceWithSilenceLeadIn_AndCommits()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, "übernehmen");

        var result = await fixture.HandleAsync();

        Assert.True(result.ShouldContinue);
        Assert.Equal(2, fixture.CapturePrompts.Count);
        Assert.StartsWith("Ich habe nichts gehört.", fixture.CapturePrompts[1], StringComparison.Ordinal);
        Assert.Contains("Übernehmen oder ändern?", fixture.CapturePrompts[1], StringComparison.Ordinal);
        Assert.Contains("max", fixture.CapturePrompts[1], StringComparison.Ordinal);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.NoInput), Times.Once);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.UnrecognizedInput), Times.Never);
        // Fortsetzungshinweis angefordert, keine Meldung über nicht verstandene Eingabe.
        Assert.DoesNotContain(fixture.Spoken, line => line.Contains("nicht verstanden", StringComparison.Ordinal));
        Assert.Contains(fixture.Spoken, line => line.Contains("weiter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unrecognized_ThenEdit_ReasksWithUnrecognizedLeadIn_AndRepairs()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture("blaugelb", "ändern");

        var result = await fixture.HandleAsync();

        Assert.True(result.ShouldContinue);
        Assert.Equal(2, fixture.CapturePrompts.Count);
        Assert.StartsWith("Das passt hier nicht.", fixture.CapturePrompts[1], StringComparison.Ordinal);
        Assert.DoesNotContain("blaugelb", fixture.CapturePrompts[1], StringComparison.Ordinal);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.UnrecognizedInput), Times.Once);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.ManualCorrection), Times.Once);
    }

    [Fact]
    public async Task SecondSilence_FallsIntoTheExistingRepairPath_WithoutAThirdCapture()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, Silence, "übernehmen");

        var result = await fixture.HandleAsync();

        Assert.True(result.ShouldContinue);
        Assert.Equal(2, fixture.CapturePrompts.Count);
        Assert.Contains(fixture.Spoken, line => line.Contains("nicht verstanden", StringComparison.Ordinal));
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.NoInput), Times.Once);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.UnrecognizedInput), Times.Once);
    }

    [Fact]
    public async Task SilenceThenUnrecognized_TheSecondIsTheSecond_NoThirdAttempt()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, "blaugelb", "übernehmen");

        await fixture.HandleAsync();

        Assert.Equal(2, fixture.CapturePrompts.Count);
        Assert.Contains(fixture.Spoken, line => line.Contains("nicht verstanden", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnchoredCommandOnTheReask_IsExecutedLikeOnTheFirstQuestion()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, "okay alex abbrechen");
        fixture.RouterReturnsNull(GlobalCommandIds.Cancel);

        await fixture.HandleAsync();

        fixture.Router.Verify(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == GlobalCommandIds.Cancel), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AfterAnAccumulationTurn_TheReaskSpeaksTheNoSpellShortForm()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, "übernehmen") { FromSpellingOrDigits = true };

        await fixture.HandleAsync();

        Assert.Contains("Übernehmen oder ändern?", fixture.CapturePrompts[1], StringComparison.Ordinal);
        Assert.DoesNotContain("buchstabieren", fixture.CapturePrompts[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TransientCaptureFault_CountsAsSilence()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(TransientFault, "übernehmen");

        await fixture.HandleAsync();

        Assert.StartsWith("Ich habe nichts gehört.", fixture.CapturePrompts[1], StringComparison.Ordinal);
        fixture.Telemetry.Verify(t => t.TrackRepairAttempt(It.IsAny<EvaluationSession>(), "name", FormVoiceCommandFieldErrorCodes.NoInput), Times.Once);
    }

    [Fact]
    public async Task InTheTeachingTurns_TheFirstQuestionIsFull_TheReaskIsShort()
    {
        var fixture = new FormVoiceCommandConfirmationReaskFixture(Silence, "übernehmen");

        await fixture.HandleAsync();

        Assert.Contains("Zum Übernehmen sagen Sie übernehmen", fixture.CapturePrompts[0], StringComparison.Ordinal);
        Assert.DoesNotContain("Zum Übernehmen sagen Sie", fixture.CapturePrompts[1], StringComparison.Ordinal);
        Assert.Equal(1, fixture.ConfirmationTurnCount);
    }

    [Fact]
    public void UndecidedWithoutAReason_IsNotConstructible()
    {
        Assert.Null(typeof(ConfirmationDecision).GetMethod("Undecided", Type.EmptyTypes));
        Assert.Null(typeof(FormVoiceCommandConfirmationMissingDecisionPolicy).GetMethod("Resolve", Type.EmptyTypes));
    }
}
