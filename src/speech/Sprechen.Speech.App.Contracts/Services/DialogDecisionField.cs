namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Benannter Wert einer <see cref="DialogDecision"/>, unabhängig von ihrer Textdarstellung.
/// </summary>
public readonly record struct DialogDecisionField(string Name, string Value);
