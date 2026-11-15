using Sprechen.Speech.State;

namespace Sprechen.Speech.State.Tests.DialogExamples;

internal sealed class ExampleDialogReducer : IStateReducer<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>
{
    private const int RequiredCodeDigits = 4;
    private const string EmailExample = "name@beispiel.de";

    private static readonly IReadOnlyDictionary<string, char> DigitWords = new Dictionary<string, char>(StringComparer.OrdinalIgnoreCase)
    {
        ["null"] = '0',
        ["eins"] = '1',
        ["ein"] = '1',
        ["zwei"] = '2',
        ["drei"] = '3',
        ["vier"] = '4',
        ["fünf"] = '5',
        ["fuenf"] = '5',
        ["sechs"] = '6',
        ["sieben"] = '7',
        ["acht"] = '8',
        ["neun"] = '9'
    };

    public StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> Reduce(ExampleDialogState state, ExampleDialogEvent @event)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(@event);

        return @event switch
        {
            ExampleAmbiguityDetectedEvent ambiguityDetectedEvent => ReduceAmbiguityDetected(state, ambiguityDetectedEvent),
            ExampleUserChoiceEvent choiceEvent => ReduceAmbiguityChoice(state, choiceEvent),
            ExampleUserConfirmationEvent confirmationEvent => ReduceConfirmation(state, confirmationEvent),
            ExampleUserCommandEvent commandEvent => ReduceCommand(state, commandEvent),
            ExampleUserSpelledCharEvent spelledCharEvent => ReduceSpelledChar(state, spelledCharEvent),
            ExampleUserTextEvent textEvent => ReduceText(state, textEvent),
            _ => StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state)
        };
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceAmbiguityDetected(
        ExampleDialogState state,
        ExampleAmbiguityDetectedEvent @event)
    {
        var next = state with
        {
            FocusField = @event.Field,
            PendingAmbiguity = new ExampleAmbiguity(@event.Field, @event.OptionOne, @event.OptionTwo),
            PendingSuggestion = null,
            Phase = ExampleDialogPhase.AwaitingAmbiguityChoice,
            IsSpellingMode = false,
            SpellingBuffer = string.Empty
        };

        var prompt = $"Ich habe zwei Möglichkeiten: eins >{@event.OptionOne}<, zwei >{@event.OptionTwo}<. Bitte eins oder zwei.";
        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect(prompt));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceAmbiguityChoice(
        ExampleDialogState state,
        ExampleUserChoiceEvent @event)
    {
        var ambiguity = state.PendingAmbiguity;
        if (ambiguity is null)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var chosen = @event.Option switch
        {
            1 => ambiguity.OptionOne,
            2 => ambiguity.OptionTwo,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(chosen))
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                state,
                new ExamplePromptEffect("Bitte eins oder zwei."));
        }

        chosen = NormalizeFieldValue(ambiguity.Field, chosen);

        var next = state
            .WithValue(ambiguity.Field, chosen)
            with
            {
                FocusField = ambiguity.Field,
                PendingAmbiguity = null,
                Phase = ExampleDialogPhase.AwaitingInput
            };

        var prompt = $"Übernommen: {chosen}.";
        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect(prompt));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceConfirmation(
        ExampleDialogState state,
        ExampleUserConfirmationEvent @event)
    {
        if (state.Phase == ExampleDialogPhase.AwaitingSuggestionConfirmation && state.PendingSuggestion is not null)
        {
            return @event.IsYes
                ? AcceptSuggestion(state, state.PendingSuggestion)
                : RejectSuggestion(state);
        }

        if (state.Phase == ExampleDialogPhase.AwaitingSpellingDecision)
        {
            return @event.IsYes
                ? StartSpelling(state)
                : StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                    state with { Phase = ExampleDialogPhase.AwaitingInput },
                    new ExamplePromptEffect("Ok. Bitte erneut."));
        }

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> AcceptSuggestion(
        ExampleDialogState state,
        ExampleSuggestion suggestion)
    {
        var next = state
            .WithValue(suggestion.Field, suggestion.Value)
            with
            {
                PendingSuggestion = null,
                Phase = ExampleDialogPhase.AwaitingInput
            };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
            next,
            new ExamplePromptEffect($"Übernommen für {GetFieldLabel(suggestion.Field)}: {suggestion.Value}."));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> RejectSuggestion(ExampleDialogState state)
    {
        var next = state with
        {
            PendingSuggestion = null,
            Phase = ExampleDialogPhase.AwaitingInput
        };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect("Ok."));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceCommand(
        ExampleDialogState state,
        ExampleUserCommandEvent @event)
    {
        return @event.Command switch
        {
            ExampleDialogCommand.Change => StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                state,
                new ExamplePromptEffect("Ok. Bitte buchstabieren an.")),
            ExampleDialogCommand.SpellingOn => StartSpelling(state),
            ExampleDialogCommand.SpellingOff => StopSpelling(state),
            ExampleDialogCommand.DeleteLastBlock => DeleteLastBlock(state),
            _ => StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state)
        };
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> StartSpelling(ExampleDialogState state)
    {
        if (state.IsSpellingMode)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var next = state with
        {
            IsSpellingMode = true,
            SpellingBuffer = string.Empty,
            PendingAmbiguity = null,
            PendingSuggestion = null,
            Phase = ExampleDialogPhase.AwaitingInput
        };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect("Buchstabieren an."));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> StopSpelling(ExampleDialogState state)
    {
        if (!state.IsSpellingMode)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var rawValue = state.SpellingBuffer.Trim();
        var normalized = NormalizeFieldValue(state.FocusField, rawValue);

        if (state.FocusField == ExampleFieldId.Email && !IsValidEmail(normalized))
        {
            var invalidState = state with
            {
                IsSpellingMode = false,
                SpellingBuffer = string.Empty,
                Phase = ExampleDialogPhase.AwaitingSpellingDecision
            };

            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                invalidState,
                new ExamplePromptEffect($"E-Mail: ungültiges Format. Beispiel: {EmailExample}. Möchten Sie buchstabieren?"));
        }

        var next = state
            .WithValue(state.FocusField, normalized)
            with
            {
                IsSpellingMode = false,
                SpellingBuffer = string.Empty,
                Phase = ExampleDialogPhase.AwaitingInput
            };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
            next,
            new ExamplePromptEffect($"Übernommen für {GetFieldLabel(state.FocusField)}: {normalized}."));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> DeleteLastBlock(ExampleDialogState state)
    {
        if (state.FocusField != ExampleFieldId.FourDigitCode)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var next = state.WithoutValue(ExampleFieldId.FourDigitCode) with
        {
            Phase = ExampleDialogPhase.AwaitingInput
        };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect("Block gelöscht."));
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceSpelledChar(
        ExampleDialogState state,
        ExampleUserSpelledCharEvent @event)
    {
        if (!state.IsSpellingMode)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var next = state with
        {
            SpellingBuffer = state.SpellingBuffer + @event.Character
        };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(next);
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> ReduceText(
        ExampleDialogState state,
        ExampleUserTextEvent @event)
    {
        if (state.IsSpellingMode)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        var text = @event.Text?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state);
        }

        return state.FocusField switch
        {
            ExampleFieldId.LastName => CommitValue(state, ExampleFieldId.LastName, NormalizeName(text)),
            ExampleFieldId.Street => CommitStreetOrSuggestPostalCity(state, text),
            ExampleFieldId.PostalCity => CommitValue(state, ExampleFieldId.PostalCity, text),
            ExampleFieldId.Email => CommitEmail(state, text),
            ExampleFieldId.FourDigitCode => CommitFourDigitCode(state, text),
            _ => StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithoutEffects(state)
        };
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> CommitStreetOrSuggestPostalCity(
        ExampleDialogState state,
        string input)
    {
        if (!TrySplitStreetAndPostalCity(input, out var street, out var postalCity))
        {
            return CommitValue(state, ExampleFieldId.Street, input);
        }

        var next = state
            .WithValue(ExampleFieldId.Street, street)
            with
            {
                PendingSuggestion = new ExampleSuggestion(ExampleFieldId.PostalCity, postalCity),
                Phase = ExampleDialogPhase.AwaitingSuggestionConfirmation
            };

        var prompt = $"Übernommen für Straße: {street}. Soll ich {postalCity} als PLZ/Ort übernehmen? Ja oder Nein.";
        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(next, new ExamplePromptEffect(prompt));
    }

    private static bool TrySplitStreetAndPostalCity(string input, out string street, out string postalCity)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parts = input.Split(',', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            street = string.Empty;
            postalCity = string.Empty;
            return false;
        }

        street = parts[0].TrimEnd('.', ';');
        postalCity = parts[1].TrimEnd('.', ';');
        return street.Length > 0 && postalCity.Length > 0;
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> CommitEmail(ExampleDialogState state, string email)
    {
        if (!IsValidEmail(email))
        {
            var next = state with
            {
                Phase = ExampleDialogPhase.AwaitingSpellingDecision
            };

            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                next,
                new ExamplePromptEffect($"E-Mail: ungültiges Format. Beispiel: {EmailExample}. Möchten Sie buchstabieren?"));
        }

        return CommitValue(state, ExampleFieldId.Email, email.ToLowerInvariant());
    }

    private static bool IsValidEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex == email.Length - 1)
        {
            return false;
        }

        var dotIndex = email.LastIndexOf('.');
        if (dotIndex <= atIndex + 1 || dotIndex == email.Length - 1)
        {
            return false;
        }

        return true;
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> CommitFourDigitCode(ExampleDialogState state, string input)
    {
        if (!TryParseFourDigitCode(input, out var code))
        {
            return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
                state,
                new ExamplePromptEffect("Code nicht erkannt. Bitte erneut."));
        }

        var next = state
            .WithValue(ExampleFieldId.FourDigitCode, code)
            with
            {
                Phase = ExampleDialogPhase.AwaitingCodeConfirmation
            };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
            next,
            new ExamplePromptEffect($"{FormatCodeForPrompt(code)} -- weiter?"));
    }

    private static bool TryParseFourDigitCode(string input, out string code)
    {
        ArgumentNullException.ThrowIfNull(input);

        var buffer = new List<char>(RequiredCodeDigits);
        var tokens = input.Split([' ', '-', '–', '—', ',', '.', ';', ':'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            if (token.All(char.IsDigit))
            {
                foreach (var digit in token)
                {
                    buffer.Add(digit);
                    if (buffer.Count == RequiredCodeDigits)
                    {
                        code = new string([.. buffer]);
                        return true;
                    }
                }

                continue;
            }

            if (DigitWords.TryGetValue(token, out var mapped))
            {
                buffer.Add(mapped);
                if (buffer.Count == RequiredCodeDigits)
                {
                    code = new string([.. buffer]);
                    return true;
                }
            }
        }

        code = string.Empty;
        return false;
    }

    private static string FormatCodeForPrompt(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var words = code.Select(ToGermanDigitWord).ToArray();
        if (words.Length > 0)
        {
            words[0] = Capitalize(words[0]);
        }

        return string.Join("--", words);
    }

    private static string ToGermanDigitWord(char digit)
    {
        return digit switch
        {
            '0' => "null",
            '1' => "eins",
            '2' => "zwei",
            '3' => "drei",
            '4' => "vier",
            '5' => "fünf",
            '6' => "sechs",
            '7' => "sieben",
            '8' => "acht",
            '9' => "neun",
            _ => digit.ToString()
        };
    }

    private static string Capitalize(string word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return word;
        }

        if (word.Length == 1)
        {
            return word.ToUpperInvariant();
        }

        return char.ToUpperInvariant(word[0]) + word[1..];
    }

    private static StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext> CommitValue(
        ExampleDialogState state,
        ExampleFieldId field,
        string value)
    {
        value = NormalizeFieldValue(field, value);

        var next = state
            .WithValue(field, value)
            with
            {
                FocusField = field,
                Phase = ExampleDialogPhase.AwaitingInput
            };

        return StateUpdate<ExampleDialogState, ExampleDialogEvent, ExampleDialogContext>.WithEffects(
            next,
            new ExamplePromptEffect($"Übernommen für {GetFieldLabel(field)}: {value}."));
    }

    private static string NormalizeFieldValue(ExampleFieldId field, string value)
    {
        value = value.Trim();

        return field switch
        {
            ExampleFieldId.LastName => NormalizeName(value),
            ExampleFieldId.Email => value.ToLowerInvariant(),
            _ => value
        };
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var lower = value.ToLowerInvariant();

        return lower.Length switch
        {
            1 => lower.ToUpperInvariant(),
            _ => char.ToUpperInvariant(lower[0]) + lower[1..]
        };
    }

    private static string GetFieldLabel(ExampleFieldId field)
    {
        return field switch
        {
            ExampleFieldId.LastName => "Nachname",
            ExampleFieldId.Street => "Straße",
            ExampleFieldId.PostalCity => "PLZ/Ort",
            ExampleFieldId.Email => "E-Mail",
            ExampleFieldId.FourDigitCode => "Code",
            _ => field.ToString()
        };
    }
}

