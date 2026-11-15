using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Typisierte Formularkennung innerhalb einer Anwendung mit JSON-Darstellung als Zeichenkette.
/// From verlangt eine nicht leere Angabe. Die Umwandlung aus string
/// ist ausdrücklich, die Umwandlung in string implizit.
/// </summary>
[JsonConverter(typeof(FormIdJsonConverter))]
public readonly record struct FormId
{
    private readonly string? _value;

    private FormId(string value) => _value = value;

    public string Value => _value ?? throw new InvalidOperationException("FormId is uninitialized.");

    public static FormId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new FormId(value);
    }

    public static implicit operator string(FormId id) => id.Value;

    public override string ToString() => Value;
}
