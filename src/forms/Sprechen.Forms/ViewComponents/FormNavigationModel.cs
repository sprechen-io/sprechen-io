namespace Sprechen.Forms.ViewComponents;

/// <summary>
/// Enthält Formularfolge, aktuelle Auswahl und Studienangaben
/// für die Darstellung der Formularnavigation.
/// </summary>
public sealed class FormNavigationModel
{

    public string ApplicationId { get; init; } = string.Empty;


    public string CurrentFormId { get; init; } = string.Empty;

    /// <summary>
    /// Anzeigesprache der Oberflächentexte, damit die Komponente das Cookie nicht selbst lesen muss.
    /// </summary>
    public string Language { get; init; } = "de";

    /// <summary>
    /// Pseudonyme Teilnehmerkennung für die Studiennavigation.
    /// </summary>
    public string ParticipantId { get; init; } = string.Empty;


    public string TaskId { get; init; } = string.Empty;

    /// <summary>
    /// Studienbedingung. Für die Forms-Oberfläche ist A vorgesehen.
    /// </summary>
    public string Condition { get; init; } = "A";

    /// <summary>
    /// Geordnete Formular-IDs der Anwendung.
    /// </summary>
    public IReadOnlyList<string> FormSequence { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Nullbasierte Position des Formulars. -1, wenn es nicht in der Folge vorkommt.
    /// </summary>
    public int CurrentIndex => new FormNavigationSequence(FormSequence).FindIndex(CurrentFormId);

    /// <summary>
    /// Einsbasierte Formularnummer für die Anzeige. 0, wenn kein Formular gefunden wurde.
    /// </summary>
    public int CurrentFormNumber => CurrentIndex + 1;


    public int TotalForms => FormSequence.Count;


    public bool HasPrevious => CurrentIndex > 0;


    public bool HasNext => CurrentIndex >= 0 && CurrentIndex < FormSequence.Count - 1;

    /// <summary>
    /// Ob dies das letzte Formular ist, damit Absenden statt Weiter erscheint.
    /// </summary>
    public bool IsLastForm => CurrentIndex >= 0 && CurrentIndex == FormSequence.Count - 1;


    public bool IsFirstForm => CurrentIndex == 0;


    public string? PreviousFormId => HasPrevious ? FormSequence[CurrentIndex - 1] : null;


    public string? NextFormId => HasNext ? FormSequence[CurrentIndex + 1] : null;


    public string BuildFormUrl(string formId)
    {
        return new FormNavigationUrlBuilder().Build(this, formId);
    }


    public string? PreviousFormUrl => PreviousFormId is not null ? BuildFormUrl(PreviousFormId) : null;


    public string? NextFormUrl => NextFormId is not null ? BuildFormUrl(NextFormId) : null;

    /// <summary>
    /// Gerundeter Anteil der aktuellen Formularnummer an der Gesamtzahl in Prozent.
    /// Bei leerer Folge oder fehlender Auswahl ist der Wert 0.
    /// </summary>
    public int ProgressPercent => TotalForms > 0 ? (int)Math.Round(100.0 * CurrentFormNumber / TotalForms) : 0;
}
