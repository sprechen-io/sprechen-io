using System.Collections.Immutable;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Befehle mit vorangestelltem Aktivierungswort beim Buchstabieren, bei Ziffern und
/// bei Mehrfachauswahl. Hilfe erhält die bisherige Eingabe, ein Feldwechsel beendet sie.
/// Ein Befehlswort ohne Aktivierungswort geht an den Parser. Das alleinige Aktivierungswort
/// löst einen Hinweis aus; ein unbekannter Befehl wird abgelehnt. Parser und Sprachdienst
/// sind simuliert, geprüft werden Rückgaben, Aufrufe und Ausgabetexte.
/// </summary>
public sealed class AnchoredCommandInAccumulationTests
{
    [Fact]
    public async Task Spelling_AnchoredHelp_RunsTheHelpHandler_KeepsTheBuffer()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        fixture.RouterReturnsNull(GlobalCommandIds.Help);

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex hilfe", 90), ("Berta", 90), ("fertig", 90));

        fixture.Router.Verify(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == GlobalCommandIds.Help), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("ab", result.CapturedTranscript);
    }

    [Fact]
    public async Task Spelling_BareHelpWord_StaysContent_NeverReachesTheRouter()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();

        var result = await fixture.RunSpellingAsync("Anton", ("hilfe", 90), ("fertig", 90));

        fixture.Router.VerifyNoOtherCalls();
        fixture.Parser.Verify(p => p.ParseSpelling("de", "hilfe"), Times.Once);
        Assert.Equal("a", result.CapturedTranscript);
    }

    [Fact]
    public async Task Spelling_AnchoredDoneWord_FinalizesWithoutAnchorLetters()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex fertig", 90));

        fixture.Router.VerifyNoOtherCalls();
        Assert.True(result.HasCapturedTranscript);
        Assert.Equal("a", result.CapturedTranscript);
    }

    [Fact]
    public async Task Spelling_AnchoredNext_FinalizesLikeDone()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex weiter", 90));

        fixture.Router.VerifyNoOtherCalls();
        Assert.Equal("a", result.CapturedTranscript);
    }

    [Fact]
    public async Task Spelling_BareAnchor_SpeaksTheAnchorHint_KeepsTheBuffer()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex", 90), ("fertig", 90));

        fixture.Router.VerifyNoOtherCalls();
        Assert.Contains(fixture.Spoken, line => line.Contains("Okay Alex Hilfe", StringComparison.Ordinal));
        Assert.Equal("a", result.CapturedTranscript);
    }

    [Fact]
    public async Task Spelling_AnchoredNonCommand_IsRejected_NotSpelled()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex berta cesar", 90), ("fertig", 90));

        fixture.Router.VerifyNoOtherCalls();
        fixture.Parser.Verify(p => p.ParseSpelling("de", It.Is<string>(t => t.Contains("okay", StringComparison.OrdinalIgnoreCase))), Times.Never);
        Assert.Equal("a", result.CapturedTranscript);
        Assert.Contains(fixture.Spoken, line => line.Contains("nicht verstanden", StringComparison.OrdinalIgnoreCase) || line.Contains("passt hier nicht", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Spelling_AnchoredJump_LeavesTheTurnWithoutAValue()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        fixture.RouterMovesFocusTo(GlobalCommandIds.JumpTo, "alter", fixture.CreateField(InputMode.Voice, fieldId: "alter"));

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex gehe zu alter", 90), ("Berta", 90), ("fertig", 90));

        Assert.False(result.HasCapturedTranscript);
        Assert.True(result.ExitedOnFocusOrModeChange);
        Assert.Null(result.DialogResult);
        // Nach dem Sprung bleiben zwei weitere Texte und zwei simulierte Eingaben ohne Sprache unverbraucht.
        Assert.Equal(4, fixture.RemainingCaptures);
    }

    [Fact]
    public async Task Spelling_AnchoredCommandWithDialogResult_ReturnsIt()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        var dialogResult = VoiceCommandDialogResult.Cancelled("app", "current", ImmutableDictionary<string, string>.Empty);
        fixture.Router
            .Setup(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == GlobalCommandIds.Cancel), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dialogResult);

        var result = await fixture.RunSpellingAsync("Anton", ("okay alex abbrechen", 90), ("Berta", 90));

        Assert.False(result.HasCapturedTranscript);
        Assert.Same(dialogResult, result.DialogResult);
    }

    [Fact]
    public async Task Digits_AnchoredHelp_RunsTheHelpHandler_KeepsTheBuffer()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        fixture.RouterReturnsNull(GlobalCommandIds.Help);

        var result = await fixture.RunDigitsAsync("eins zwei", ("okay alex hilfe", 90), ("drei", 90), ("fertig", 90));

        fixture.Router.Verify(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == GlobalCommandIds.Help), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("123", result.CapturedTranscript);
    }

    [Fact]
    public async Task Digits_AnchoredJumpWithArgument_LeavesTheTurn()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        fixture.RouterMovesFocusTo(GlobalCommandIds.JumpTo, "alter", fixture.CreateField(InputMode.Voice, fieldId: "alter"));

        var result = await fixture.RunDigitsAsync("eins", ("okay alex gehe zu alter", 90), ("zwei", 90), ("fertig", 90));

        Assert.False(result.HasCapturedTranscript);
        Assert.True(result.ExitedOnFocusOrModeChange);
        Assert.Equal(4, fixture.RemainingCaptures);
    }

    [Fact]
    public async Task Set_AnchoredHelp_RunsTheHelpHandler_KeepsTheSelection()
    {
        var fixture = new AnchoredCommandInAccumulationFixture();
        fixture.RouterReturnsNull(GlobalCommandIds.Help);

        var result = await fixture.RunSetAsync("email", ("okay alex hilfe", 90), ("sms", 90), ("fertig", 90));

        fixture.Router.Verify(r => r.TryHandleAsync(It.Is<CommandId>(c => c.Value == GlobalCommandIds.Help), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(result.HasCapturedTranscript);
        Assert.Contains("email", result.CapturedTranscript, StringComparison.Ordinal);
        Assert.Contains("sms", result.CapturedTranscript, StringComparison.Ordinal);
    }
}
