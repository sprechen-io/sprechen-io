
/** Referenz auf eine .NET-Instanz für die Weiterleitung von Tastaturbefehlen. */
interface DotNetObjectReference {
    invokeMethodAsync(methodName: string): Promise<void>;
}

/** Browserfunktionen des Composers für Zwischenablage, Dateidownload und Zustandsspeicherung. */
interface ComposerInterop {
    copyToClipboard(text: string): Promise<boolean>;
    downloadJson(filename: string, content: string): void;
    registerKeyboardShortcuts(dotNetRef: DotNetObjectReference): void;
    unregisterKeyboardShortcuts(): void;
    triggerFileInput(inputId: string): void;
    saveState(key: string, value: string): void;
    loadState(key: string): string | null;
    removeState(key: string): void;
    setDirtyFlag(isDirty: boolean): void;
    _keyHandler: ((e: KeyboardEvent) => void) | null;
    _beforeUnloadHandler: ((e: BeforeUnloadEvent) => void) | null;
}

const composerInterop: ComposerInterop = {
    _keyHandler: null,
    _beforeUnloadHandler: null,

    async copyToClipboard(text: string): Promise<boolean> {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            const textarea = document.createElement("textarea");
            textarea.value = text;
            textarea.style.position = "fixed";
            textarea.style.opacity = "0";
            document.body.appendChild(textarea);
            textarea.select();
            const ok = document.execCommand("copy");
            document.body.removeChild(textarea);
            return ok;
        }
    },

    downloadJson(filename: string, content: string): void {
        const blob = new Blob([content], { type: "application/json" });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    },

    registerKeyboardShortcuts(dotNetRef: DotNetObjectReference): void {
        if (this._keyHandler) {
            document.removeEventListener("keydown", this._keyHandler);
        }
        this._keyHandler = (e: KeyboardEvent): void => {
            const mod = e.ctrlKey || e.metaKey;
            const tag = (e.target as HTMLElement).tagName;
            const isInput = tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT";

            if (mod && e.key === "s") {
                e.preventDefault();
                dotNetRef.invokeMethodAsync("OnKeyboardDownload");
            } else if (mod && e.shiftKey && e.key === "C") {
                e.preventDefault();
                dotNetRef.invokeMethodAsync("OnKeyboardCopy");
            } else if (mod && !e.shiftKey && e.key === "z" && !isInput) {
                e.preventDefault();
                dotNetRef.invokeMethodAsync("OnKeyboardUndo");
            } else if (mod && e.key === "y" && !isInput) {
                e.preventDefault();
                dotNetRef.invokeMethodAsync("OnKeyboardRedo");
            }
        };
        document.addEventListener("keydown", this._keyHandler);
    },

    unregisterKeyboardShortcuts(): void {
        if (this._keyHandler) {
            document.removeEventListener("keydown", this._keyHandler);
            this._keyHandler = null;
        }
    },

    triggerFileInput(inputId: string): void {
        const el = document.getElementById(inputId) as HTMLInputElement | null;
        if (el) {
            el.click();
        }
    },

    // Speichert den Entwurf in localStorage, damit er nach einem Neuladen wiederhergestellt werden kann.
    saveState(key: string, value: string): void {
        try { localStorage.setItem(key, value); } catch {  }
    },

    loadState(key: string): string | null {
        try { return localStorage.getItem(key); } catch { return null; }
    },

    removeState(key: string): void {
        try { localStorage.removeItem(key); } catch {  }
    },

    setDirtyFlag(isDirty: boolean): void {
        if (isDirty && !this._beforeUnloadHandler) {
            this._beforeUnloadHandler = (e: BeforeUnloadEvent): void => {
                e.preventDefault();
                e.returnValue = "";
            };
            window.addEventListener("beforeunload", this._beforeUnloadHandler);
        } else if (!isDirty && this._beforeUnloadHandler) {
            window.removeEventListener("beforeunload", this._beforeUnloadHandler);
            this._beforeUnloadHandler = null;
        }
    },
};

// Stellt composerInterop auf window für Blazor-Aufrufe bereit.
(window as unknown as Record<string, unknown>)["composerInterop"] = composerInterop;
