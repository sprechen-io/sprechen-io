using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.Identifiers;

/// <summary>
/// Typisierte Feldkennung innerhalb eines Formulars mit JSON-Darstellung als Zeichenkette.
/// From verlangt eine nicht leere Angabe. Die Umwandlung aus string
/// ist ausdrücklich, die Umwandlung in string implizit.
/// </summary>
[JsonConverter(typeof(FieldIdJsonConverter))]
public readonly record struct FieldId
{
    private readonly string? _value;

    private FieldId(string value) => _value = value;

    public string Value => _value ?? throw new InvalidOperationException("FieldId is uninitialized.");

    public static FieldId From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new FieldId(value);
    }

    public static implicit operator string(FieldId id) => id.Value;

    public override string ToString() => Value;
}
