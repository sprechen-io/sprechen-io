using System.Text.Json;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht die Identität offener Fehler und die Auswahl einer offenen Regel pro Feld
/// mit einer unabhängig vorgegebenen Referenztabelle. Die Forms-Tests verwenden dieselbe
/// Tabelle, damit beide Kanäle dieselben Zähleinheiten prüfen. Eine aus der Implementierung
/// erzeugte Tabelle würde diesen unabhängigen Vergleich nicht leisten.
/// </summary>
public sealed class FormOpenErrorIdentityTableTests
{
    [Fact]
    public void Identity_GroupsEveryAuthoredCaseTheWayTheTableSays()
    {
        using var table = LoadTable();
        var cases = table.RootElement.GetProperty("cases").EnumerateArray().ToArray();

        Assert.NotEmpty(cases);

        foreach (var authoredCase in cases)
        {
            var caseName = authoredCase.GetProperty("case").GetString()!;
            var keysById = authoredCase.GetProperty("violations").EnumerateArray().ToDictionary(
                violation => violation.GetProperty("id").GetString()!,
                violation => FormOpenErrorResolver.BuildOpenErrorKey(ReadViolation(violation)),
                StringComparer.Ordinal);

            var expectedGroups = authoredCase.GetProperty("expectedOpenErrors").EnumerateArray()
                .Select(group => group.EnumerateArray().Select(id => id.GetString()!).ToArray())
                .ToArray();

            Assert.Equal(
                keysById.Count,
                expectedGroups.Sum(group => group.Length));

            var groupByKey = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (var group in expectedGroups)
            {
                foreach (var id in group)
                {
                    var key = Assert.Contains(id, keysById);
                    if (groupByKey.TryGetValue(key, out var alreadyGrouped))
                    {
                        Assert.True(
                            ReferenceEquals(alreadyGrouped, group),
                            $"'{caseName}': '{id}' and '{alreadyGrouped[0]}' are one open error here, but the table puts them in different groups.");
                        continue;
                    }

                    groupByKey[key] = group;
                }
            }

            Assert.Equal(expectedGroups.Length, groupByKey.Count);
        }
    }

    [Fact]
    public void Table_ExercisesEveryPartOfTheIdentity()
    {
        // Jeder Schlüsselbestandteil braucht einen Fall, in dem nur dieser Bestandteil
        // zwei Fehler unterscheidet. Sonst könnte sein Weglassen unbemerkt bleiben.
        using var table = LoadTable();
        var cases = table.RootElement.GetProperty("cases").EnumerateArray().ToArray();

        foreach (var part in new[] { "scope", "code", "severity", "fieldId", "fieldGroupId" })
        {
            Assert.True(
                cases.Any(authoredCase => SeparatesTwoOpenErrorsByExactly(authoredCase, part)),
                $"No authored case separates two open errors by '{part}' alone, so dropping it from the identity would stay green.");
        }

        Assert.Contains(cases, authoredCase => authoredCase.GetProperty("expectedOpenErrors")
            .EnumerateArray().Any(group => group.GetArrayLength() > 1));
    }

    [Fact]
    public void OpenRuleAtOneField_IsTheOneEveryAuthoredCaseNames()
    {
        using var table = LoadTable();
        var cases = table.RootElement.GetProperty("openRuleAtOneFieldCases").EnumerateArray().ToArray();

        Assert.NotEmpty(cases);

        var precedence = new FormOpenRulePrecedence();

        foreach (var authoredCase in cases)
        {
            var caseName = authoredCase.GetProperty("case").GetString()!;
            var brokenRules = authoredCase.GetProperty("violations").EnumerateArray()
                .Select(ReadViolation)
                .ToArray();
            var expectedConstraintId = Read(
                FindViolation(authoredCase, authoredCase.GetProperty("expectedOpenViolationId").GetString()!),
                "constraintId");

            // Die Auswahl muss auch bei umgekehrter Eingabereihenfolge dieselbe bleiben.
            foreach (var order in new[] { brokenRules, brokenRules.Reverse().ToArray() })
            {
                var open = precedence.SelectOpen(order);

                Assert.True(open is not null, $"'{caseName}': the field breaks rules, so one of them is the open one.");
                Assert.Equal(expectedConstraintId, open!.ConstraintId);
            }
        }
    }

    [Fact]
    public void OpenRuleTable_ExercisesEveryPartOfThePrecedence()
    {
        // Die Fälle müssen den Vorrang des Schweregrads und bei gleichem Schweregrad
        // die Entscheidung anhand der Regelkennung unterscheiden.
        using var table = LoadTable();
        var cases = table.RootElement.GetProperty("openRuleAtOneFieldCases").EnumerateArray().ToArray();

        foreach (var (higher, lower) in new[] { ("error", "warning"), ("warning", "info") })
        {
            Assert.True(
                cases.Any(authoredCase => SeverityOutranksTheRuleInstance(authoredCase, higher, lower)),
                $"No authored case has a '{higher}' rule open while a '{lower}' rule of the same field sorts first, so dropping '{lower}' below '{higher}' would stay green.");
        }

        Assert.True(
            cases.Any(DecidedByTheRuleInstanceAlone),
            "No authored case leaves two rules of equal severity, so dropping the rule-instance tiebreak would stay green.");
    }

    private static bool SeverityOutranksTheRuleInstance(JsonElement authoredCase, string higher, string lower)
    {
        var open = FindViolation(authoredCase, authoredCase.GetProperty("expectedOpenViolationId").GetString()!);
        if (Read(open, "severity") != higher)
        {
            return false;
        }

        return authoredCase.GetProperty("violations").EnumerateArray().Any(other =>
            Read(other, "severity") == lower
            && string.CompareOrdinal(Read(other, "constraintId"), Read(open, "constraintId")) < 0);
    }

    private static bool DecidedByTheRuleInstanceAlone(JsonElement authoredCase)
    {
        var open = FindViolation(authoredCase, authoredCase.GetProperty("expectedOpenViolationId").GetString()!);

        return authoredCase.GetProperty("violations").EnumerateArray().Any(other =>
            Read(other, "id") != Read(open, "id")
            && Read(other, "severity") == Read(open, "severity")
            && string.CompareOrdinal(Read(other, "constraintId"), Read(open, "constraintId")) > 0);
    }

    private static JsonElement FindViolation(JsonElement authoredCase, string id)
        => authoredCase.GetProperty("violations").EnumerateArray()
            .First(violation => violation.GetProperty("id").GetString() == id);

    private static bool SeparatesTwoOpenErrorsByExactly(JsonElement authoredCase, string part)
    {
        var violationsById = authoredCase.GetProperty("violations").EnumerateArray().ToDictionary(
            violation => violation.GetProperty("id").GetString()!,
            violation => violation,
            StringComparer.Ordinal);

        var groups = authoredCase.GetProperty("expectedOpenErrors").EnumerateArray()
            .Select(group => group.EnumerateArray().Select(id => id.GetString()!).ToArray())
            .ToArray();

        for (var left = 0; left < groups.Length; left++)
        {
            for (var right = left + 1; right < groups.Length; right++)
            {
                if (DiffersInExactly(violationsById[groups[left][0]], violationsById[groups[right][0]], part))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool DiffersInExactly(JsonElement left, JsonElement right, string part)
    {
        var parts = IdentityParts(left);
        if (!parts.SequenceEqual(IdentityParts(right)) || !parts.Contains(part))
        {
            // Bei unterschiedlichen Schlüsselbestandteilen lässt sich nicht durch Änderung
            // nur eines gemeinsamen Bestandteils vergleichen.
            return false;
        }

        return parts.All(candidate => (Read(left, candidate) != Read(right, candidate)) == (candidate == part));
    }

    private static string[] IdentityParts(JsonElement violation)
        // Feldbezogene Fehler werden über das Feld unterschieden. Bei anderen Geltungsbereichen
        // gehört die Gruppe zum Schlüssel, nicht das Feld, über das der Fehler erreicht wurde.
        => Read(violation, "scope") == "field"
            ? ["scope", "code", "severity", "fieldId"]
            : ["scope", "code", "severity", "fieldGroupId"];

    private static string Read(JsonElement violation, string property)
        => violation.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    private static FormConstraintViolation ReadViolation(JsonElement violation)
        => new(
            Read(violation, "fieldId"),
            ConstraintId: Read(violation, "constraintId") is { Length: > 0 } constraintId
                ? constraintId
                : Read(violation, "code"),
            Severity: Read(violation, "severity"),
            Code: Read(violation, "code"),
            Message: string.Empty,
            Remediation: null,
            Scope: Read(violation, "scope"),
            FieldGroupId: Read(violation, "fieldGroupId") is { Length: > 0 } groupId ? groupId : null);

    private static JsonDocument LoadTable()
        => ReferenceTableResources.Read("open-error-identity.reference.json");

}
