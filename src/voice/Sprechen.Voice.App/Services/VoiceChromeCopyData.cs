/// <summary>
/// Bündelt die Übersetzungen der Oberflächentexte nach Schlüssel,
/// damit Navigation und Statusanzeigen denselben Katalog verwenden.
/// </summary>
internal static class VoiceChromeCopyData
{
    internal static readonly IReadOnlyDictionary<string, ChromeCopyText> Entries =
        new Dictionary<string, ChromeCopyText>(StringComparer.Ordinal)
        {
        ["GetCloseNavigation"] = new ChromeCopyText("Navigation schließen", "Close navigation", "Fermer la navigation", "Cerrar la navegación", "Chiudi la navigazione"),
        ["GetDiagnosticModeNotice"] = new ChromeCopyText("Diagnosebetrieb. Diese Sitzung wird aufgezeichnet.", "Diagnostic mode. This session is being recorded.", "Mode diagnostic. Cette session est enregistrée.", "Modo de diagnóstico. Esta sesión se está registrando.", "Modalità diagnostica. Questa sessione viene registrata."),
        ["GetHomeTitle"] = new ChromeCopyText("Startseite", "Home", "Accueil", "Inicio", "Pagina iniziale"),
        ["GetInvalidStudyLinkHeading"] = new ChromeCopyText("Ungültiger Studienlink", "Invalid study link", "Lien d’étude non valide", "Enlace del estudio no válido", "Link dello studio non valido"),
        ["GetInvalidStudyLinkMessage"] = new ChromeCopyText("Dieser Studienlink ist ungültig. Bitte prüfen Sie den Link oder wenden Sie sich an die Studienleitung.", "This study link is invalid. Please check the link or contact the study coordinator.", "Ce lien d’étude n’est pas valide. Veuillez vérifier le lien ou contacter la personne responsable de l’étude.", "Este enlace del estudio no es válido. Compruebe el enlace o póngase en contacto con la persona responsable del estudio.", "Questo link dello studio non è valido. Controlli il link o contatti la persona responsabile dello studio."),
        ["GetNotFoundHeading"] = new ChromeCopyText("Nicht gefunden", "Not Found", "Introuvable", "No encontrado", "Non trovato"),
        ["GetNotFoundMessage"] = new ChromeCopyText("Entschuldigung, der gesuchte Inhalt existiert nicht.", "Sorry, the content you are looking for does not exist.", "Désolé, le contenu que vous recherchez n'existe pas.", "Lo sentimos, el contenido que busca no existe.", "Spiacenti, il contenuto che stai cercando non esiste."),
        ["GetLanguageTesterTitle"] = new ChromeCopyText("Sprachtester", "Language Tester", "Testeur de langue", "Probador de idioma", "Tester della lingua"),
        ["GetNavigationLabel"] = new ChromeCopyText("Navigation", "Navigation", "Navigation", "Navegación", "Navigazione"),
        ["GetOpenNavigation"] = new ChromeCopyText("Navigation öffnen", "Open navigation", "Ouvrir la navigation", "Abrir la navegación", "Apri la navigazione"),
        ["GetSettingsNavLabel"] = new ChromeCopyText("Einstellungen", "Settings", "Paramètres", "Configuración", "Impostazioni"),
        ["GetStartControlAccessibleName"] = new ChromeCopyText("Sprachassistent starten - zum Starten aktivieren", "Start voice assistant - activate to begin", "Démarrer l'assistant vocal - activez pour commencer", "Iniciar el asistente de voz - active para empezar", "Avvia l'assistente vocale - attiva per iniziare"),
        ["GetStartControlInstruction"] = new ChromeCopyText("Willkommen. Aktivieren Sie diese Schaltfläche, um den Sprachassistenten zu starten und das Mikrofon freizugeben.", "Welcome. Activate this button to start the voice assistant and allow the microphone.", "Bienvenue. Activez ce bouton pour démarrer l'assistant vocal et autoriser le microphone.", "Bienvenido. Active este botón para iniciar el asistente de voz y permitir el micrófono.", "Benvenuto. Attiva questo pulsante per avviare l'assistente vocale e consentire il microfono."),
        ["GetUnexpectedError"] = new ChromeCopyText("Ein unerwarteter Fehler ist aufgetreten. Bitte laden Sie die Seite neu.", "An unexpected error has occurred. Please reload the page.", "Une erreur inattendue s'est produite. Veuillez recharger la page.", "Se ha producido un error inesperado. Vuelva a cargar la página.", "Si è verificato un errore imprevisto. Ricarica la pagina."),
        };

    internal static string Select(string copyId, string? language)
        => Entries[copyId].For(language);
}
