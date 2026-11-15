/**
 * Liest die Teilnehmerkennung aus `/umfrage/<token>`. Zusätzliche Pfadsegmente
 * liefern keine Identität. Die Bedingung bezeichnet den Anwendungskanal
 * (Formular: A) und ist kein Pfadparameter.
 */

interface StudyIdentity {
    participantId: string;
    condition: string;
    hasStudyContext: boolean;
}

const EMPTY_IDENTITY: StudyIdentity = {
    participantId: "",
    condition: "A",
    hasStudyContext: false,
};

const normalizeCondition = (
    raw: string | null | undefined,
    participantInvited: boolean
): StudyCondition => {
    const normalized = raw?.trim().toUpperCase() ?? "";
    if (normalized === "A" || normalized === "B") {
        return normalized;
    }

    if (!participantInvited && normalized.length === 0) {
        return "A";
    }

    throw new Error("telemetry.identity.study_condition_invalid");
};

/** Vom Studienlink gelieferte Werte vor dem Einsetzen von Platzhaltern. */
interface SuppliedStudyConfiguration {
    participantId?: string | null;
}

// Die Einladung wird an der ursprünglich gelieferten Teilnehmerkennung erkannt.
// Nach dem Einsetzen eines Platzhalters wäre eine fehlende Kennung nicht mehr erkennbar.
// hasStudyContext steuert dagegen die Anzeige des Studienblocks anhand des URL-Pfads.
const studyRunIsParticipantInvited = (supplied: SuppliedStudyConfiguration): boolean =>
    (supplied.participantId?.trim().length ?? 0) > 0;

const TOKEN_PATTERN = /^[a-z0-9]{6,16}$/;

(function () {
    "use strict";

    const parsePath = (pathname: string): StudyIdentity => {
        const segments = pathname.split("/").filter((segment) => segment.length > 0);

        if (segments.length !== 2
            || segments[0].toLowerCase() !== "umfrage"
            || !TOKEN_PATTERN.test(segments[1])) {
            return { ...EMPTY_IDENTITY };
        }

        return {
            participantId: segments[1],
            condition: EMPTY_IDENTITY.condition,
            hasStudyContext: true,
        };
    };

    const applyToDom = (identity: StudyIdentity): void => {
        const setTextContent = (selector: string, value: string): void => {
            const element = document.querySelector<HTMLElement>(selector);
            if (!element) {
                return;
            }
            element.textContent = value;
        };

        setTextContent("[data-study-info-participant]", identity.participantId);
        setTextContent("[data-study-info-condition]", identity.condition);

        const studyInfoBlock = document.querySelector<HTMLElement>("[data-study-info-block]");
        if (studyInfoBlock) {
            studyInfoBlock.hidden = !identity.hasStudyContext;
        }
    };

    const initialize = (): void => {
        const identity = parsePath(window.location.pathname);
        (window as unknown as { studyIdentity: StudyIdentity }).studyIdentity = identity;
        applyToDom(identity);
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize, { once: true });
    } else {
        initialize();
    }
})();
