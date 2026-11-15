namespace Sprechen.Forms.Pages;

/// <summary>
/// Liefert Überschrift und Begründungstext der Forms-Ablehnungsseite in der Anzeigesprache.
/// </summary>
internal static class FormsStudyLinkRejectionCopy
{
    private static readonly ChromeCopyText HeadingText = new(
        "Ungültiger Studienlink",
        "Invalid study link",
        "Lien d’étude non valide",
        "Enlace del estudio no válido",
        "Link dello studio non valido");

    private static readonly ChromeCopyText TokenMessage = new(
        "Dieser Studienlink ist ungültig, weil das Token nicht stimmt. Bitte prüfen Sie den Link oder wenden Sie sich an die Studienleitung.",
        "This study link is invalid because the token is wrong. Please check the link or contact the study coordinator.",
        "Ce lien d’étude n’est pas valide, car le jeton est incorrect. Veuillez vérifier le lien ou contacter la personne responsable de l’étude.",
        "Este enlace del estudio no es válido porque el token no es correcto. Compruebe el enlace o póngase en contacto con la persona responsable del estudio.",
        "Questo link dello studio non è valido perché il token non è corretto. Controlli il link o contatti la persona responsabile dello studio.");

    private static readonly ChromeCopyText SegmentsMessage = new(
        "Dieser Studienlink ist ungültig, weil er zusätzliche Pfadteile enthält. Bitte prüfen Sie den Link oder wenden Sie sich an die Studienleitung.",
        "This study link is invalid because it contains extra path segments. Please check the link or contact the study coordinator.",
        "Ce lien d’étude n’est pas valide, car il contient des segments de chemin supplémentaires. Veuillez vérifier le lien ou contacter la personne responsable de l’étude.",
        "Este enlace del estudio no es válido porque contiene segmentos de ruta adicionales. Compruebe el enlace o póngase en contacto con la persona responsable del estudio.",
        "Questo link dello studio non è valido perché contiene segmenti di percorso aggiuntivi. Controlli il link o contatti la persona responsabile dello studio.");

    public static string Heading(string? language) => HeadingText.For(language);

    public static string Message(string? language, string? reason)
        => (reason == "segments" ? SegmentsMessage : TokenMessage).For(language);
}
