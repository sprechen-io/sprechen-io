namespace Sprechen.Mda.Contracts.Generator;

/// <summary>
/// Beschreibt den aufgelösten C#-Typ einer Schema-Eigenschaft samt Initialisierung und benötigtem Namensraum für die Codeerzeugung.
/// </summary>
internal readonly record struct ResolvedPropertyType(
    string TypeName,
    string? Initializer,
    bool UsesJsonElement,
    string? UsingNamespace);
