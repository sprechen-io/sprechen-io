/**
 * Zeigt Feldgruppen als einzelne Formularseiten und verwaltet Vor-/Zurück-Navigation,
 * Fokuswechsel und Positionsansage. formNavigation verwaltet weiterhin Regeln,
 * Validierung und Speicherung und ruft danach syncVisibility auf.
 * Dieses klassische Skript wird vor formNavigation.js geladen. Ohne JavaScript
 * bleiben die serverseitig untereinander angeordneten Feldgruppen bestehen.
 */

interface FormGroupPagerDeps {
    form: HTMLElement;
    getText(key: UiTextKey): string;
    isGroupRuleHidden(fieldGroupId: string): boolean;
    /** Führt den regelgetriebenen Sichtbarkeitslauf erneut aus (endet mit `syncVisibility`). */
    renderRuleVisibility(): void;
}

/** Schnittstelle zum Anzeigen einer Feldgruppe und zum Abgleichen ihrer Sichtbarkeit. */
interface FormGroupPager {
    readonly isPagingActive: boolean;
    readonly currentGroupId: string | null;
    /** Zeigt die Gruppe ohne Fokuswechsel. Eine geänderte Position wird angesagt. */
    revealGroup(fieldGroupId: string): void;
    /** Gleicht die aktuelle Seite mit der regelabhängigen Sichtbarkeit ab. */
    syncVisibility(): void;
    dispose(): void;
}

function createFormGroupPager(deps: FormGroupPagerDeps): FormGroupPager {
    const groupIdsInDomOrder = deriveGroupIdsInDomOrder();
    const pagingActive = groupIdsInDomOrder.length > 1;
    let currentGroupId: string | null = pagingActive ? groupIdsInDomOrder[0] : null;
    let disposed = false;

    let navElement: HTMLElement | null = null;
    let statusElement: HTMLElement | null = null;
    let previousButton: HTMLButtonElement | null = null;
    let nextButton: HTMLButtonElement | null = null;

    if (pagingActive) {
        renderControls();
        // Wählt bei der Initialisierung ohne Ansage die erste verfügbare Gruppe.
        const visible = visibleSequence();
        if (currentGroupId === null || visible.indexOf(currentGroupId) < 0) {
            currentGroupId = visible[0] ?? null;
        }

        applyPageVisibility();
        updateControls();
    }

    function deriveGroupIdsInDomOrder(): string[] {
        const ids: string[] = [];
        const seen = new Set<string>();
        const elements = deps.form.querySelectorAll<HTMLElement>("[data-field-group-id]");
        for (const element of Array.from(elements)) {
            const id = (element.dataset.fieldGroupId ?? "").trim();
            if (id.length > 0 && !seen.has(id)) {
                seen.add(id);
                ids.push(id);
            }
        }

        return ids;
    }

    function groupElements(fieldGroupId: string): HTMLElement[] {
        const elements = deps.form.querySelectorAll<HTMLElement>("[data-field-group-id]");
        return Array.from(elements).filter((element) => (element.dataset.fieldGroupId ?? "").trim() === fieldGroupId);
    }

    function groupHeader(fieldGroupId: string): HTMLElement | null {
        const headings = deps.form.querySelectorAll<HTMLElement>("h3[data-field-group-id]");
        for (const heading of Array.from(headings)) {
            if ((heading.dataset.fieldGroupId ?? "").trim() === fieldGroupId) {
                return heading;
            }
        }

        return null;
    }

    function visibleSequence(): string[] {
        const visible = groupIdsInDomOrder.filter((id) => !deps.isGroupRuleHidden(id));
        // Wenn die Regeln keine sichtbare Gruppe liefern, bleibt die erste Gruppe als Ersatz im Seitenablauf.
        return visible.length > 0 ? visible : groupIdsInDomOrder.slice(0, 1);
    }

    function formatStep(template: string, position: number, total: number): string {
        return template.replace("{0}", String(position)).replace("{1}", String(total));
    }

    function renderControls(): void {
        navElement = document.createElement("nav");
        navElement.className = "field-group-pager";
        navElement.setAttribute("aria-label", deps.getText("pagerNavLabel"));

        previousButton = document.createElement("button");
        previousButton.type = "button";
        previousButton.className = "field-group-pager-previous";
        previousButton.textContent = deps.getText("pagerPrevious");
        previousButton.addEventListener("click", () => goToOffset(-1));

        nextButton = document.createElement("button");
        nextButton.type = "button";
        nextButton.className = "field-group-pager-next";
        nextButton.textContent = deps.getText("pagerNext");
        nextButton.addEventListener("click", () => goToOffset(1));

        navElement.appendChild(previousButton);
        navElement.appendChild(nextButton);

        const crossFormNavigation = deps.form.querySelector<HTMLElement>(".form-navigation");
        if (crossFormNavigation) {
            deps.form.insertBefore(navElement, crossFormNavigation);
        } else {
            deps.form.appendChild(navElement);
        }

        // Eigene Live-Region außerhalb der Formularregionen, um verschachtelte Positionsansagen zu vermeiden.
        statusElement = document.createElement("div");
        statusElement.className = "sr-only field-group-pager-status";
        statusElement.setAttribute("role", "status");
        statusElement.setAttribute("aria-live", "polite");
        document.body.appendChild(statusElement);
    }

    function updateControls(): void {
        if (!previousButton || !nextButton || currentGroupId === null) {
            return;
        }

        const sequence = visibleSequence();
        const index = sequence.indexOf(currentGroupId);
        const position = index + 1;
        const total = sequence.length;

        previousButton.disabled = index <= 0;
        previousButton.setAttribute("aria-label", formatStep(deps.getText("pagerPreviousGroupTemplate"), position, total));

        const isLast = index < 0 || index >= total - 1;
        nextButton.style.display = isLast ? "none" : "";
        nextButton.setAttribute("aria-label", formatStep(deps.getText("pagerNextGroupTemplate"), position, total));
    }

    function applyPageVisibility(): void {
        if (!pagingActive || disposed || currentGroupId === null) {
            return;
        }

        // Der Regel-Sichtbarkeitslauf läuft davor. Hier werden nur die nicht aktuellen Seiten verborgen.
        // Die aktuelle Seite behält die vom Regellauf gesetzte Sichtbarkeit.
        const allGroupElements = deps.form.querySelectorAll<HTMLElement>("[data-field-group-id]");
        for (const element of Array.from(allGroupElements)) {
            const id = (element.dataset.fieldGroupId ?? "").trim();
            if (id.length > 0 && id !== currentGroupId) {
                element.style.display = "none";
            }
        }
    }

    function announcePosition(): void {
        if (!statusElement || currentGroupId === null) {
            return;
        }

        const sequence = visibleSequence();
        const index = sequence.indexOf(currentGroupId);
        const step = formatStep(deps.getText("pagerStepTemplate"), index + 1, sequence.length);
        const header = groupHeader(currentGroupId);
        const title = (header?.textContent ?? "").trim();
        statusElement.textContent = title.length > 0 ? `${step}: ${title}` : step;
    }

    function focusCurrentGroup(): void {
        if (currentGroupId === null) {
            return;
        }

        const target = groupHeader(currentGroupId) ?? groupElements(currentGroupId)[0] ?? null;
        if (!target) {
            return;
        }

        target.setAttribute("tabindex", "-1");
        target.focus();
    }

    function goTo(fieldGroupId: string, moveFocus: boolean): void {
        if (!pagingActive || disposed) {
            return;
        }

        const changed = fieldGroupId !== currentGroupId;
        currentGroupId = fieldGroupId;
        deps.renderRuleVisibility();

        if (changed) {
            announcePosition();
        }

        if (moveFocus) {
            focusCurrentGroup();
        }
    }

    function goToOffset(offset: number): void {
        if (currentGroupId === null) {
            return;
        }

        const sequence = visibleSequence();
        const index = sequence.indexOf(currentGroupId);
        const target = sequence[index + offset];
        if (target !== undefined) {
            goTo(target, true);
        }
    }

    return {
        get isPagingActive(): boolean {
            return pagingActive && !disposed;
        },

        get currentGroupId(): string | null {
            return currentGroupId;
        },

        revealGroup(fieldGroupId: string): void {
            if (!pagingActive || disposed) {
                return;
            }

            const id = (fieldGroupId ?? "").trim();
            if (id.length === 0 || groupIdsInDomOrder.indexOf(id) < 0) {
                return;
            }

            // Nur Gruppen der aktuellen sichtbaren Sequenz dürfen ausgewählt werden.
            if (visibleSequence().indexOf(id) < 0) {
                return;
            }

            goTo(id, false);
        },

        syncVisibility(): void {
            if (!pagingActive || disposed) {
                return;
            }

            const sequence = visibleSequence();
            if (currentGroupId !== null && sequence.indexOf(currentGroupId) < 0) {
                // Der Regellauf hat die aktuelle Seite verborgen: weiter zur nächsten sichtbaren Gruppe
                // (sonst die letzte sichtbare davor) und einmal ansagen.
                const currentIndex = groupIdsInDomOrder.indexOf(currentGroupId);
                let target: string | undefined;
                for (const candidate of sequence) {
                    if (groupIdsInDomOrder.indexOf(candidate) > currentIndex) {
                        target = candidate;
                        break;
                    }
                }

                target = target ?? sequence[sequence.length - 1];
                if (target !== undefined) {
                    goTo(target, false);
                    return;
                }
            }

            applyPageVisibility();
            updateControls();
        },

        dispose(): void {
            disposed = true;
            navElement?.remove();
            statusElement?.remove();
            navElement = null;
            statusElement = null;
            previousButton = null;
            nextButton = null;
        }
    };
}
