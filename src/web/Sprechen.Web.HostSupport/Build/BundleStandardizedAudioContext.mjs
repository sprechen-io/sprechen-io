import { build } from 'esbuild';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// Bündelt standardized-audio-context (chrisguttandin, MIT) samt Abhängigkeiten als Browser-IIFE.
// Das npm-Paket wird unter window.standardizedAudioContext verfügbar; AudioContext und
// AudioWorkletNode können so von den klassischen Laufzeitskripten verwendet werden.
// Die erzeugte Datei liegt mit den übrigen Drittanbieterdateien im vendor-Verzeichnis.

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectDirectory = path.resolve(scriptDirectory, '..');
const outputFile = path.join(
    projectDirectory,
    'wwwroot',
    'voice-support',
    'vendor',
    'standardized-audio-context',
    'sac.global.js');

await build({
    stdin: {
        contents: "export * from 'standardized-audio-context';",
        resolveDir: projectDirectory,
        loader: 'js',
        sourcefile: 'sac-global-entry.js'
    },
    bundle: true,
    format: 'iife',
    globalName: 'standardizedAudioContext',
    outfile: outputFile,
    platform: 'browser',
    target: 'es2020',
    minify: true,
    legalComments: 'none'
});
