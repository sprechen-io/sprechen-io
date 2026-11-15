using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Hält die WASM-Einstiegsklasse aus der Coverage-Messung heraus, damit der
/// Host-Start nicht als Fachlogik zählt.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
}
