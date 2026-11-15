/** Farbschema als Klartext für das frühe Initialisierungsskript. */
type ThemeMode = "system" | "light" | "dark";

/** Liest, speichert und aktiviert das ausgewählte Farbschema. */
type ThemeSettingsApi = {
    get(): ThemeMode;
    set(value: unknown): void;
};

/** Stellt den Zugriff auf das Farbschema für klassische Skripte auf window bereit. */
interface Window {
    themeSettings: ThemeSettingsApi;
}

((): void => {
    "use strict";

    // HostedVoiceWebDocument.razor liest denselben Schlüssel vor dem Laden der Stylesheets.
    // Der Modus wird als einfacher Text gespeichert, nicht als JSON.
    // Schlüssel und erlaubte Werte müssen mit dem Initialisierungsskript übereinstimmen.
    const StorageKey = "sprechenIo.voice.appearance.theme";
    const ThemeAttribute = "data-sp-theme";

    const normalizeMode = (value: unknown): ThemeMode => {
        return value === "light" || value === "dark"
            ? value
            : "system";
    };

    const applyTheme = (mode: ThemeMode): void => {
        // Ohne Attribut bestimmt prefers-color-scheme die Darstellung.
        if (mode === "system") {
            document.documentElement.removeAttribute(ThemeAttribute);
            return;
        }

        document.documentElement.setAttribute(ThemeAttribute, mode);
    };

    const tryRead = (): ThemeMode => {
        try {
            return normalizeMode(window.localStorage.getItem(StorageKey));
        } catch {
            return "system";
        }
    };

    const tryWrite = (mode: ThemeMode): void => {
        try {
            if (mode === "system") {
                window.localStorage.removeItem(StorageKey);
                return;
            }

            window.localStorage.setItem(StorageKey, mode);
        } catch {
        }
    };

    applyTheme(tryRead());

    window.themeSettings = {
        get: (): ThemeMode => tryRead(),
        set: (value: unknown): void => {
            const mode = normalizeMode(value);
            tryWrite(mode);
            applyTheme(mode);
        }
    };
})();
