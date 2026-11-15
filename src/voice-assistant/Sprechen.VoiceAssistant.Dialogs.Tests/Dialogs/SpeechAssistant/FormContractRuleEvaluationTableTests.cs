using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht die Validierung der Sprachoberfläche mit einer gemeinsamen Solltabelle für beide Anwendungskanäle.
/// Gleiche Eingaben und Regeln sollen gleich bewertet werden, damit Unterschiede der Laufzeiten
/// den Vergleich der Fehler nicht verfälschen. Geprüft wird der Einstieg in die Regelvalidierung
/// einschließlich Wertumwandlung, nicht nur ein einzelner Regelauswerter.
/// </summary>
public sealed class FormContractRuleEvaluationTableTests
{
    private const string ValueVariableId = "value";

    [Fact]
    public void PatternRule_IsBrokenExactlyWhereTheTableSaysItIs()
    {
        using var table = LoadTable();

        foreach (var authoredCase in table.RootElement.GetProperty("patternRuleCases").EnumerateArray())
        {
            AssertMatchesTable(authoredCase, IsBroken(authoredCase, PatternConstraint(Pattern(authoredCase))));
        }
    }

    [Fact]
    public void LengthRule_IsBrokenExactlyWhereTheTableSaysItIs()
    {
        using var table = LoadTable();

        foreach (var authoredCase in table.RootElement.GetProperty("lengthRuleCases").EnumerateArray())
        {
            foreach (var constraint in LengthConstraints(authoredCase))
            {
                AssertMatchesTable(authoredCase, IsBroken(authoredCase, constraint));
            }
        }
    }

    [Fact]
    public void ChoiceRule_IsBrokenExactlyWhereTheTableSaysItIs()
    {
        using var table = LoadTable();

        foreach (var authoredCase in table.RootElement.GetProperty("choiceRuleCases").EnumerateArray())
        {
            AssertMatchesTable(authoredCase, IsBroken(authoredCase, ChoiceConstraint(authoredCase)));
        }
    }

    [Fact]
    public void BoundedRule_IsBrokenExactlyWhereTheTableSaysItIs()
    {
        using var table = LoadTable();

        // Abweichungen aller Zeilen sammeln, damit Unterschiede bei mehreren Werttypen gemeinsam sichtbar werden.
        var disagreements = new List<string>();
        foreach (var authoredCase in table.RootElement.GetProperty("boundedRuleCases").EnumerateArray())
        {
            CollectDisagreement(disagreements, authoredCase, IsBroken(authoredCase, BoundedConstraint(authoredCase)), "the value the field carries");
        }

        Assert.True(disagreements.Count == 0, string.Join(Environment.NewLine, disagreements));
    }

    [Fact]
    public void BlankField_BreaksExactlyTheRulesTheTableSaysItBreaks()
    {
        using var table = LoadTable();

        // Abweichungen bei leeren Werten gesammelt melden, statt nach dem ersten Fall abzubrechen.
        var disagreements = new List<string>();
        foreach (var authoredCase in table.RootElement.GetProperty("blankValueCases").EnumerateArray())
        {
            var constraint = BlankFieldConstraint(authoredCase);

            // Die Erwartung muss sowohl für einen geleerten als auch für einen nie gesetzten Wert gelten.
            CollectDisagreement(disagreements, authoredCase, IsBroken(authoredCase, constraint, isValueSet: true), "a field that was filled and emptied again");
            CollectDisagreement(disagreements, authoredCase, IsBroken(authoredCase, constraint, isValueSet: false), "a field nobody ever touched");
        }

        Assert.True(disagreements.Count == 0, string.Join(Environment.NewLine, disagreements));
    }

    [Fact]
    public void UnreadableBound_IsReportedAsABrokenContract()
    {
        using var table = LoadTable();

        // Alle nicht gemeldeten ungültigen Grenzen sammeln, damit mehrere betroffene Regelarten sichtbar bleiben.
        var dropped = new List<string>();
        foreach (var authoredCase in table.RootElement.GetProperty("unreadableBoundCases").EnumerateArray())
        {
            try
            {
                var actualBroken = IsBroken(authoredCase, BoundedConstraint(authoredCase));
                dropped.Add(
                    $"'{authoredCase.GetProperty("case").GetString()}': this channel answered the rule {(actualBroken ? "broken" : "satisfied")} instead of reporting the contract.");
            }
            catch (FormConstraintAuthoringException)
            {
            }
        }

        Assert.True(dropped.Count == 0, string.Join(Environment.NewLine, dropped));
    }

    [Fact]
    public void ValueType_StatesExactlyTheRulesTheTableSaysItStates()
    {
        using var table = LoadTable();

        // Fehlende und zusätzliche abgeleitete Regeln sammeln. Abweichende Eigenschaften
        // einer vorhandenen Regel werden unmittelbar geprüft.
        var disagreements = new List<string>();
        foreach (var authoredCase in table.RootElement.GetProperty("valueTypeStatedRuleCases").EnumerateArray())
        {
            var caseName = authoredCase.GetProperty("case").GetString();
            var derivedById = FormValueTypeStatedRules
                .From(VariableWithValueType(authoredCase.GetProperty("valueType")))
                .ToDictionary(rule => rule.ConstraintId, StringComparer.Ordinal);

            foreach (var expected in authoredCase.GetProperty("expectedRules").EnumerateArray())
            {
                var constraintId = expected.GetProperty("constraintId").GetString()!;
                if (!derivedById.Remove(constraintId, out var derived))
                {
                    disagreements.Add($"'{caseName}': the table says this value type states '{constraintId}', this channel states no such rule.");
                    continue;
                }

                var boundName = expected.GetProperty("boundName").GetString()!;
                Assert.True(
                    derived.Code == expected.GetProperty("code").GetString()
                        && derived.Severity.ToWireToken() == expected.GetProperty("severity").GetString()
                        && derived.Kind.ToWireToken() == expected.GetProperty("ruleKind").GetString()
                        && derived.Scope == ConstraintScope.Field
                        && FormConstraintJsonValueReader.TryGetAdditionalProperty(derived, boundName, out var bound)
                        && bound.GetRawText() == expected.GetProperty("bound").GetRawText(),
                    $"'{caseName}': this channel states '{constraintId}' as another rule than the table does.");
            }

            foreach (var surplus in derivedById.Keys)
            {
                disagreements.Add($"'{caseName}': this channel states '{surplus}', a rule the table does not give this value type.");
            }
        }

        Assert.True(disagreements.Count == 0, string.Join(Environment.NewLine, disagreements));
    }

    [Fact]
    public void ValueTypeStatedRule_CarriesAnUnreadableBoundThroughToTheContractReport()
    {
        using var table = LoadTable();

        // Abgeleitete Regeln müssen ihre Grenzen unverändert weiterreichen. Mindestens eine
        // unlesbare Grenze aus der Tabelle muss deshalb als Vertragsfehler gemeldet werden.
        var refused = 0;
        foreach (var authoredCase in table.RootElement.GetProperty("valueTypeStatedRuleCases").EnumerateArray())
        {
            foreach (var derived in FormValueTypeStatedRules.From(VariableWithValueType(authoredCase.GetProperty("valueType"))))
            {
                try
                {
                    IsBroken(WholeNumberCapture, derived);
                }
                catch (FormConstraintAuthoringException)
                {
                    refused++;
                }
            }
        }

        Assert.True(refused > 0, "No authored value type states a bound nobody can read, so a channel that dropped one while deriving would stay green.");
    }

    private static VariableDefinition VariableWithValueType(JsonElement valueType)
        => new()
        {
            VariableId = VariableId.From(ValueVariableId),
            ValueType = valueType.Clone()
        };

    // Die Ableitungsfälle haben keinen eigenen Eingabewert. Für ihre Grenzen wird ein
    // gültiger ganzzahliger Testwert verwendet.
    private static readonly JsonElement WholeNumberCapture =
        JsonSerializer.SerializeToElement(new { value = "20", valueTypeKind = ValueTypeKinds.Integer });

    [Fact]
    public void Table_ExercisesWhatCouldDifferBetweenTheEngines()
    {
        using var table = LoadTable();
        var cases = table.RootElement.GetProperty("patternRuleCases").EnumerateArray().ToArray();

        // Die Tabelle muss mindestens einen Unterschied zwischen Teiltreffer und vollständigem Mustertreffer erkennen.
        Assert.True(
            cases.Any(authoredCase => Disagrees(authoredCase, Pattern(authoredCase))),
            "No authored case separates matching the whole value from searching inside it, so a channel that searched would stay green.");

        // Ohne Gruppierung wird ja|nein durch äußere Anker zu „beginnt mit ja oder endet mit nein“.
        // Ein Tabellenfall muss diese falsche Verankerung erkennen.
        Assert.True(
            cases.Any(authoredCase => Disagrees(authoredCase, $@"\A{Pattern(authoredCase)}\z")),
            "No authored case separates anchoring the pattern as a whole from anchoring it ungrouped, so a channel that dropped the group would stay green.");

        // Die Leerwertfälle müssen die Pflichtregel verletzen, andere Wertregeln hingegen nicht.
        // Leere Zeichenketten und ausschließlich Leerzeichen müssen vertreten sein.
        var blankCases = table.RootElement.GetProperty("blankValueCases").EnumerateArray().ToArray();
        Assert.All(blankCases, authoredCase => Assert.True(
            (authoredCase.GetProperty("ruleKind").GetString() == ConstraintKinds.Required)
                == authoredCase.GetProperty("expectedBroken").GetBoolean(),
            "A blank field breaks the rule that says it may not be blank and no rule about its value; a row that expects otherwise contradicts the model."));
        Assert.True(
            blankCases.Any(authoredCase => authoredCase.GetProperty("value").GetString()!.Length == 0)
                && blankCases.Any(authoredCase => authoredCase.GetProperty("value").GetString() is { Length: > 0 } spaces && spaces.Trim().Length == 0),
            "No authored case separates a field that was never filled from one that carries only spaces, so a channel that answered only the first would stay green.");

        // Auch leere Werte anderer Typen müssen vorkommen; reine Textfälle würden
        // eine fehlerhafte Leerheitsprüfung bei Zahlen nicht erkennen.
        Assert.True(
            blankCases.Any(authoredCase => authoredCase.TryGetProperty("valueTypeKind", out var kind) && kind.GetString() != ValueTypeKinds.String),
            "No authored case holds a field whose value is not text, so a channel that answered emptiness only where the capture is text would stay green.");
        Assert.True(
            blankCases.Any(authoredCase => authoredCase.GetProperty("ruleKind").GetString() == ConstraintKinds.Required),
            "No authored case states the one rule a blank field does break, so a channel that reported nothing at all on an empty field would stay green.");

        // Die Tabelle muss Typgrenzen prüfen, etwa Bruchzahlen in Ganzzahlfeldern
        // und Zeichenketten in Datumsform, die kein gültiges Kalenderdatum sind.
        var boundedCases = table.RootElement.GetProperty("boundedRuleCases").EnumerateArray().ToArray();
        Assert.True(
            boundedCases.Any(authoredCase => authoredCase.GetProperty("expectedBroken").GetBoolean()
                && ALenientReadingWouldSitInsideTheBounds(authoredCase)),
            "No authored case separates reading a capture as the kind the field holds from reading it as any number its text could mean, so a channel that took a fractional capture for a whole number would stay green.");
        Assert.True(
            boundedCases.Any(authoredCase => authoredCase.GetProperty("expectedBroken").GetBoolean()
                && HasTheShapeOfADateWithoutBeingOne(authoredCase)),
            "No authored case separates a date from a capture that only has the shape of one, so a channel that let a day the calendar does not have roll into the next month would stay green.");

        // Numerische Grenzen können als JSON-Zahl oder dezimale Zeichenkette vorliegen.
        // Mindestens ein Fall muss die zweite Darstellung prüfen.
        Assert.True(
            boundedCases.Any(authoredCase => new[] { ConstraintPropertyNames.Min, ConstraintPropertyNames.Max }
                .Any(boundName => authoredCase.TryGetProperty(boundName, out var bound) && bound.ValueKind == JsonValueKind.String)),
            "No authored case states a bound in the decimal-string notation the contract vocabulary allows, so a channel that read only the JSON number notation would stay green.");

        // Ein Testwert außerhalb des modellierten Ganzzahlbereichs deckt unterschiedliche Zahlenbreiten der Laufzeiten auf.
        Assert.True(
            boundedCases.Any(authoredCase => authoredCase.GetProperty("expectedBroken").GetBoolean()
                && IsAWholeNumberBeyondTheModeledWidth(authoredCase.GetProperty("value").GetString()!)),
            "No authored case holds a whole number wider than a field can carry, so a channel that stopped at a different width would stay green.");

        // Auch Regeln mit nur einer Grenze müssen vorkommen; reine Bereichsregeln decken ihre Verarbeitung nicht ab.
        Assert.True(
            boundedCases.Any(authoredCase => StatesItsBoundAlone(authoredCase)),
            "No authored case states a bound on a rule kind that carries that bound alone, so a channel that evaluated only the rule kinds naming both bounds would stay green.");

        // Ungültige Grenzen müssen auch als JSON-Zahl und bei mehreren Regelarten vorkommen.
        // Sonst könnte ein Auswerter nur offensichtlich nicht numerische Grenzen zurückweisen.
        var unreadableBoundCases = table.RootElement.GetProperty("unreadableBoundCases").EnumerateArray().ToArray();
        Assert.True(
            unreadableBoundCases.Any(authoredCase => AuthoredBounds(authoredCase)
                .Any(bound => bound.ValueKind == JsonValueKind.Number)),
            "No authored case states an unreadable bound as a JSON number, so a channel that refused only a bound that is not a number at all would stay green.");
        Assert.True(
            unreadableBoundCases.Select(authoredCase => authoredCase.GetProperty("ruleKind").GetString()).Distinct().Count() > 1,
            "No authored case carries an unreadable bound on more than one rule kind, so a channel that reported one kind and dropped the other would stay green.");

        // Auch eine einzelne ungültige Grenze muss als Vertragsfehler erkennbar sein.
        // Würde sie entfallen, bliebe von dieser Regel keine wirksame Grenze übrig.
        Assert.True(
            unreadableBoundCases.Any(authoredCase => StatesItsBoundAlone(authoredCase)),
            "No authored case carries an unreadable bound on a rule kind that states that bound alone, so a channel that refused one only where a second bound remained would stay green.");

        // Die Ableitung aus Werttypen wird ebenfalls gegen unabhängige Sollregeln geprüft.
        var valueTypeCases = table.RootElement.GetProperty("valueTypeStatedRuleCases").EnumerateArray().ToArray();
        var statedRules = valueTypeCases.SelectMany(authoredCase => authoredCase.GetProperty("expectedRules").EnumerateArray()).ToArray();

        // Eine lesbare Grenze als dezimale Zeichenkette muss auch bei der Regelableitung vorkommen.
        Assert.True(
            statedRules.Any(rule => rule.GetProperty("bound") is { ValueKind: JsonValueKind.String } bound
                && int.TryParse(bound.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _)),
            "No authored value type states a readable bound in the decimal-string notation, so a channel that derived a rule only from the JSON number notation would stay green.");

        // Eine unlesbare Grenze darf bei der Ableitung nicht unbemerkt entfallen.
        Assert.True(
            statedRules.Any(rule => rule.GetProperty("bound") is { ValueKind: JsonValueKind.String } bound
                && !int.TryParse(bound.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _)),
            "No authored value type states a bound nobody can read, so a channel that dropped one while deriving would stay green.");

        // Zwei Grenzen eines Werttyps müssen als getrennte Regeln mit eigenen Kennungen vorkommen.
        // So bleiben die einzelnen Verstöße zuordenbar.
        Assert.True(
            valueTypeCases.Any(authoredCase => authoredCase.GetProperty("expectedRules").GetArrayLength() > 1
                && authoredCase.GetProperty("expectedRules").EnumerateArray()
                    .Select(rule => rule.GetProperty("constraintId").GetString())
                    .Distinct(StringComparer.Ordinal)
                    .Count() > 1),
            "No authored value type states two bounds as two rules with their own identities, so a channel that folded them into one would stay green.");

        // Zusätzliche Eigenschaften ohne definierte Regel dürfen keine erfundene Regel erzeugen.
        Assert.True(
            valueTypeCases.Any(authoredCase => authoredCase.GetProperty("expectedRules").GetArrayLength() == 0
                && authoredCase.GetProperty("valueType").EnumerateObject()
                    .Any(property => property.Name != ValueTypePropertyNames.Kind)),
            "No authored value type carries something that is not a rule of this vocabulary, so a channel that invented a rule kind for it would stay green.");
    }

    // Erkennt Regelarten mit genau einer Grenze.
    private static bool StatesItsBoundAlone(JsonElement authoredCase)
        => BoundNamesByRuleKind.TryGetValue(authoredCase.GetProperty("ruleKind").GetString()!, out var boundNames)
            && boundNames.Length == 1;

    private static IEnumerable<JsonElement> AuthoredBounds(JsonElement authoredCase)
        => new[] { ConstraintPropertyNames.Min, ConstraintPropertyNames.Max, ConstraintPropertyNames.MinDate, ConstraintPropertyNames.MaxDate }
            .Where(boundName => authoredCase.TryGetProperty(boundName, out _))
            .Select(boundName => authoredCase.GetProperty(boundName));

    // Prüft, ob eine großzügige Gleitkommaauslegung des Texts innerhalb der angegebenen Grenzen läge.
    private static bool ALenientReadingWouldSitInsideTheBounds(JsonElement authoredCase)
    {
        if (authoredCase.GetProperty("ruleKind").GetString() != ConstraintKinds.Range
            || !double.TryParse(authoredCase.GetProperty("value").GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var lenientlyRead))
        {
            return false;
        }

        return (!authoredCase.TryGetProperty(ConstraintPropertyNames.Min, out var min) || lenientlyRead >= ReadLeniently(min))
            && (!authoredCase.TryGetProperty(ConstraintPropertyNames.Max, out var max) || lenientlyRead <= ReadLeniently(max));
    }

    // Liest die Grenze absichtlich großzügig als double für die Gegenprobe.
    private static double ReadLeniently(JsonElement bound)
        => bound.ValueKind == JsonValueKind.Number
            ? bound.GetDouble()
            : double.TryParse(bound.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : double.NaN;

    // Erkennt Ganzzahlen innerhalb von Int64, aber außerhalb des modellierten Int32-Bereichs.
    private static bool IsAWholeNumberBeyondTheModeledWidth(string value)
        => long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var asWholeNumber)
            && (asWholeNumber > int.MaxValue || asWholeNumber < int.MinValue);

    private static bool HasTheShapeOfADateWithoutBeingOne(JsonElement authoredCase)
    {
        var value = authoredCase.GetProperty("value").GetString()!;
        return Regex.IsMatch(value, @"^\d{4}-\d{2}-\d{2}$")
            && !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    private static void CollectDisagreement(List<string> disagreements, JsonElement authoredCase, bool actualBroken, string asCaptured)
    {
        var expectedBroken = authoredCase.GetProperty("expectedBroken").GetBoolean();
        if (expectedBroken == actualBroken)
        {
            return;
        }

        disagreements.Add(
            $"'{authoredCase.GetProperty("case").GetString()}' ({asCaptured}): the table says this value {(expectedBroken ? "breaks" : "satisfies")} the rule, this channel says the opposite.");
    }

    private static void AssertMatchesTable(JsonElement authoredCase, bool actualBroken)
    {
        var expectedBroken = authoredCase.GetProperty("expectedBroken").GetBoolean();

        Assert.True(
            expectedBroken == actualBroken,
            $"'{authoredCase.GetProperty("case").GetString()}': the table says this value {(expectedBroken ? "breaks" : "satisfies")} the rule, this channel says the opposite.");
    }

    private static string Pattern(JsonElement authoredCase)
        => authoredCase.GetProperty("pattern").GetString()!;

    // Prüft, ob diese alternative Musterauslegung der Solltabelle widerspricht.
    private static bool Disagrees(JsonElement authoredCase, string asRead)
        => Regex.IsMatch(authoredCase.GetProperty("value").GetString()!, asRead)
            == authoredCase.GetProperty("expectedBroken").GetBoolean();

    private static IEnumerable<ConstraintDefinition> LengthConstraints(JsonElement authoredCase)
    {
        if (authoredCase.TryGetProperty("minLength", out var minLength))
        {
            yield return LengthConstraint(ConstraintKinds.MinLength, ConstraintPropertyNames.MinLength, minLength.GetInt32());
        }

        if (authoredCase.TryGetProperty("maxLength", out var maxLength))
        {
            yield return LengthConstraint(ConstraintKinds.MaxLength, ConstraintPropertyNames.MaxLength, maxLength.GetInt32());
        }
    }

    private static ConstraintDefinition BlankFieldConstraint(JsonElement authoredCase)
    {
        var ruleKind = authoredCase.GetProperty("ruleKind").GetString();
        return ruleKind switch
        {
            ConstraintKinds.Required => BuildConstraint(ConstraintKinds.Required, []),
            ConstraintKinds.Pattern => PatternConstraint(Pattern(authoredCase)),
            ConstraintKinds.MinLength => LengthConstraint(ConstraintKinds.MinLength, ConstraintPropertyNames.MinLength, authoredCase.GetProperty("minLength").GetInt32()),
            ConstraintKinds.MaxLength => LengthConstraint(ConstraintKinds.MaxLength, ConstraintPropertyNames.MaxLength, authoredCase.GetProperty("maxLength").GetInt32()),
            ConstraintKinds.OneOf => ChoiceConstraint(authoredCase),
            ConstraintKinds.Range => BoundedConstraint(authoredCase),
            _ => throw new InvalidOperationException($"The table authored a rule kind this test does not build: '{ruleKind}'.")
        };
    }

    // Die Zuordnung erhält die jeweiligen Grenznamen für ein- und zweiseitige Zahlen- und Datumsregeln.
    private static readonly Dictionary<string, string[]> BoundNamesByRuleKind = new(StringComparer.Ordinal)
    {
        [ConstraintKinds.Min] = [ConstraintPropertyNames.Min],
        [ConstraintKinds.Max] = [ConstraintPropertyNames.Max],
        [ConstraintKinds.Range] = [ConstraintPropertyNames.Min, ConstraintPropertyNames.Max],
        [ConstraintKinds.DateMin] = [ConstraintPropertyNames.MinDate],
        [ConstraintKinds.DateMax] = [ConstraintPropertyNames.MaxDate],
        [ConstraintKinds.DateRange] = [ConstraintPropertyNames.MinDate, ConstraintPropertyNames.MaxDate]
    };

    private static ConstraintDefinition BoundedConstraint(JsonElement authoredCase)
    {
        var ruleKind = authoredCase.GetProperty("ruleKind").GetString()!;
        if (!BoundNamesByRuleKind.TryGetValue(ruleKind, out var boundNames))
        {
            throw new InvalidOperationException($"The table authored a rule kind this test does not build: '{ruleKind}'.");
        }

        return BuildConstraint(
            ruleKind,
            [.. boundNames
                .Where(propertyName => authoredCase.TryGetProperty(propertyName, out _))
                .Select(propertyName => (Name: propertyName, Value: authoredCase.GetProperty(propertyName).Clone()))]);
    }

    private static ConstraintDefinition PatternConstraint(string pattern)
        => BuildConstraint(ConstraintKinds.Pattern, [(ConstraintPropertyNames.Pattern, JsonSerializer.SerializeToElement(pattern))]);

    private static ConstraintDefinition LengthConstraint(string kind, string propertyName, int bound)
        => BuildConstraint(kind, [(propertyName, JsonSerializer.SerializeToElement(bound))]);

    private static ConstraintDefinition ChoiceConstraint(JsonElement authoredCase)
        => BuildConstraint(ConstraintKinds.OneOf, [(ConstraintPropertyNames.Values, authoredCase.GetProperty("values").Clone())]);

    private static ConstraintDefinition BuildConstraint(string kind, (string Name, JsonElement Value)[] properties)
        => new()
        {
            ConstraintId = $"constraint.{kind}",
            Kind = ConstraintKindExtensions.TryFromWireToken(kind, out var parsedKind)
                ? parsedKind
                : throw new InvalidOperationException($"The test built a constraint kind the contract vocabulary does not know: '{kind}'."),
            Target = new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From(ValueVariableId) },
            Severity = ConstraintSeverity.Error,
            Message = new LocalizedText { ["de"] = "Fehler" },
            AdditionalProperties = properties.ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal)
        };

    private static bool IsBroken(JsonElement authoredCase, ConstraintDefinition constraint, bool isValueSet = true)
    {
        var value = authoredCase.GetProperty("value").GetString()!;
        var valueTypeKind = authoredCase.TryGetProperty("valueTypeKind", out var authoredKind)
            ? authoredKind.GetString()!
            : ValueTypeKinds.String;

        // Der Validator erhält den Rohtext und den nach Feldtyp aufgelösten Wert.
        // Scheitert die Umwandlung, bleibt der aufgelöste Wert beim Standardwert.
        var parsedValue = FormValueParsers.TryParseFormValue(value, valueTypeKind, out var parsed) ? parsed : default;
        var validator = new FormConstraintValidator(
            new Dictionary<string, string>(StringComparer.Ordinal) { [ValueVariableId] = value },
            variableId => variableId == ValueVariableId ? parsedValue : null);

        return !validator.IsConstraintSatisfied(
            constraint,
            ValueVariableId,
            parsedValue,
            isValueSet,
            valueTypeKind);
    }

    [Fact]
    public void BlockingRules_AreExactlyTheOnesTheOtherChannelMayCarryInItsSurface()
    {
        using var table = LoadTable();

        // Prüft die blockierenden Regeln anhand ihres Schweregrads. Die laut Tabelle im HTML
        // enthaltenen Regeln müssen zu dieser Menge gehören. Der Test verwendet die Solltabelle,
        // nicht das tatsächlich gerenderte HTML.
        var disagreements = new List<string>();
        foreach (var authoredCase in table.RootElement.GetProperty("enforcedRuleProjectionCases").EnumerateArray())
        {
            var caseName = authoredCase.GetProperty("case").GetString();
            var statedRules = authoredCase.GetProperty("statedRules").EnumerateArray().ToList();
            var violations = statedRules
                .Select(rule => new FormConstraintViolation(
                    "target",
                    rule.GetProperty("constraintId").GetString()!,
                    rule.GetProperty("severity").GetString()!,
                    "form.target.invalid",
                    string.Empty,
                    null,
                    ConstraintScopes.Field,
                    null))
                .ToImmutableArray();

            var blocking = new FormErrorViolationFilter()
                .Filter(violations)
                .Select(violation => violation.ConstraintId)
                .ToHashSet(StringComparer.Ordinal);

            var authoredBlocking = statedRules
                .Where(rule => rule.GetProperty("severity").GetString() == SeverityLevels.Error)
                .Select(rule => rule.GetProperty("constraintId").GetString()!)
                .ToHashSet(StringComparer.Ordinal);

            if (!blocking.SetEquals(authoredBlocking))
            {
                disagreements.Add($"'{caseName}': the table states '{string.Join(", ", authoredBlocking.Order(StringComparer.Ordinal))}' as the blocking rules, this channel blocks on '{string.Join(", ", blocking.Order(StringComparer.Ordinal))}'.");
            }

            foreach (var carried in authoredCase.GetProperty("expectedCarried").EnumerateArray())
            {
                var constraintId = carried.GetProperty("constraintId").GetString()!;
                if (!blocking.Contains(constraintId))
                {
                    disagreements.Add($"'{caseName}': the table has the other channel carry '{constraintId}' in its surface, and this channel does not block on that rule at all.");
                }
            }
        }

        Assert.True(disagreements.Count == 0, string.Join(Environment.NewLine, disagreements));
    }

    private static JsonDocument LoadTable()
        => ReferenceTableResources.Read("contract-rule-evaluation.reference.json");

}
