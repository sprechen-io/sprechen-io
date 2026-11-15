namespace Sprechen.Forms.ViewComponents;

/// <summary>
/// Findet die nullbasierte Position eines Formulars unabhängig von Groß- und Kleinschreibung.
/// Fehlt das Formular in der Folge, ist das Ergebnis -1.
/// </summary>
internal sealed class FormNavigationSequence
{
    private readonly IReadOnlyList<string> _formSequence;

    public FormNavigationSequence(IReadOnlyList<string> formSequence)
    {
        _formSequence = formSequence ?? throw new ArgumentNullException(nameof(formSequence));
    }

    public int FindIndex(string currentFormId)
    {
        for (var i = 0; i < _formSequence.Count; i++)
        {
            if (string.Equals(_formSequence[i], currentFormId, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }
}
