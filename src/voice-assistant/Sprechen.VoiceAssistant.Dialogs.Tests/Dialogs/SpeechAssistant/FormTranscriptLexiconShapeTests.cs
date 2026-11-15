using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormTranscriptLexiconShapeTests
{
    private static readonly string LexiconJson = ReadEmbeddedLexicon();

    [Fact]
    public void Lexicon_DeserializesStrictly_WithNoUnmappedMembers()
    {
        var lexicon = DeserializeStrictly();

        Assert.NotNull(lexicon);
    }

    [Fact]
    public void Lexicon_RequiredSections_ArePresentAndNonEmpty()
    {
        var lexicon = DeserializeStrictly();

        Assert.NotEmpty(lexicon.SpellingTokens);
        Assert.NotEmpty(lexicon.SpellingAlphabets);
        Assert.NotEmpty(lexicon.SpokenSpellingFormatByLanguage);
        Assert.NotEmpty(lexicon.UmlautDescriptors);
        Assert.NotEmpty(lexicon.AcuteDescriptors);
        Assert.NotEmpty(lexicon.UmlautByBaseToken);
        Assert.NotEmpty(lexicon.AcuteByBaseToken);
        foreach (var isoCode in new[] { "de", "en", "fr", "es", "it" })
        {
            Assert.True(
                lexicon.FillerTokensByLanguage.TryGetValue(isoCode, out var fillerTokens) && fillerTokens.Length > 0,
                $"fillerTokensByLanguage must carry a non-empty set for '{isoCode}'.");
            Assert.True(
                lexicon.SpellingConnectorsByLanguage.TryGetValue(isoCode, out var spellingConnectors) && spellingConnectors.Length > 0,
                $"spellingConnectorsByLanguage must carry a non-empty set for '{isoCode}'.");
            Assert.True(
                lexicon.DiacriticsConnectorTokensByLanguage.TryGetValue(isoCode, out var diacriticsConnectors) && diacriticsConnectors.Length > 0,
                $"diacriticsConnectorTokensByLanguage must carry a non-empty set for '{isoCode}'.");
        }
        Assert.True(lexicon.NumberWordsByLanguage.ContainsKey("de"));
        Assert.True(lexicon.NumberWordsByLanguage.ContainsKey("en"));
        foreach (var isoCode in new[] { "de", "en" })
        {
            var numberWords = lexicon.NumberWordsByLanguage[isoCode];
            Assert.NotEmpty(numberWords.Units);
            Assert.NotEmpty(numberWords.Teens);
            Assert.NotEmpty(numberWords.Tens);
            Assert.NotEmpty(numberWords.Scales);
        }
    }

    [Fact]
    public void Lexicon_SpokenSpellingFormats_SelectModeledAlphabetsByLocale()
    {
        var lexicon = DeserializeStrictly();

        Assert.Equal("german", lexicon.SpokenSpellingFormatByLanguage["de"].Alphabet);
        foreach (var isoCode in new[] { "en", "fr", "es", "it" })
        {
            Assert.Equal("nato", lexicon.SpokenSpellingFormatByLanguage[isoCode].Alphabet);
        }
    }

    [Fact]
    public void LexiconIndex_RejectsIncompleteOutputAlphabet()
    {
        var lexicon = DeserializeStrictly();
        lexicon.SpellingAlphabets["german"].LetterWords.Remove("z");

        Assert.Throws<InvalidOperationException>(() => new FormTranscriptLexiconIndex(lexicon));
    }

    [Fact]
    public void LexiconIndex_RejectsOutputWordThatDoesNotRoundTripThroughAcceptedInput()
    {
        var lexicon = DeserializeStrictly();
        lexicon.SpellingAlphabets["nato"].LetterWords["a"] = "Berta";

        var exception = Assert.Throws<InvalidOperationException>(() => new FormTranscriptLexiconIndex(lexicon));
        Assert.Contains("does not round-trip", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LexiconIndex_RejectsUnknownAlphabetReferenceOrIncompleteCharacterNames()
    {
        var unknownAlphabet = DeserializeStrictly();
        unknownAlphabet.SpokenSpellingFormatByLanguage["en"] = new FormSpokenSpellingFormatWords
        {
            Alphabet = "unknown",
            Connector = "for",
            UppercaseMarker = "capital",
            CharacterNames = new Dictionary<string, string>
            {
                ["."] = "dot", ["@"] = "at sign", ["-"] = "dash", ["_"] = "underscore",
            },
        };
        Assert.Throws<InvalidOperationException>(() => new FormTranscriptLexiconIndex(unknownAlphabet));

        var missingCharacter = DeserializeStrictly();
        missingCharacter.SpokenSpellingFormatByLanguage["en"].CharacterNames.Remove("@");
        Assert.Throws<InvalidOperationException>(() => new FormTranscriptLexiconIndex(missingCharacter));
    }

    [Fact]
    public void Lexicon_SpokenSpellingOutput_HasTwoCompleteAlphabetsAndFiveFormats()
    {
        using var document = JsonDocument.Parse(LexiconJson);
        var root = document.RootElement;
        var alphabets = root.GetProperty("spellingAlphabets");
        var formats = root.GetProperty("spokenSpellingFormatByLanguage");

        Assert.Equal(
            new[] { "german", "nato" },
            alphabets.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        foreach (var alphabet in alphabets.EnumerateObject())
        {
            var letterWords = alphabet.Value.GetProperty("letterWords");
            Assert.Equal(26, letterWords.EnumerateObject().Count());
            for (var letter = 'a'; letter <= 'z'; letter++)
            {
                Assert.True(letterWords.TryGetProperty(letter.ToString(), out var word));
                Assert.False(string.IsNullOrWhiteSpace(word.GetString()));
            }
        }

        Assert.Equal(
            new[] { "de", "en", "es", "fr", "it" },
            formats.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        foreach (var format in formats.EnumerateObject())
        {
            Assert.True(alphabets.TryGetProperty(format.Value.GetProperty("alphabet").GetString()!, out _));
            Assert.False(string.IsNullOrWhiteSpace(format.Value.GetProperty("connector").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(format.Value.GetProperty("uppercaseMarker").GetString()));
            var characterNames = format.Value.GetProperty("characterNames");
            Assert.Equal(
                new[] { "-", ".", "@", "_" },
                characterNames.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
            Assert.All(characterNames.EnumerateObject(), property => Assert.False(string.IsNullOrWhiteSpace(property.Value.GetString())));
        }
    }

    [Fact]
    public void Lexicon_EverySpellingTokenValue_IsExactlyOneCharacterOrDigit()
    {
        var lexicon = DeserializeStrictly();

        Assert.All(lexicon.SpellingTokens.Values, value => Assert.Equal(1, value.Length));
    }

    [Theory]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public void Lexicon_DigitWordLanguages_HaveAsciiCleanUnitsZeroToNine(string isoCode)
    {
        var lexicon = DeserializeStrictly();

        Assert.True(lexicon.NumberWordsByLanguage.ContainsKey(isoCode));
        var units = lexicon.NumberWordsByLanguage[isoCode].Units;
        Assert.NotEmpty(units);
        Assert.All(units, entry =>
        {
            Assert.True(IsAsciiLower(entry.Key), $"'{entry.Key}' must be ASCII-folded lowercase.");
            Assert.InRange(entry.Value, 0, 9);
        });
        for (var digit = 0; digit <= 9; digit++)
        {
            Assert.Contains(digit, units.Values);
        }
    }

    private static bool IsAsciiLower(string value)
        => value.Length > 0 && value.All(c => (c is >= 'a' and <= 'z') || (c is >= '0' and <= '9'));

    [Fact]
    public void Lexicon_SpokenOutputConnectors_ArePresentInTheInputConnectorTables()
    {
        var lexicon = DeserializeStrictly();

        foreach (var isoCode in new[] { "de", "en", "fr", "es", "it" })
        {
            var connector = lexicon.SpokenSpellingFormatByLanguage[isoCode].Connector;
            Assert.Contains(connector, lexicon.SpellingConnectorsByLanguage[isoCode]);
        }
    }

    [Fact]
    public void Lexicon_SpokenOutputUppercaseMarkers_ArePresentInTheInputMarkerTables()
    {
        var lexicon = DeserializeStrictly();

        foreach (var isoCode in new[] { "de", "en", "fr", "es", "it" })
        {
            var foldedMarker = TextNormalizer.NormalizeForPhraseMatching(
                lexicon.SpokenSpellingFormatByLanguage[isoCode].UppercaseMarker);
            Assert.Contains(
                foldedMarker,
                lexicon.UppercaseMarkersByLanguage[isoCode].Select(word => TextNormalizer.NormalizeForPhraseMatching(word)));
        }
    }

    [Fact]
    public void Lexicon_GermanDateInputVocabulary_IsCompleteAndSelfNormalized()
    {
        var lexicon = DeserializeStrictly();

        Assert.True(
            lexicon.DateInputVocabularyByLanguage.TryGetValue("de", out var german),
            "dateInputVocabularyByLanguage must carry the German date-input vocabulary.");

        Assert.Equal(-2, german.RelativeDayOffsets["vorgestern"]);
        Assert.Equal(-1, german.RelativeDayOffsets["gestern"]);
        Assert.Equal(0, german.RelativeDayOffsets["heute"]);
        Assert.Equal(1, german.RelativeDayOffsets["morgen"]);
        Assert.Equal(2, german.RelativeDayOffsets["übermorgen"]);
        Assert.Equal(2, german.RelativeDayOffsets["uebermorgen"]);

        Assert.Equal(new[] { "am", "der", "den", "die" }, german.LeadingParticles);
        Assert.Equal(new[] { "er", "en", "e" }, german.OrdinalEndings);

        // Für jeden Tag von 1 bis 31 ist ein Stamm vorhanden; die geprüften ß-Formen haben auch ss-Varianten.
        Assert.Equal(34, german.OrdinalDayStems.Count);
        for (var day = 1; day <= 31; day++)
        {
            Assert.Contains(day, german.OrdinalDayStems.Values);
        }
        Assert.Equal(5, german.OrdinalDayStems["fünft"]);
        Assert.Equal(1, german.OrdinalDayStems["erst"]);
        Assert.Equal(20, german.OrdinalDayStems["zwanzigst"]);
        Assert.Equal(30, german.OrdinalDayStems["dreissigst"]);
        Assert.Equal(30, german.OrdinalDayStems["dreißigst"]);
        Assert.Equal(31, german.OrdinalDayStems["einunddreissigst"]);
        Assert.Equal(31, german.OrdinalDayStems["einunddreißigst"]);

        // Die Schlüssel behalten die sprachübliche Schreibweise, weil sie auch als
        // Erkennungshinweise dienen. Der Index leitet normalisierte Suchschlüssel beim
        // Laden ab; entfernte Diakritika lassen sich nicht zuverlässig zurückgewinnen.
        // Deshalb müssen die Schlüssel erst nach dieser Normalisierung eindeutig sein.
        var germanKeys = german.RelativeDayOffsets.Keys.Concat(german.OrdinalDayStems.Keys).ToArray();
        var normalizedKeys = germanKeys
            .Select(TextNormalizer.NormalizeForPhraseMatching)
            .ToArray();
        Assert.Equal(normalizedKeys.Length, normalizedKeys.Distinct(StringComparer.Ordinal).Count());

        // Partikeln und Endungen werden im normalisierten Transkript entfernt
        // und liegen deshalb bereits normalisiert vor.
        Assert.All(
            german.LeadingParticles.Concat(german.OrdinalEndings),
            key => Assert.Equal(key, TextNormalizer.NormalizeForPhraseMatching(key)));
    }

    [Fact]
    public void Lexicon_GermanTimeInputKeywords_ArePresent()
    {
        var lexicon = DeserializeStrictly();

        Assert.True(
            lexicon.TimeInputKeywordsByLanguage.TryGetValue("de", out var german),
            "timeInputKeywordsByLanguage must carry the German time-input keywords.");
        Assert.Equal("halb", german.HalfHour);
        Assert.Equal("viertel", german.Quarter);
        Assert.Equal("vor", german.Before);
        Assert.Equal("nach", german.After);
        Assert.Equal("uhr", german.ClockMarker);
    }

    [Fact]
    public void Lexicon_NumberGrammar_DeAndEn_ArePresent()
    {
        var lexicon = DeserializeStrictly();

        Assert.True(
            lexicon.NumberGrammarByLanguage.TryGetValue("de", out var german),
            "numberGrammarByLanguage must carry the German number grammar.");
        Assert.Equal("und", german.AndConnector);
        Assert.Equal(new[] { "dutzend", "dutzende" }, german.UnsupportedMagnitudeWords);
        Assert.Equal(new[] { "minus", "negativ" }, german.SignWords);

        Assert.True(
            lexicon.NumberGrammarByLanguage.TryGetValue("en", out var english),
            "numberGrammarByLanguage must carry the English number grammar.");
        Assert.Equal(new[] { "minus", "negative" }, english.SignWords);

        // Bausteine der Jahresausgabe: gebundenes „ein“ statt freiem „eins“,
        // Hunderter- und Tausenderwörter sowie englisches „oh“.
        Assert.Equal("ein", german.BoundOnePrefix);
        Assert.Equal("hundert", german.HundredWord);
        Assert.Equal("tausend", german.ThousandWord);
        Assert.Equal("oh", english.OhWord);
        Assert.Equal("hundred", english.HundredWord);
        Assert.Equal("thousand", english.ThousandWord);

        // Die Ausgabebausteine müssen denselben Zahlenwerten in den Eingabetabellen entsprechen.
        Assert.Equal(100, lexicon.NumberWordsByLanguage["de"].Scales[german.HundredWord!]);
        Assert.Equal(1000, lexicon.NumberWordsByLanguage["de"].Scales[german.ThousandWord!]);
        Assert.Equal(1, lexicon.NumberWordsByLanguage["de"].Units[german.BoundOnePrefix!]);
        Assert.Equal(100, lexicon.NumberWordsByLanguage["en"].Scales[english.HundredWord!]);
        Assert.Equal(1000, lexicon.NumberWordsByLanguage["en"].Scales[english.ThousandWord!]);

        // Die Zahlengrammatik ist hier auf Deutsch und Englisch begrenzt.
        Assert.Equal(
            new[] { "de", "en" },
            lexicon.NumberGrammarByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Lexicon_SpokenOrdinalDays_GermanAndFrenchDayOne_AreComplete()
    {
        var lexicon = DeserializeStrictly();

        // Für die deutsche Ausgabe stehen vollständige Ordinalwörter für die Tage 1 bis 31
        // in sprachüblicher Schreibweise, getrennt von Eingabestämmen und Endungen.
        Assert.True(
            lexicon.SpokenOrdinalDayByLanguage.TryGetValue("de", out var german),
            "spokenOrdinalDayByLanguage must carry the German written ordinal days.");
        Assert.Equal(31, german.Count);
        for (var day = 1; day <= 31; day++)
        {
            Assert.Contains(day, german.Keys);
        }
        Assert.Equal("erster", german[1]);
        Assert.Equal("fünfter", german[5]);
        Assert.Equal("zwölfter", german[12]);
        Assert.Equal("zwanzigster", german[20]);
        Assert.Equal("dreißigster", german[30]);
        Assert.Equal("einunddreißigster", german[31]);

        // Die französische Ausgabetabelle enthält nur die besondere Form 1er für den ersten Tag.
        Assert.True(
            lexicon.SpokenOrdinalDayByLanguage.TryGetValue("fr", out var french),
            "spokenOrdinalDayByLanguage must carry the French first-day form.");
        Assert.Equal(new Dictionary<int, string> { [1] = "1er" }, french);

        // Für Englisch, Spanisch und Italienisch sind hier keine eigenen Ordinalausgabe-Einträge vorgesehen.
        Assert.Equal(
            new[] { "de", "fr" },
            lexicon.SpokenOrdinalDayByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Lexicon_SpokenDateConnector_IsSpanishOnly()
    {
        var lexicon = DeserializeStrictly();

        // Nur die spanische Datumsausgabe hat in dieser Tabelle das Verbindungswort de.
        Assert.True(
            lexicon.SpokenDateConnectorByLanguage.TryGetValue("es", out var connector),
            "spokenDateConnectorByLanguage must carry the Spanish date connector.");
        Assert.Equal("de", connector);
        Assert.Equal(
            new[] { "es" },
            lexicon.SpokenDateConnectorByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Lexicon_SpokenTimeFormat_AllLanguages_AreCompleteByRole()
    {
        var lexicon = DeserializeStrictly();

        // Alle fünf vorgesehenen Sprachen benötigen Einträge für die Zeitausgabe.
        Assert.Equal(
            new[] { "de", "en", "es", "fr", "it" },
            lexicon.SpokenTimeFormatByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));

        var german = lexicon.SpokenTimeFormatByLanguage["de"];
        Assert.Equal("Uhr", german.ClockMarker);
        Assert.Null(german.AnteMeridiem);
        Assert.Null(german.MinuteConnector);

        var english = lexicon.SpokenTimeFormatByLanguage["en"];
        Assert.Equal("AM", english.AnteMeridiem);
        Assert.Equal("PM", english.PostMeridiem);
        Assert.Null(english.ClockMarker);

        var french = lexicon.SpokenTimeFormatByLanguage["fr"];
        Assert.Equal("heure", french.HourWordSingular);
        Assert.Equal("heures", french.HourWordPlural);
        Assert.Null(french.MinuteConnector);

        var spanish = lexicon.SpokenTimeFormatByLanguage["es"];
        Assert.Equal("la", spanish.ArticleSingular);
        Assert.Equal("las", spanish.ArticlePlural);
        Assert.Equal("y", spanish.MinuteConnector);
        Assert.Null(spanish.OneOClockForm);

        var italian = lexicon.SpokenTimeFormatByLanguage["it"];
        Assert.Equal("le", italian.ArticlePlural);
        Assert.Equal("l'una", italian.OneOClockForm);
        Assert.Equal("e", italian.MinuteConnector);
        Assert.Null(italian.ArticleSingular);
    }

    [Fact]
    public void Lexicon_DateInputParticles_EnAndRomance_ArePresent()
    {
        var lexicon = DeserializeStrictly();


        Assert.True(
            lexicon.DateInputParticlesByLanguage.TryGetValue("en", out var english),
            "dateInputParticlesByLanguage must carry the English date-input fillers.");
        Assert.Equal(new[] { "the", "of", "on", "in" }, english);

        // Der gemeinsame Parser für Französisch, Spanisch und Italienisch verwendet
        // dieselbe Partikelmenge; die drei Einträge müssen deshalb übereinstimmen.
        var romanceUnion = new[]
        {
            "le", "la", "l", "el", "lo", "los", "las", "il", "i", "gli", "the", "d", "de", "del", "di", "du", "of",
        };
        foreach (var isoCode in new[] { "fr", "es", "it" })
        {
            Assert.True(
                lexicon.DateInputParticlesByLanguage.TryGetValue(isoCode, out var particles),
                $"dateInputParticlesByLanguage must carry the shared romance particle union for '{isoCode}'.");
            Assert.Equal(romanceUnion, particles);
        }

        // Deutsche Anfangspartikeln stehen stattdessen in dateInputVocabularyByLanguage.leadingParticles.
        Assert.False(lexicon.DateInputParticlesByLanguage.ContainsKey("de"));
        Assert.Equal(
            new[] { "en", "es", "fr", "it" },
            lexicon.DateInputParticlesByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    [Fact]
    public void Lexicon_OrdinalDays_EnAndRomance_AreCompleteAndSelfNormalized()
    {
        var lexicon = DeserializeStrictly();

        // Die englische Tabelle enthält Ordinalwörter für 1 bis 19 sowie 20 und 30.
        Assert.True(
            lexicon.OrdinalDaysByLanguage.TryGetValue("en", out var english),
            "ordinalDaysByLanguage must carry the English ordinal-day words.");
        Assert.Equal(21, english.Days.Count);
        for (var day = 1; day <= 20; day++)
        {
            Assert.Contains(day, english.Days.Values);
        }
        Assert.Contains(30, english.Days.Values);
        Assert.Equal(1, english.Days["first"]);
        Assert.Equal(8, english.Days["eighth"]);
        Assert.Equal(20, english.Days["twentieth"]);
        Assert.Equal(30, english.Days["thirtieth"]);

        // Die Ergänzungstabelle enthält Ordnungszahlwörter für die Einerstelle in zusammengesetzten Formen wie twenty first.
        Assert.Equal(9, english.CompoundOnes.Count);
        for (var ones = 1; ones <= 9; ones++)
        {
            Assert.Contains(ones, english.CompoundOnes.Values);
        }
        Assert.Equal(1, english.CompoundOnes["first"]);
        Assert.Equal(9, english.CompoundOnes["ninth"]);

        // Französisch, Spanisch und Italienisch teilen die Formen für den ersten Tag im gemeinsamen Parser.
        var firstDayUnion = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["premier"] = 1, ["premiere"] = 1, ["primero"] = 1, ["primer"] = 1, ["primo"] = 1,
        };
        foreach (var isoCode in new[] { "fr", "es", "it" })
        {
            Assert.True(
                lexicon.OrdinalDaysByLanguage.TryGetValue(isoCode, out var romance),
                $"ordinalDaysByLanguage must carry the shared romance first-day words for '{isoCode}'.");
            Assert.Equal(firstDayUnion, romance.Days);
            Assert.Empty(romance.CompoundOnes);
        }

        // Deutsche Ordinaltage werden aus Stamm und Endung in dateInputVocabularyByLanguage zusammengesetzt.
        Assert.False(lexicon.OrdinalDaysByLanguage.ContainsKey("de"));
        Assert.Equal(
            new[] { "en", "es", "fr", "it" },
            lexicon.OrdinalDaysByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));

        // Diese Wortschlüssel müssen bereits in ihrer normalisierten Form gespeichert sein.
        Assert.All(
            lexicon.OrdinalDaysByLanguage.Values.SelectMany(vocabulary => vocabulary.Days.Keys)
                .Concat(english.CompoundOnes.Keys)
                .Concat(lexicon.DateInputParticlesByLanguage.Values.SelectMany(particles => particles)),
            key => Assert.Equal(key, TextNormalizer.NormalizeForPhraseMatching(key)));
    }
    [Fact]
    public void Lexicon_CommandPolitePaddingTokens_DeAndEn_AreExactAndSelfNormalized()
    {
        var lexicon = DeserializeStrictly();

        // Diese Befehlszusätze mildern Aufforderungen ab und sind getrennt von allgemeinen
        // Füllwörtern erfasst. Die erwarteten Einträge sind auf Deutsch und Englisch begrenzt.
        Assert.Equal(
            new[] { "de", "en" },
            lexicon.CommandPolitePaddingTokensByLanguage.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            new[] { "bitte", "mal", "doch", "einmal", "kurz" },
            lexicon.CommandPolitePaddingTokensByLanguage["de"]);
        Assert.Equal(
            new[] { "please", "kindly", "just" },
            lexicon.CommandPolitePaddingTokensByLanguage["en"]);

        // Die gespeicherten Befehlszusätze müssen bereits normalisiert sein.
        Assert.All(
            lexicon.CommandPolitePaddingTokensByLanguage.Values.SelectMany(tokens => tokens),
            token => Assert.Equal(token, TextNormalizer.NormalizeForPhraseMatching(token)));
    }

    [Fact]
    public void LexiconIndex_CommandPolitePaddingTokens_UnionSpansEveryLanguageEntry()
    {
        var index = new FormTranscriptLexiconIndex(DeserializeStrictly());
        var union = index.GetCommandPolitePaddingTokens();

        // Der Index liefert die gemeinsame Menge aus den Spracheinträgen.
        Assert.Equal(
            new[] { "bitte", "doch", "einmal", "just", "kindly", "kurz", "mal", "please" },
            union.OrderBy(token => token, StringComparer.Ordinal));
    }


    [Theory]
    [InlineData("yesNoByLanguage")]
    [InlineData("choiceNegationTokens")]
    [InlineData("choiceFillerTokens")]
    [InlineData("spellingNoiseTokens")]
    [InlineData("spellingConnectors")]
    [InlineData("diacriticsConnectorTokens")]
    [InlineData("digitModeAliasesByLanguage")]
    public void Lexicon_DoesNotContainRemovedSection(string removedKey)
    {
        using var document = JsonDocument.Parse(LexiconJson);

        Assert.False(
            document.RootElement.TryGetProperty(removedKey, out _),
            $"The lexicon must not contain the '{removedKey}' section.");
    }

    private static FormTranscriptLexicon DeserializeStrictly()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };

        return JsonSerializer.Deserialize<FormTranscriptLexicon>(LexiconJson, options)
            ?? throw new InvalidOperationException("The embedded transcript lexicon could not be deserialized.");
    }

    private static string ReadEmbeddedLexicon()
    {
        var assembly = typeof(EmbeddedFormTranscriptLexiconProvider).Assembly;
        using var stream = assembly.GetManifestResourceStream(FormTranscriptLexiconResourceNames.Default)
            ?? throw new InvalidOperationException("The embedded transcript lexicon resource is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
