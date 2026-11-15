
namespace Sprechen.Speech.App.Contracts.Messages;

/// <summary>
/// Meldet die aktive Klangfamilie, damit Beobachter Änderungen der akustischen Rückmeldung nachvollziehen können.
/// </summary>
public sealed record EarconFamilyChanged(EarconFamily? ActiveFamily);


