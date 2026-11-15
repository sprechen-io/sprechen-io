namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Stabile Kennungen für Fehler und Warnungen der Modellvorprüfung.
/// </summary>
public static class PreflightCodes
{
    /// <summary>
    /// Doppelt verwendete Kennung.
    /// </summary>
    public const string ErrorIdDuplicate = "E_ID_DUPLICATE";

    /// <summary>
    /// Fehlende referenzierte Definition.
    /// </summary>
    public const string ErrorRefMissing = "E_REF_MISSING";

    /// <summary>
    /// Zyklus im Feldgruppengraphen.
    /// </summary>
    public const string ErrorFieldGroupCycle = "E_FIELD_GROUP_CYCLE";

    /// <summary>
    /// Vom Einstieg aus unerreichbare Feldgruppe.
    /// </summary>
    public const string ErrorFieldGroupUnreachable = "E_FIELD_GROUP_UNREACHABLE";

    /// <summary>
    /// Feldbindung mit unpassendem Werttyp.
    /// </summary>
    public const string ErrorBindingTypeMismatch = "E_BINDING_TYPE_MISMATCH";

    /// <summary>
    /// Fehlende erforderliche Übersetzung.
    /// </summary>
    public const string ErrorLocalizationMissing = "E_LOCALIZATION_MISSING";

    /// <summary>
    /// Unzulässiger Nullwert an einer kanonischen Wertposition.
    /// </summary>
    public const string ErrorNullForbidden = "E_NULL_FORBIDDEN";

    /// <summary>
    /// Verletzte Datenschutzregel.
    /// </summary>
    public const string ErrorPrivacyGuardrailViolation = "E_PRIVACY_GUARDRAIL_VIOLATION";

    /// <summary>
    /// Nicht unterstütztes Merkmal außerhalb des implementierten Modellumfangs.
    /// </summary>
    public const string ErrorFeatureNotSupported = "E_FEATURE_NOT_SUPPORTED";

    /// <summary>
    /// Widerspruch zwischen referenzierter und verfügbarer Version.
    /// </summary>
    public const string ErrorVersionInvariantBroken = "E_VERSION_INVARIANT_BROKEN";

    /// <summary>
    /// Widerspruch zwischen Standardsprache und unterstützten Sprachen.
    /// </summary>
    public const string ErrorLanguageInvariantBroken = "E_LANGUAGE_INVARIANT_BROKEN";

    /// <summary>
    /// Fehlende erforderliche Kennung.
    /// </summary>
    public const string ErrorIdMissing = "E_ID_MISSING";

    /// <summary>
    /// Wert außerhalb des zulässigen Bereichs.
    /// </summary>
    public const string ErrorValueOutOfRange = "E_VALUE_OUT_OF_RANGE";

    /// <summary>
    /// Zyklus zwischen Formularvoraussetzungen.
    /// </summary>
    public const string ErrorPrerequisiteCycle = "E_PREREQ_CYCLE";

    /// <summary>
    /// Ungültige Dialogeinstellung.
    /// </summary>
    public const string ErrorSettingsInvalid = "E_SETTINGS_INVALID";

    /// <summary>
    /// Nach Normalisierung leerer Auswahlname.
    /// </summary>
    public const string ErrorSelectionNameEmpty = "E_SELECTION_NAME_EMPTY";

    /// <summary>
    /// Mehrere Auswahlnamen mit demselben kompakten Vergleichsschlüssel.
    /// </summary>
    public const string ErrorSelectionNameDuplicate = "E_SELECTION_NAME_DUPLICATE";

    /// <summary>
    /// Auswahlname entspricht einem vorrangig erkannten Kommando.
    /// </summary>
    public const string ErrorSelectionNameIntentCollision = "E_SELECTION_NAME_INTENT_COLLISION";

    /// <summary>
    /// Auswahlname entspricht einer Aktivierungsform.
    /// </summary>
    public const string ErrorSelectionNameWakeCollision = "E_SELECTION_NAME_WAKE_COLLISION";

    /// <summary>
    /// Warnung zu einer im Ausdruck unbekannten Variable.
    /// </summary>
    public const string WarnExprVarUnknown = "W_EXPR_VAR_UNKNOWN";

    /// <summary>
    /// Warnung zu einer unvollständigen Übersetzung bei nicht strenger Übersetzungsvorgabe.
    /// </summary>
    public const string WarnLocalizationPartial = "W_LOCALIZATION_PARTIAL";

    /// <summary>
    /// Warnung zu Nicht-ASCII-Zeichen in einem Suchbegriff.
    /// </summary>
    public const string WarnTagNonAscii = "W_TAG_NON_ASCII";

    /// <summary>
    /// Feldübergreifende Regel zielt auf das ganze Formular statt auf ein bestimmtes Feld
    /// oder eine Variable. Eine eindeutige Zuordnung zur Korrektur fehlt.
    /// </summary>
    public const string WarnCrossFieldFormTarget = "W_CROSS_FIELD_FORM_TARGET";
}

