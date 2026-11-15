using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Bedingungen für vorhandene Werte den Text-, Ganzzahl-, Datums- und Gleichheitsprüfern zu und weist nicht unterstützte Arten zurück.
/// </summary>
internal sealed class FormSetValueConstraintEvaluator(IReadOnlyDictionary<string, string> rawValuesByVariableId)
{
    private readonly FormDateSetValueConstraintEvaluator _dateConstraints = new();
    private readonly FormEqualitySetValueConstraintEvaluator _equalityConstraints = new();
    private readonly FormNumericSetValueConstraintEvaluator _numericConstraints = new();
    private readonly FormTextSetValueConstraintEvaluator _textConstraints = new(
        rawValuesByVariableId ?? throw new ArgumentNullException(nameof(rawValuesByVariableId)));

    private static readonly string[] SupportedConstraintKinds =
    [
        ConstraintKinds.Pattern,
        ConstraintKinds.MinLength,
        ConstraintKinds.MaxLength,
        ConstraintKinds.Min,
        ConstraintKinds.Max,
        ConstraintKinds.Range,
        ConstraintKinds.DateMin,
        ConstraintKinds.DateMax,
        ConstraintKinds.DateRange,
        ConstraintKinds.Equals,
        ConstraintKinds.NotEquals,
        ConstraintKinds.Compare
    ];

    internal bool IsSatisfied(string kind, ConstraintDefinition constraint, string variableId, FormValue value)
    {
        if (_textConstraints.Supports(kind))
        {
            return _textConstraints.IsSatisfied(kind, constraint, variableId);
        }

        if (_numericConstraints.Supports(kind))
        {
            return _numericConstraints.IsSatisfied(kind, constraint, value);
        }

        if (_dateConstraints.Supports(kind))
        {
            return _dateConstraints.IsSatisfied(kind, constraint, value);
        }

        if (_equalityConstraints.Supports(kind))
        {
            return _equalityConstraints.IsSatisfied(kind, constraint, value);
        }

        throw new InvalidOperationException($"Unsupported constraint kind '{kind}'.");
    }

    internal static bool Supports(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
        {
            return false;
        }

        return SupportedConstraintKinds.Contains(kind.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
