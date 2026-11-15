using Sprechen.Mda.SchemaBundle;

namespace Sprechen.MdaComposer.SchemaEngine;

/// <summary>
/// Verzeichnet die im Composer auswählbaren Basisschemas, damit Auswahl und Referenzzuordnung denselben Katalog verwenden.
/// </summary>
public static class ComposerRootCatalog
{
    private const string EvaluationModelArea = "Evaluationsmodell";

    public static IReadOnlyList<ComposerRootSchemaInfo> All { get; } =
    [
        new("field-definitions.schema.json", "Felddefinitionen", "Informationsmodell",
            "Wiederverwendbare Definitionen des Feldkatalogs"),
        new("form-contract.schema.json", "Formularvertrag", "Informationsmodell",
            "Laufzeitvertrag des Formulars"),
        new("variable-definitions.schema.json", "Variablendefinitionen", "Informationsmodell",
            "Katalog der Variablentypen"),
        new("contract-rule-evaluation.schema.json", "Vertragsregelauswertung", "Informationsmodell",
            "Verfasste Wahrheitstabelle, was eine Regel des Formularvertrags fuer einen Wert bedeutet"),

        new("form-voice-profile.schema.json", "Formular-Sprachprofil", "Dialogmodell",
            "Spracheinstellungen (Sicherheit, Zeitgrenzen, Sprechtempo)"),
        new("form-surface.schema.json", "Formularoberflaeche", "Dialogmodell",
            "Nur-lese-Momentaufnahme der Oberflaeche einer sprachgesteuerten Formularsitzung"),
        new("voice-interface.schema.json", "Sprachschnittstelle", "Dialogmodell",
            "Katalog der Sprachoberflaeche"),
        new("generic-prompt-catalog.schema.json", "Prompt-Katalog", "Dialogmodell",
            "Prompt-Definitionen"),
        new("wake-word-catalog.schema.json", "Aktivierungswort-Katalog", "Dialogmodell",
            "Aktivierungswort-Konfigurationen"),
        new("wake-word-detector-catalog.schema.json", "Aktivierungswort-Detektorkatalog", "Dialogmodell",
            "Eignung, Rueckfall und Politik der akustischen Aktivierungserkennung"),
        new("voice-resource-catalog.schema.json", "Sprachressourcen-Katalog", "Dialogmodell",
            "Sprachressourcen"),
        new("voice-lexicon-catalog.schema.json", "Sprachlexikon-Katalog", "Dialogmodell",
            "Lexikondefinitionen"),
        new("form-transcript-lexicon.schema.json", "Formular-Transkriptlexikon", "Dialogmodell",
            "Buchstabieralphabet, gesprochene Zahlwoerter und diakritische Bezeichnungen, die ein blinder Nutzer spricht, um Felder zu fuellen"),
        new("dialog-state-machine.schema.json", "Dialog-Zustandsmaschine", "Dialogmodell",
            "Zustandsmaschine des Dialogs"),
        new("speech-capture-profile.schema.json", "Spracherfassungsprofil", "Dialogmodell",
            "Zeitprofil der Spracherfassung je Modus (Stilleentprellung, maximale Aeusserungsdauer, Bestaetigung des Sprachstarts)"),

        new("forms-catalog.schema.json", "Formularkatalog", "Navigationsmodell",
            "Formularkatalog mit Bestaetigungspolitik"),
        new("application-distribution.schema.json", "Anwendungs-Distribution", "Navigationsmodell",
            "Eine Distribution von Anwendungen, aus der der ausgelieferte Katalog entsteht"),
        new("forms-chrome-copy.schema.json", "Chrome-Copy der Forms-Oberflaeche", "Navigationsmodell",
            "Vom Server gefuehrter Katalog fuer die Chrome-Copy der HTML-Oberflaeche"),
        new("application-selection-dialog.schema.json", "Anwendungsauswahl-Dialog", "Navigationsmodell",
            "Oberflaeche der Anwendungsauswahl"),
        new("application-selection-dialog-profile.schema.json", "Anwendungsauswahl-Profil", "Navigationsmodell",
            "Profil der Anwendungsauswahl"),
        new("voice-commands.schema.json", "Sprachbefehle", "Navigationsmodell",
            "Sprachbefehle"),
        new("status-announcement.schema.json", "Statusansage", "Navigationsmodell",
            "Statusmeldungen"),

        new("telemetry-events.schema.json", "Telemetrie-Ereignisse", "Evaluationsmodell",
            "Ereignisaufzeichnung"),
        new("telemetry-fragments.schema.json", "Telemetrie-Fragmente", "Evaluationsmodell",
            "Ereignisfragmente"),
        new("task-end-projection.schema.json", "Aufgabenende-Projektion", "Evaluationsmodell",
            "Verfasste Wahrheitstabelle, an die beide Kanaele beim Aufgabenende gebunden sind"),
        new("task-card-comparison-key.schema.json", "Vergleichsschluessel der Aufgabenkarte", "Evaluationsmodell",
            "Verfasste Wahrheitstabelle, an die beide Kanaele beim Vergleich eines Werts mit der Aufgabenkarte gebunden sind"),
        new("canonical-transition-projection.schema.json", "Kanonische Uebergangsprojektion", "Evaluationsmodell",
            "Verfasste Wahrheitstabelle fuer die Projektion der Interaktionsschritte"),
        new("open-error-identity.schema.json", "Offene-Fehler-Identitaet", "Evaluationsmodell",
            "Verfasste Wahrheitstabelle, wann zwei gemeldete Verstoesse ein offener Fehler sind"),
    ];

    public static IReadOnlyDictionary<string, IReadOnlyList<ComposerRootSchemaInfo>> ByModelArea { get; } =
        All.GroupBy(static schema => schema.ModelArea)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyList<ComposerRootSchemaInfo>)group.ToList(),
                StringComparer.OrdinalIgnoreCase);

    public static ComposerRootSchemaInfo? FindByFileName(string schemaFileName)
        => FindByFileName(schemaFileName, options: null);

    public static ComposerRootSchemaInfo? FindByFileName(string schemaFileName, ComposerOptions? options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaFileName);

        return GetForOptions(options).FirstOrDefault(
            schema => schema.FileName.Equals(schemaFileName, StringComparison.OrdinalIgnoreCase));
    }

    public static ComposerRootSchemaInfo? MatchReferenceContract(string referenceContractFileName)
        => MatchReferenceContract(referenceContractFileName, options: null);

    public static ComposerRootSchemaInfo? MatchReferenceContract(
        string referenceContractFileName,
        ComposerOptions? options)
        => MatchReferenceContract(referenceContractFileName, GetForOptions(options));

    // Die Zuordnung einer Referenzdatei zum Schema übernimmt der gemeinsame Resolver.
    // Dieser Katalog beschränkt die dafür verfügbaren Kandidaten.
    public static ComposerRootSchemaInfo? MatchReferenceContract(
        string referenceContractFileName,
        IEnumerable<ComposerRootSchemaInfo> catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceContractFileName);
        ArgumentNullException.ThrowIfNull(catalog);

        var candidates = catalog.ToList();
        var named = new MdaArtifactSchemaFileNameResolver()
            .Resolve(referenceContractFileName, candidates.Select(static schema => schema.FileName));

        return named is null
            ? null
            : candidates.First(schema => schema.FileName.Equals(named, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Liefert den Katalog gemäß den Optionen. Bei deaktiviertem
    /// <see cref="ComposerOptions.IncludeEvaluationSchemas"/> werden Schemas
    /// des Bereichs <c>Evaluationsmodell</c> ausgeschlossen.
    /// </summary>
    public static IReadOnlyList<ComposerRootSchemaInfo> GetForOptions(ComposerOptions? options)
    {
        if (options is null || options.IncludeEvaluationSchemas)
        {
            return All;
        }

        return All
            .Where(static schema => !schema.ModelArea.Equals(EvaluationModelArea, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
