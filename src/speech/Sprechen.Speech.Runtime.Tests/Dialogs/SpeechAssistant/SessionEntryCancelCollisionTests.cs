using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.LanguageContracts;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Kollisionen von Abbruch- und Antwortwörtern für die fünf aufgeführten Sprachen.
/// Abbruchwörter beider Höflichkeitsstufen dürfen keine Sprachwahl bestätigen;
/// modellierte Antworten dürfen nicht auf formale Abbruchwörter passen.
/// Weitere Fälle prüfen Ankerentfernung und vollständige Äußerungen.
/// </summary>
public sealed class SessionEntryCancelCollisionTests
{
    private static readonly string[] Languages = ["de", "en", "fr", "es", "it"];
    private static readonly VoiceCommandCatalogCancelVocabulary Vocabulary = new();
    private static readonly ILanguageCatalog Endonyms = VoiceInterfaceLanguageCatalog.CreateDefault();

    public static TheoryData<string, string> SessionAndAskedLanguagePairs()
    {
        var pairs = new TheoryData<string, string>();
        foreach (var session in Languages)
        {
            foreach (var asked in Languages)
            {
                pairs.Add(session, asked);
            }
        }

        return pairs;
    }

    // Ein Abbruchwort der Sitzungssprache darf in keiner angebotenen Sprache als Zustimmung gelten.
    [Theory]
    [MemberData(nameof(SessionAndAskedLanguagePairs))]
    public void ACancelWord_IsNeverAnAcceptance(string sessionIsoCode, string askedIsoCode)
    {
        foreach (var politeness in new[] { PolitenessLevel.Formal, PolitenessLevel.Informal })
        {
            foreach (var cancelWord in Vocabulary.GetCancelUtterances(sessionIsoCode, politeness))
            {
                Assert.False(
                    SessionEntryAnswerReader.AcceptsTheSessionLanguage(cancelWord, askedIsoCode),
                    $"'{cancelWord}' ({sessionIsoCode}) must not accept the keep rung of {askedIsoCode}.");
                Assert.False(
                    SessionEntryAnswerReader.AcceptsTheOfferedLanguage(cancelWord, askedIsoCode, sessionIsoCode),
                    $"'{cancelWord}' ({sessionIsoCode}) must not accept the {askedIsoCode} rung.");
            }
        }
    }

    // Umgekehrt dürfen modellierte Antworten kein formales Abbruchwort treffen.
    // Diese Textprüfung misst keine akustischen Erkennungsfehler.
    [Theory]
    [MemberData(nameof(SessionAndAskedLanguagePairs))]
    public void NoModeledAnswer_ReadsAsACancel(string sessionIsoCode, string answerIsoCode)
    {
        var cancelUtterances = Vocabulary.GetCancelUtterances(sessionIsoCode, PolitenessLevel.Formal);

        var modeledAnswers = VoiceInterfaceModelCatalog.Lexicon.YesNo.GetYesTokens(answerIsoCode)
            .Concat(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNoTokens(answerIsoCode))
            .Concat(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNotUnderstoodPhrases(answerIsoCode))
            .Concat(VoiceInterfaceModelCatalog.Lexicon.YesNo.GetNegationCues(answerIsoCode))
            .Append(Endonyms.GetEndonym(LanguageCode.ParseIso6391(answerIsoCode)))
            .Append(Endonyms.GetDisplayName(
                LanguageCode.ParseIso6391(answerIsoCode),
                LanguageCode.ParseIso6391(sessionIsoCode)));

        foreach (var answer in modeledAnswers)
        {
            Assert.False(
                SessionEntryCancelReader.IsCancelled(answer, cancelUtterances),
                $"'{answer}' ({answerIsoCode}) must not read as a cancel of {sessionIsoCode}.");
        }
    }

    // Nach Entfernen des Aktivierungsankers muss ein Abbruchwort weiterhin abbrechen; der Anker allein genügt nicht.
    [Fact]
    public void AnAnchoredCancel_StillCancelsAfterTheActivationWordIsStripped()
    {
        var cancelUtterances = Vocabulary.GetCancelUtterances("de", PolitenessLevel.Formal);

        var stripped = SessionEntryActivationWordFilter.Strip("Okay Alex abbrechen");
        Assert.True(SessionEntryCancelReader.IsCancelled(stripped, cancelUtterances));

        var anchorOnly = SessionEntryActivationWordFilter.Strip("Okay Alex");
        Assert.False(SessionEntryCancelReader.IsCancelled(anchorOnly, cancelUtterances));
    }

    // Nur die ganze Äußerung darf als Abbruchwort gelten; Einbettung, vorangestellte Wörter und Verdoppelung müssen ohne Treffer bleiben.
    [Theory]
    [InlineData("ich moechte abbrechen")]
    [InlineData("abbrechen bitte")]
    [InlineData("nicht abbrechen")]
    [InlineData("abbrechen abbrechen")]
    public void AnEmbeddedCancelWord_IsNotACancel(string transcript)
    {
        var cancelUtterances = Vocabulary.GetCancelUtterances("de", PolitenessLevel.Formal);

        Assert.False(SessionEntryCancelReader.IsCancelled(transcript, cancelUtterances));
    }

    // Großschreibung und Satzzeichen ändern den Treffer eines alleinstehenden Abbruchworts nicht.
    [Theory]
    [InlineData("Abbrechen.")]
    [InlineData("ABBRECHEN")]
    [InlineData(" beenden ")]
    public void RecognizerPunctuationAndCasing_DoNotHideTheCancelWord(string transcript)
    {
        var cancelUtterances = Vocabulary.GetCancelUtterances("de", PolitenessLevel.Formal);

        Assert.True(SessionEntryCancelReader.IsCancelled(transcript, cancelUtterances));
    }
}
