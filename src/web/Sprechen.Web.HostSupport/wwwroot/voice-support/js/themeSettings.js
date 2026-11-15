"use strict";
(() => {
    "use strict";
    // HostedVoiceWebDocument.razor liest denselben Schlüssel vor dem Laden der Stylesheets.
    // Der Modus wird als einfacher Text gespeichert, nicht als JSON.
    // Schlüssel und erlaubte Werte müssen mit dem Initialisierungsskript übereinstimmen.
    const StorageKey = "sprechenIo.voice.appearance.theme";
    const ThemeAttribute = "data-sp-theme";
    const normalizeMode = (value) => {
        return value === "light" || value === "dark"
            ? value
            : "system";
    };
    const applyTheme = (mode) => {
        // Ohne Attribut bestimmt prefers-color-scheme die Darstellung.
        if (mode === "system") {
            document.documentElement.removeAttribute(ThemeAttribute);
            return;
        }
        document.documentElement.setAttribute(ThemeAttribute, mode);
    };
    const tryRead = () => {
        try {
            return normalizeMode(window.localStorage.getItem(StorageKey));
        }
        catch {
            return "system";
        }
    };
    const tryWrite = (mode) => {
        try {
            if (mode === "system") {
                window.localStorage.removeItem(StorageKey);
                return;
            }
            window.localStorage.setItem(StorageKey, mode);
        }
        catch {
        }
    };
    applyTheme(tryRead());
    window.themeSettings = {
        get: () => tryRead(),
        set: (value) => {
            const mode = normalizeMode(value);
            tryWrite(mode);
            applyTheme(mode);
        }
    };
})();
