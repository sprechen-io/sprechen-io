using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.Services;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft begrenzte Zeichentoleranz bei Sprachnamen und die Ablehnung mehrdeutiger
/// Näherungstreffer, verneinter Wünsche sowie längerer Suchphrasen.
/// </summary>
public sealed class LanguageNameNearMissTests
{
    [Theory]
    // Fälle mit einem geänderten oder fehlenden Zeichen, Akzentnormalisierung und Satzzeichen.
    [InlineData("Espanyol.", "it", "es")]
    [InlineData("Italiana", "de", "it")]
    [InlineData("Espanol", "de", "es")]
    [InlineData("Francais", "de", "fr")]
    [InlineData("Deutch", "en", "de")]
    public void ANameOneCharacterOff_StillNamesItsLanguage(string transcript, string sessionIso, string expected)
    {
        var resolver = new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault());

        Assert.True(
            resolver.TryResolve(transcript, sessionIso, out var target, out _),
            $"'{transcript}' was discarded in a {sessionIso} session; it is one edit from {expected}");
        Assert.Equal(expected, target);
    }

    [Theory]
    // Außerhalb der bekannten Namen und der Toleranz von einer Zeichenänderung bleibt das Ziel unbekannt.
    [InlineData("Espaniolo")]
    [InlineData("Katalanisch")]
    [InlineData("Kartoffelsalat")]
    public void AWordThatIsNotAName_IsStillRefused(string transcript)
    {
        var resolver = new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault());

        Assert.False(resolver.TryResolve(transcript, "de", out _, out _));
    }

    [Fact]
    public void AShortWord_NeverDriftsIntoALanguage()
    {
        // Kurze Wörter sind von Näherungstreffern ausgeschlossen. Gültige kurze Sprachcodes
        // wie es und en werden dagegen bereits im exakten Vergleich erkannt.
        var resolver = new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault());

        foreach (var transcript in new[] { "ist", "das", "und", "eine", "denn", "dann" })
        {
            Assert.False(
                resolver.TryResolve(transcript, "de", out _, out _),
                $"'{transcript}' resolved to a language");
        }
    }

    [Theory]
    // Modellierte Bitten dürfen den Sprachnamen umgeben.
    // Nur passende Randwörter werden entfernt; ein beliebiger Treffer im Satz reicht nicht.
    [InlineData("Parla italiano.", "it")]
    [InlineData("Vorrei l'italiano.", "it")]
    [InlineData("Ich haette gerne Italienisch", "it")]
    [InlineData("Je voudrais le francais", "fr")]
    public void ALanguageAskedForInASentence_IsRead(string transcript, string expected)
    {
        var resolver = new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault());

        Assert.True(resolver.TryResolve(transcript, "de", out var target, out _), $"'{transcript}' verworfen");
        Assert.Equal(expected, target);
    }

    [Theory]
    // Das Rahmen-Trimmen muss Verneinungen und mehrdeutige Sprachangaben erhalten.
    [InlineData("Ich moechte kein Italienisch")]
    [InlineData("nicht italienisch")]
    [InlineData("Non voglio italiano")]
    [InlineData("I don't want English")]
    // Zwei genannte Sprachen bestimmen kein eindeutiges Wechselziel.
    [InlineData("italiano oder espanol")]
    [InlineData("Italienisch oder Französisch")]
    // Ein Sprachname in einem Kurstitel ist kein Wechselwunsch.
    [InlineData("italienisch fuer anfaenger")]
    [InlineData("spanisch lernen")]
    public void ASentenceThatNegatesOrHesitates_IsNotARequest(string transcript)
    {
        var resolver = new LanguageChangeTargetResolver(VoiceInterfaceLanguageCatalog.CreateDefault());

        Assert.False(
            resolver.TryResolve(transcript, "de", out var target, out _),
            $"'{transcript}' wurde als Wunsch nach '{target}' gelesen");
    }

    [Fact]
    public void EveryNameInTheSet_IsMoreThanOneEditFromEveryOther()
    {
        // Der Namensabstand prüft direkte Kollisionen. Er schließt einen gemeinsamen
        // Näherungskandidaten nicht aus; dafür steht der gesonderte Mehrdeutigkeitstest.
        var catalog = VoiceInterfaceLanguageCatalog.CreateDefault();
        var namesByLanguage = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            var language = LanguageCode.ParseIso6391(isoCode);
            var names = new HashSet<string>(StringComparer.Ordinal)
            {
                Normalize(catalog.GetEndonym(language)),
            };

            foreach (var displayIso in VoiceInterfaceModelCatalog.SupportedLanguages)
            {
                names.Add(Normalize(catalog.GetDisplayName(language, LanguageCode.ParseIso6391(displayIso))));
            }

            namesByLanguage[isoCode] = names;
        }

        foreach (var (isoCode, names) in namesByLanguage)
        {
            foreach (var (otherIsoCode, otherNames) in namesByLanguage)
            {
                if (isoCode == otherIsoCode)
                {
                    continue;
                }

                foreach (var name in names.Where(name => name.Length >= 5))
                {
                    foreach (var other in otherNames.Where(other => other.Length >= 5))
                    {
                        Assert.False(
                            IsWithinOneEdit(name, other),
                            $"'{name}' ({isoCode}) and '{other}' ({otherIsoCode}) are within one edit; "
                            + "the near-miss tolerance can no longer tell them apart");
                    }
                }
            }
        }
    }

    [Fact]
    public void WhenTwoLanguagesAreWithinOneEdit_NeitherIsChosen()
    {
        // Slowanisch liegt jeweils eine Änderung von Slowakisch und Slowenisch entfernt,
        // obwohl die Namen zwei Änderungen auseinanderliegen. Der Resolver muss die Mehrdeutigkeit ablehnen.
        var resolver = new LanguageChangeTargetResolver(new CollidingLanguageCatalog());

        Assert.False(
            resolver.TryResolve("Slowanisch", "de", out var target, out _),
            $"a name one edit from two different languages resolved to '{target}'");
    }

    private static string Normalize(string value) => TextNormalizer.NormalizeForPhraseMatching(value);

    /// <summary>
    /// Berechnet den Zeichenabstand unabhängig vom Produktionsvergleich.
    /// Ein Aufruf desselben Produktionshelfers würde nur dessen Übereinstimmung mit sich selbst prüfen.
    /// </summary>
    private static bool IsWithinOneEdit(string left, string right)
    {
        if (Math.Abs(left.Length - right.Length) > 1)
        {
            return false;
        }

        var distance = new int[left.Length + 1, right.Length + 1];
        for (var i = 0; i <= left.Length; i++)
        {
            distance[i, 0] = i;
        }

        for (var j = 0; j <= right.Length; j++)
        {
            distance[0, j] = j;
        }

        for (var i = 1; i <= left.Length; i++)
        {
            for (var j = 1; j <= right.Length; j++)
            {
                var substitution = distance[i - 1, j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                distance[i, j] = Math.Min(substitution, Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1));
            }
        }

        return distance[left.Length, right.Length] <= 1;
    }
}
