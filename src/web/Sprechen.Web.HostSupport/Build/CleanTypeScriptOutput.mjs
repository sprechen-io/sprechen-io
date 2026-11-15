import { rmSync } from "node:fs";

// TypeScript entfernt die Ausgaben gelöschter Quelldateien nicht selbst. Der erzeugte
// JavaScript-Baum wird vor dem Build geleert, damit keine veralteten Dateien veröffentlicht werden.
rmSync(new URL("../wwwroot/voice-support/js", import.meta.url), {
    force: true,
    recursive: true
});
