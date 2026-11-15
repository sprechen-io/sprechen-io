import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const require = createRequire(pathToFileURL(process.argv[1]));
const puppeteer = require('puppeteer-core');
const axe = require('axe-core');

const target = process.argv[2];
const chromePath = process.argv[3];
const tags = (process.argv[4] || 'wcag2a,wcag2aa,wcag21a,wcag21aa,wcag22aa')
    .split(',')
    .map((tag) => tag.trim())
    .filter(Boolean);
const timeoutMs = Number(process.argv[5] || 90000);

const browser = await puppeteer.launch({
    executablePath: chromePath,
    headless: true,
    args: ['--no-sandbox', '--disable-dev-shm-usage', '--disable-gpu']
});

try {
    const page = await browser.newPage();
    page.setDefaultNavigationTimeout(timeoutMs);
    await page.goto(target, { waitUntil: 'domcontentloaded', timeout: timeoutMs });
    await page.evaluate(axe.source);
    const result = await page.evaluate(async (selectedTags) => {
        return await axe.run(document, {
            runOnly: { type: 'tag', values: selectedTags },
            resultTypes: ['violations', 'incomplete', 'passes']
        });
    }, tags);
    result.testEngine = { name: 'axe-core', version: axe.version };
    process.stdout.write(JSON.stringify(result));
}
finally {
    await browser.close();
}
