using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Speech.TestDoubles;

public static class FormsCatalogIntentTestCatalog
{
    public static FormsCatalog Create()
    {
        return new FormsCatalog
        {
            SchemaVersion = "1",
            CatalogVersion = "1",
            SelectionDialog = CreateSelectionDialog(),
            Applications =
            [
                new ApplicationDescriptor
                {
                    ApplicationId = "sample.catalog",
                    DisplayName = CreateText(("de", "Katalog"), ("en", "Catalog"), ("fr", "Catalogue"), ("es", "Catalogo"), ("it", "Catalogo")),
                    Description = CreateText(("de", "Katalog"), ("en", "Catalog"), ("fr", "Catalogue"), ("es", "Catalogo"), ("it", "Catalogo")),
                    SupportedLanguages = ["de", "en", "fr", "es", "it"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("sample-form"),
                            DisplayName = CreateText(("de", "Formular"), ("en", "Form"), ("fr", "Formulaire"), ("es", "Formulario"), ("it", "Modulo")),
                            Description = CreateText(("de", "Formular"), ("en", "Form"), ("fr", "Formulaire"), ("es", "Formulario"), ("it", "Modulo")),
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                }
            ]
        };
    }

    private static ApplicationSelectionDialog CreateSelectionDialog()
    {
        var confirmYes = CatalogVoiceIntentId.ConfirmYes;
        var confirmNo = CatalogVoiceIntentId.ConfirmNo;
        var cancel = CatalogVoiceIntentId.Cancel;
        var changeLanguage = CatalogVoiceIntentId.ChangeLanguage;
        var repeat = CatalogVoiceIntentId.Repeat;
        var help = CatalogVoiceIntentId.Help;
        var list = CatalogVoiceIntentId.List;
        var selectByNumber = CatalogVoiceIntentId.SelectByNumber;
        var search = CatalogVoiceIntentId.Search;
        var describe = CatalogVoiceIntentId.Describe;
        var phrase = CatalogVoiceIntentMatchMode.Phrase;
        var prefix = CatalogVoiceIntentMatchMode.Prefix;

        return new ApplicationSelectionDialog
        {
            SchemaVersion = "1",
            ModelVersion = "1",
            SelectionDialogId = "default.selectionDialog",
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            Intents =
            [
                CreateIntent(confirmYes, phrase, ("de", "ja"), ("en", "yes"), ("fr", "oui"), ("es", "si"), ("it", "si")),
                CreateIntent(confirmYes, phrase, ("de", "jawohl"), ("en", "yeah"), ("fr", "oui bien sur"), ("es", "claro"), ("it", "certo")),
                CreateIntent(confirmYes, phrase, ("de", "genau"), ("en", "correct"), ("fr", "exact"), ("es", "correcto"), ("it", "corretto")),
                CreateIntent(confirmYes, phrase, ("de", "richtig"), ("en", "right"), ("fr", "correct"), ("es", "es correcto"), ("it", "giusto")),
                CreateIntent(confirmYes, phrase, ("de", "korrekt"), ("en", "sure"), ("fr", "bien sur"), ("es", "claro que si"), ("it", "certamente")),
                CreateIntent(confirmYes, phrase, ("de", "ok"), ("en", "ok"), ("fr", "ok"), ("es", "ok"), ("it", "ok")),
                CreateIntent(confirmYes, phrase, ("de", "okay"), ("en", "okay"), ("fr", "daccord"), ("es", "vale"), ("it", "okay")),
                CreateIntent(confirmNo, phrase, ("de", "nein"), ("en", "no"), ("fr", "non"), ("es", "no"), ("it", "no")),
                CreateIntent(confirmNo, phrase, ("de", "ne"), ("en", "nope"), ("fr", "non merci"), ("es", "para nada"), ("it", "nope")),
                CreateIntent(confirmNo, phrase, ("de", "falsch"), ("en", "wrong"), ("fr", "faux"), ("es", "incorrecto"), ("it", "sbagliato")),
                CreateIntent(confirmNo, phrase, ("de", "nicht richtig"), ("en", "incorrect"), ("fr", "pas correct"), ("es", "no es correcto"), ("it", "non corretto")),
                CreateIntent(cancel, phrase, ("de", "abbrechen"), ("en", "cancel"), ("fr", "annuler"), ("es", "cancelar"), ("it", "annulla")),
                CreateIntent(cancel, phrase, ("de", "zurueck"), ("en", "back"), ("fr", "retour"), ("es", "atras"), ("it", "indietro")),
                CreateIntent(cancel, phrase, ("de", "beenden"), ("en", "quit"), ("fr", "quitter"), ("es", "terminar"), ("it", "esci")),
                CreateIntent(cancel, phrase, ("de", "stopp"), ("en", "stop"), ("fr", "stop"), ("es", "stop"), ("it", "stop")),
                CreateIntent(changeLanguage, phrase, ("de", "sprache wechseln"), ("en", "change language"), ("fr", "changer de langue"), ("es", "cambiar idioma"), ("it", "cambia lingua")),
                CreateIntent(changeLanguage, phrase, ("de", "sprache andern"), ("en", "switch language"), ("fr", "changer la langue"), ("es", "cambiar lengua"), ("it", "cambiare lingua")),
                CreateIntent(changeLanguage, prefix, ("de", "sprache wechseln"), ("en", "change language"), ("fr", "changer de langue"), ("es", "cambiar idioma"), ("it", "cambia lingua")),
                CreateIntent(changeLanguage, prefix, ("de", "sprache andern"), ("en", "switch language"), ("fr", "changer la langue"), ("es", "cambiar lengua"), ("it", "cambiare lingua")),
                CreateIntent(repeat, phrase, ("de", "wiederholen"), ("en", "repeat"), ("fr", "repeter"), ("es", "repetir"), ("it", "ripeti")),
                CreateIntent(repeat, phrase, ("de", "nochmal"), ("en", "again"), ("fr", "encore"), ("es", "otra vez"), ("it", "di nuovo")),
                CreateIntent(repeat, phrase, ("de", "wie bitte"), ("en", "pardon"), ("fr", "comment"), ("es", "como dices"), ("it", "come hai detto")),
                CreateIntent(repeat, phrase, ("de", "wiederhole"), ("en", "repeat that"), ("fr", "repete"), ("es", "repite"), ("it", "ripetilo")),
                CreateIntent(repeat, phrase, ("de", "noch mal"), ("en", "once more"), ("fr", "encore une fois"), ("es", "una vez mas"), ("it", "ancora una volta")),
                CreateIntent(repeat, phrase, ("de", "wie war das"), ("en", "what was that"), ("fr", "cetait quoi"), ("es", "que era eso"), ("it", "comera")),
                CreateIntent(help, phrase, ("de", "hilfe"), ("en", "help"), ("fr", "aide"), ("es", "ayuda"), ("it", "aiuto")),
                CreateIntent(help, phrase, ("de", "was kann ich"), ("en", "what can i"), ("fr", "que puis je dire"), ("es", "que puedo decir"), ("it", "cosa posso dire")),
                CreateIntent(help, phrase, ("de", "optionen"), ("en", "options"), ("fr", "options"), ("es", "opciones"), ("it", "opzioni")),
                CreateIntent(list, phrase, ("de", "alle anwendungen"), ("en", "all applications"), ("fr", "toutes les applications"), ("es", "todas las aplicaciones"), ("it", "tutte le applicazioni")),
                CreateIntent(list, phrase, ("de", "was gibt es"), ("en", "what is available"), ("fr", "quest ce qui est disponible"), ("es", "que hay disponible"), ("it", "cosa e disponibile")),
                CreateIntent(list, phrase, ("de", "zeige mir"), ("en", "show me"), ("fr", "montre moi"), ("es", "muestrame"), ("it", "mostrami")),
                CreateIntent(list, phrase, ("de", "liste"), ("en", "list"), ("fr", "liste"), ("es", "lista"), ("it", "lista")),
                CreateIntent(list, phrase, ("de", "welche anwendungen gibt es"), ("en", "which applications are there"), ("fr", "quelles applications existent"), ("es", "que aplicaciones hay"), ("it", "quali applicazioni ci sono")),
                CreateIntent(list, phrase, ("de", "uebersicht"), ("en", "overview"), ("fr", "vue densemble"), ("es", "vision general"), ("it", "panoramica")),
                CreateIntent(list, phrase, ("de", "welche anwendung gibt es"), ("en", "which application is there"), ("fr", "quelle application existe"), ("es", "que aplicacion hay"), ("it", "quale applicazione c e")),
                CreateIntent(list, phrase, ("de", "die anwendungen"), ("en", "the applications"), ("fr", "les applications"), ("es", "las aplicaciones"), ("it", "le applicazioni")),
                CreateIntent(list, phrase, ("de", "die anwendung"), ("en", "the app"), ("fr", "lapplication"), ("es", "la aplicacion"), ("it", "lapplicazione")),
                CreateIntent(selectByNumber, prefix, ("de", "nummer"), ("en", "number"), ("fr", "numero"), ("es", "numero"), ("it", "numero")),
                CreateIntent(search, prefix, ("de", "suche nach"), ("en", "search for"), ("fr", "rechercher"), ("es", "buscar"), ("it", "cerca")),
                CreateIntent(search, prefix, ("de", "suche"), ("en", "search"), ("fr", "cherche"), ("es", "busca"), ("it", "cerca")),
                CreateIntent(search, prefix, ("de", "finde"), ("en", "find"), ("fr", "trouve"), ("es", "encuentra"), ("it", "trova")),
                CreateIntent(search, prefix, ("de", "such"), ("en", "look for"), ("fr", "cherche"), ("es", "busca"), ("it", "cerca")),
                CreateIntent(describe, prefix, ("de", "beschreibe"), ("en", "describe"), ("fr", "decris"), ("es", "describe"), ("it", "descrivi")),
                CreateIntent(describe, prefix, ("de", "was ist"), ("en", "what is"), ("fr", "quest ce que cest"), ("es", "que es"), ("it", "che cos e")),
                CreateIntent(describe, prefix, ("de", "erklaere"), ("en", "explain"), ("fr", "explique"), ("es", "explica"), ("it", "spiega"))
            ],
            NumberWords =
            [
                CreateNumberWord(1, ("de", "eins"), ("en", "one"), ("fr", "un"), ("es", "uno"), ("it", "uno")),
                CreateNumberWord(1, ("de", "erste"), ("en", "first"), ("fr", "premier"), ("es", "primero"), ("it", "primo")),
                CreateNumberWord(2, ("de", "zwei"), ("en", "two"), ("fr", "deux"), ("es", "dos"), ("it", "due")),
                CreateNumberWord(2, ("de", "zweite"), ("en", "second"), ("fr", "deuxieme"), ("es", "segundo"), ("it", "secondo")),
                CreateNumberWord(3, ("de", "drei"), ("en", "three"), ("fr", "trois"), ("es", "tres"), ("it", "tre")),
                CreateNumberWord(3, ("de", "dritte"), ("en", "third"), ("fr", "troisieme"), ("es", "tercero"), ("it", "terzo")),
                CreateNumberWord(4, ("de", "vier"), ("en", "four"), ("fr", "quatre"), ("es", "cuatro"), ("it", "quattro")),
                CreateNumberWord(4, ("de", "vierte"), ("en", "fourth"), ("fr", "quatrieme"), ("es", "cuarto"), ("it", "quarto")),
                CreateNumberWord(5, ("de", "funf"), ("en", "five"), ("fr", "cinq"), ("es", "cinco"), ("it", "cinque")),
                CreateNumberWord(5, ("de", "fuenf"), ("en", "five"), ("fr", "cinq"), ("es", "cinco"), ("it", "cinque")),
                CreateNumberWord(5, ("de", "funfte"), ("en", "fifth"), ("fr", "cinquieme"), ("es", "quinto"), ("it", "quinto")),
                CreateNumberWord(5, ("de", "fuenfte"), ("en", "fifth"), ("fr", "cinquieme"), ("es", "quinto"), ("it", "quinto")),
                CreateNumberWord(6, ("de", "sechs"), ("en", "six"), ("fr", "six"), ("es", "seis"), ("it", "sei")),
                CreateNumberWord(6, ("de", "sechste"), ("en", "sixth"), ("fr", "sixieme"), ("es", "sexto"), ("it", "sesto")),
                CreateNumberWord(7, ("de", "sieben"), ("en", "seven"), ("fr", "sept"), ("es", "siete"), ("it", "sette")),
                CreateNumberWord(7, ("de", "siebte"), ("en", "seventh"), ("fr", "septieme"), ("es", "septimo"), ("it", "settimo")),
                CreateNumberWord(8, ("de", "acht"), ("en", "eight"), ("fr", "huit"), ("es", "ocho"), ("it", "otto")),
                CreateNumberWord(8, ("de", "achte"), ("en", "eighth"), ("fr", "huitieme"), ("es", "octavo"), ("it", "ottavo")),
                CreateNumberWord(9, ("de", "neun"), ("en", "nine"), ("fr", "neuf"), ("es", "nueve"), ("it", "nove")),
                CreateNumberWord(9, ("de", "neunte"), ("en", "ninth"), ("fr", "neuvieme"), ("es", "noveno"), ("it", "nono")),
                CreateNumberWord(10, ("de", "zehn"), ("en", "ten"), ("fr", "dix"), ("es", "diez"), ("it", "dieci")),
                CreateNumberWord(10, ("de", "zehnte"), ("en", "tenth"), ("fr", "dixieme"), ("es", "decimo"), ("it", "decimo"))
            ]
        };
    }

    private static CatalogVoiceIntent CreateIntent(string intentId, string matchMode, params (string Language, string Value)[] utterances)
    {
        return new CatalogVoiceIntent
        {
            IntentId = intentId,
            MatchMode = matchMode,
            Utterances = [CreateText(utterances)]
        };
    }

    private static CatalogVoiceNumberWord CreateNumberWord(int value, params (string Language, string Value)[] utterances)
    {
        return new CatalogVoiceNumberWord
        {
            Value = value,
            Utterances = [CreateText(utterances)]
        };
    }

    private static LocalizedText CreateText(params (string Language, string Value)[] values)
    {
        var text = new LocalizedText();
        foreach (var (language, value) in values)
        {
            text[language] = value;
        }

        return text;
    }
}


