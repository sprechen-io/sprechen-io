using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Hält die Host-Einstiegsklasse aus der Coverage-Messung heraus, damit der
/// Prozessstart nicht als Fachlogik zählt.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class Program
{
}
