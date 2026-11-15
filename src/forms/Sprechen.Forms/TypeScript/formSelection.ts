/** Kennung und Anzeigetext eines auswählbaren Formulars. */

interface FormSelectionOption {
    formId: string;
    displayName: string;
}

/** Ungeprüfte Antwortdaten des Index-Handlers für die Formularauswahl. */
interface FormSelectionResponse {
    forms?: unknown;
    formId?: unknown;
    language?: unknown;
    applicationId?: unknown;
    errorCode?: unknown;
}

const normalizeText = (value: unknown): string =>
    typeof value === "string"
        ? value.trim()
        : "";

const isFormSelectionOption = (value: unknown): value is FormSelectionOption => {
    if (typeof value !== "object" || value === null) {
        return false;
    }

    const record = value as Record<string, unknown>;
    return typeof record.formId === "string" && typeof record.displayName === "string";
};

const parseFormSelectionOptions = (value: unknown): FormSelectionOption[] => {
    if (!Array.isArray(value)) {
        return [];
    }

    const options: FormSelectionOption[] = [];

    for (const item of value) {
        if (!isFormSelectionOption(item)) {
            continue;
        }

        const formId = normalizeText(item.formId);
        if (formId.length === 0) {
            continue;
        }

        const displayName = normalizeText(item.displayName);
        if (displayName.length === 0) {
            continue;
        }

        options.push({ formId, displayName });
    }

    return options;
};

const createSelectOption = (value: string, label: string): HTMLOptionElement => {
    const option = document.createElement("option");
    option.value = value;
    option.textContent = label;
    return option;
};

((): void => {
    "use strict";

    const selectionFormSelector = "#selection-form";
    const appSelectSelector = "#app";
    const langSelectSelector = "#lang";
    const formSelectSelector = "#form";
    const previousAppInputSelector = "input[name='prevApp']";
    const submitButtonSelector = "button[type='submit']";

    const selectionForm = document.querySelector<HTMLFormElement>(selectionFormSelector);
    const appSelect = document.querySelector<HTMLSelectElement>(appSelectSelector);
    const langSelect = document.querySelector<HTMLSelectElement>(langSelectSelector);
    const formSelect = document.querySelector<HTMLSelectElement>(formSelectSelector);

    if (!selectionForm || !appSelect || !langSelect || !formSelect) {
        return;
    }

    const submitButton = selectionForm.querySelector<HTMLButtonElement>(submitButtonSelector);
    const previousAppInput = selectionForm.querySelector<HTMLInputElement>(previousAppInputSelector);
    const placeholderText = normalizeText(formSelect.options[0]?.textContent);
    const disabledSelectText = placeholderText.length > 0 ? placeholderText : "\u2014";

    let inFlightRequest: AbortController | null = null;

    // Teilnehmer-Token: 6-16 Kleinbuchstaben oder Ziffern, wie in studyIdentityHash.
    const studyTokenPattern = /^[a-z0-9]{6,16}$/;

    // Die Formularauswahl übernimmt das Token aus window.studyIdentity in den Pfad
    // `/umfrage/<token>`, damit die Teilnehmerkennung beim Navigieren erhalten bleibt.
    const preserveStudyTokenOnSubmit = (): void => {
        const identity = (window as unknown as { studyIdentity?: { participantId?: string } }).studyIdentity;
        const participantId = identity?.participantId?.trim() ?? "";
        if (!studyTokenPattern.test(participantId)) {
            selectionForm.action = "/";
            return;
        }

        selectionForm.action = `/umfrage/${participantId}`;
    };

    const renderFormOptions = (forms: ReadonlyArray<FormSelectionOption>): void => {
        const firstForm = forms[0] ?? null;

        formSelect.replaceChildren();
        formSelect.appendChild(createSelectOption("", disabledSelectText));

        for (const form of forms) {
            formSelect.appendChild(createSelectOption(form.formId, form.displayName));
        }

        formSelect.value = "";

        const hasForms = forms.length > 0;
        formSelect.disabled = !hasForms;
        formSelect.setAttribute("aria-disabled", hasForms ? "false" : "true");

        if (submitButton) {
            submitButton.disabled = !hasForms;
        }

        if (firstForm) {
            formSelect.value = firstForm.formId;
        }

        if (submitButton) {
            submitButton.disabled = !hasForms;
        }
    };

    const clearFormOptions = (): void => {
        formSelect.replaceChildren();
        formSelect.appendChild(createSelectOption("", disabledSelectText));
        formSelect.disabled = true;
        formSelect.setAttribute("aria-disabled", "true");
        if (submitButton) {
            submitButton.disabled = true;
        }
    };

    const applyApplicationChange = async (): Promise<void> => {
        const selectedApp = normalizeText(appSelect.value);
        const selectedLanguage = normalizeText(langSelect.value);

        if (selectedApp.length === 0) {
            clearFormOptions();
            return;
        }

        inFlightRequest?.abort();
        inFlightRequest = new AbortController();

        if (submitButton) {
            submitButton.disabled = true;
        }
        formSelect.disabled = true;
        formSelect.setAttribute("aria-disabled", "true");

        // Der Formularlisten-Endpunkt liegt auf `/`, unabhängig vom Aktionspfad des Formulars,
        // den der Submit-Handler auf `/umfrage/<token>` setzen kann.
        const url = new URL("/", window.location.origin);
        url.searchParams.set("handler", "forms");
        url.searchParams.set("app", selectedApp);
        if (selectedLanguage.length > 0) {
            url.searchParams.set("lang", selectedLanguage);
        }
        else {
            url.searchParams.delete("lang");
        }

        try {
            const response = await fetch(url, {
                method: "GET",
                headers: {
                    Accept: "application/json"
                },
                signal: inFlightRequest.signal
            });

            if (!response.ok) {
                throw new Error(`Failed to load forms for application: ${selectedApp}`);
            }

            const payload = (await response.json()) as FormSelectionResponse;
            if (typeof payload !== "object" || payload === null) {
                throw new Error("Invalid form list response");
            }

            const forms = parseFormSelectionOptions(payload.forms);
            renderFormOptions(forms);
        }
        catch (error: unknown) {
            if ((error as Error & { name?: string }).name === "AbortError") {
                return;
            }

            clearFormOptions();
            console.warn("Unable to load forms for selected application", error);
        }
    };

    const submitLanguageChange = (): void => {
        inFlightRequest?.abort();
        if (previousAppInput) {
            previousAppInput.value = normalizeText(appSelect.value);
        }

        formSelect.value = "";
        preserveStudyTokenOnSubmit();

        if (typeof selectionForm.requestSubmit === "function") {
            selectionForm.requestSubmit();
            return;
        }

        selectionForm.submit();
    };

    appSelect.addEventListener("change", () => {
        void applyApplicationChange();
    });

    selectionForm.addEventListener("submit", preserveStudyTokenOnSubmit);

    langSelect.addEventListener("change", () => {
        submitLanguageChange();
    });

    if (normalizeText(appSelect.value).length === 0) {
        clearFormOptions();
    }
})();
