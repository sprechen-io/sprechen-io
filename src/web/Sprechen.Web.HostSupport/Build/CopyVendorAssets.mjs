import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const scriptDirectory = path.dirname(fileURLToPath(import.meta.url));
const projectDirectory = path.resolve(scriptDirectory, '..');
const vendorDirectory = path.join(projectDirectory, 'wwwroot', 'voice-support', 'vendor');

fs.rmSync(vendorDirectory, { recursive: true, force: true });

copyFile(
    path.join(projectDirectory, 'node_modules', 'bootstrap', 'dist', 'css', 'bootstrap.min.css'),
    path.join(vendorDirectory, 'bootstrap', 'bootstrap.min.css'));

copyFile(
    path.join(projectDirectory, 'node_modules', 'bootstrap-icons', 'font', 'bootstrap-icons.min.css'),
    path.join(vendorDirectory, 'bootstrap-icons', 'bootstrap-icons.min.css'));

copyDirectoryContents(
    path.join(projectDirectory, 'node_modules', 'bootstrap-icons', 'font', 'fonts'),
    path.join(vendorDirectory, 'bootstrap-icons', 'fonts'));

function copyFile(sourcePath, destinationPath) {
    fs.mkdirSync(path.dirname(destinationPath), { recursive: true });
    fs.copyFileSync(sourcePath, destinationPath);
}

function copyDirectoryContents(sourceDirectory, destinationDirectory) {
    fs.mkdirSync(destinationDirectory, { recursive: true });

    for (const entryName of fs.readdirSync(sourceDirectory)) {
        const sourcePath = path.join(sourceDirectory, entryName);
        const destinationPath = path.join(destinationDirectory, entryName);
        const entry = fs.statSync(sourcePath);

        if (entry.isDirectory()) {
            copyDirectoryContents(sourcePath, destinationPath);
            continue;
        }

        copyFile(sourcePath, destinationPath);
    }
}
