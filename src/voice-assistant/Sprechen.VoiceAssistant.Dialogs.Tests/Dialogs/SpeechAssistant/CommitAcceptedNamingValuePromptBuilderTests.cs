using Sprechen.VoiceAssistant.Prompting.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Bestätigungstext einer übernommenen Mehrfachauswahl in fünf Sprachen.
/// Feldbezeichnung und gewählte Optionen müssen genannt werden, damit die Bestätigung
/// inhaltlich verständlich ist. Geprüft wird der erzeugte Text, keine Audioausgabe.
/// </summary>
public sealed class CommitAcceptedNamingValuePromptBuilderTests
{
    [Theory]
    [InlineData("de", "Kontaktkanäle ist E-Mail, SMS. Das wurde übernommen.")]
    [InlineData("en", "Kontaktkanäle: E-Mail, SMS. Accepted.")]
    [InlineData("fr", "Kontaktkanäle: E-Mail, SMS. Validé.")]
    [InlineData("es", "Kontaktkanäle: E-Mail, SMS. Aceptado.")]
    [InlineData("it", "Kontaktkanäle: E-Mail, SMS. Confermato.")]
    public void GetCommitAcceptedNamingValuePrompt_NamesTheItems_PerLanguage(string isoCode, string expected)
    {
        var prompt = FormConfirmationPromptBuilder.GetCommitAcceptedNamingValuePrompt(
            isoCode,
            fieldLabel: "Kontaktkanäle",
            confirmationValue: "E-Mail, SMS");

        Assert.Equal(expected, prompt);
    }
}
