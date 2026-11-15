using Sprechen.Mda.Contracts.V1.FormContracts;
using Xunit;

namespace Sprechen.Mda.Contracts.Tests.FormContracts;

/// <summary>
/// Prüft die Auswahl des ersten unterstützten Eingabemodus, das Entfernen äußerer
/// Leerzeichen, den Standardmodus und die zugehörigen Übertragungswerte.
/// </summary>
public sealed class InputModesResolveExpectedInputModeTests
{
    [Fact]
    public void NullOrEmpty_DefaultsToVoice()
    {
        Assert.Equal(InputMode.Voice, InputModes.ResolveExpectedInputMode(null));
        Assert.Equal(InputMode.Voice, InputModes.ResolveExpectedInputMode([]));
        Assert.Equal(InputModes.Voice, InputModes.ResolveExpectedInputMode(null).ToWireValue());
    }

    [Theory]
    [InlineData("voice", InputMode.Voice)]
    [InlineData("keyboard", InputMode.Keyboard)]
    [InlineData("spelling", InputMode.Spelling)]
    [InlineData("digits", InputMode.Digits)]
    [InlineData("literal", InputMode.Literal)]
    public void SingleSupportedMode_ResolvesToThatModeAndWire(string wire, InputMode expected)
    {
        var resolved = InputModes.ResolveExpectedInputMode([wire]);

        Assert.Equal(expected, resolved);
        Assert.Equal(wire, resolved.ToWireValue());
    }

    [Fact]
    public void FirstSupportedMatch_Wins_SkippingUnsupported()
    {
        var resolved = InputModes.ResolveExpectedInputMode(["nonsense", "spelling", "digits"]);

        Assert.Equal(InputMode.Spelling, resolved);
        Assert.Equal(InputModes.Spelling, resolved.ToWireValue());
    }

    [Fact]
    public void UnsupportedOnly_DefaultsToVoice()
    {
        var resolved = InputModes.ResolveExpectedInputMode(["nonsense", "typed"]);

        Assert.Equal(InputMode.Voice, resolved);
    }

    [Theory]
    [InlineData("  keyboard  ", InputMode.Keyboard)]
    [InlineData("digits", InputMode.Digits)]
    public void WhitespaceIsTrimmed_LikeBefore(string raw, InputMode expected)
    {
        Assert.Equal(expected, InputModes.ResolveExpectedInputMode([raw]));
    }
}
