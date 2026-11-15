#Requires -Version 7.0
<#
.SYNOPSIS
    Checks one or more pages with axe-core and writes a text report.

.DESCRIPTION
    Installs the pinned releases axe-core 4.13.0 and puppeteer-core 25.11.0
    under src/tools/wcag when that exact pair is missing, then runs the
    check. 4.13.0 is the newest stable axe-core release, not a floating npm tag.
    The report is a tool result. It is not a WCAG conformance claim.

    This is separate from scripts/Invoke-FormsWcagCheck.ps1, which builds the
    forms app and sweeps its own pages. This script takes the page address as
    an argument and does not start an application.

.EXAMPLE
    pwsh ./src/tools/wcag/Invoke-WcagAbfrage.ps1
    Ohne -Url fragt das Skript die Adressen ab. Eine leere Eingabe schliesst die Liste ab.

.EXAMPLE
    pwsh ./src/tools/wcag/Invoke-WcagAbfrage.ps1 -Url https://<forms-host>/,https://<web-host>/

.EXAMPLE
    pwsh ./src/tools/wcag/Invoke-WcagAbfrage.ps1 -Url https://<forms-host>/ `
      -Tags wcag22aa -ChromePath /usr/bin/chromium -OutFile ./artifacts/wcag-abfrage/forms.txt
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string[]]$Url,

    [string]$OutFile,

    [string]$Tags = 'wcag2a,wcag2aa,wcag21a,wcag21aa,wcag22aa',

    [string]$ChromePath,

    [int]$TimeoutSec = 90
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-HomeDirectory {
    foreach ($candidate in @($HOME, $env:USERPROFILE, $env:HOME)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { return $candidate }
    }
    return $null
}

function Find-Chrome {
    param([string]$Explicit)
    if ($Explicit -and (Test-Path -LiteralPath $Explicit)) {
        return (Resolve-Path -LiteralPath $Explicit).Path
    }
    foreach ($name in @('google-chrome', 'google-chrome-stable', 'chromium', 'chromium-browser', 'chrome', 'chrome.exe', 'msedge', 'msedge.exe')) {
        $found = Get-Command $name -ErrorAction SilentlyContinue
        if ($found) { return $found.Source }
    }
    $known = @()
    foreach ($place in @(
            @{ Root = $env:ProgramFiles; Tail = 'Google\Chrome\Application\chrome.exe' },
            @{ Root = ${env:ProgramFiles(x86)}; Tail = 'Google\Chrome\Application\chrome.exe' },
            @{ Root = $env:LOCALAPPDATA; Tail = 'Google\Chrome\Application\chrome.exe' },
            @{ Root = $env:ProgramFiles; Tail = 'Microsoft\Edge\Application\msedge.exe' },
            @{ Root = ${env:ProgramFiles(x86)}; Tail = 'Microsoft\Edge\Application\msedge.exe' }
        )) {
        if ([string]::IsNullOrWhiteSpace($place.Root)) { continue }
        $known += (Join-Path $place.Root $place.Tail)
    }
    foreach ($path in $known) {
        if (Test-Path -LiteralPath $path) { return (Resolve-Path -LiteralPath $path).Path }
    }
    $caches = @()
    if (-not [string]::IsNullOrWhiteSpace($env:PLAYWRIGHT_BROWSERS_PATH)) {
        $caches += $env:PLAYWRIGHT_BROWSERS_PATH
    }
    $homeDir = Get-HomeDirectory
    if ($homeDir) {
        $caches += (Join-Path $homeDir '.cache/ms-playwright')
        $caches += (Join-Path $homeDir 'AppData/Local/ms-playwright')
    }
    foreach ($cache in $caches) {
        if (-not (Test-Path -LiteralPath $cache)) { continue }
        $candidate = Get-ChildItem -Path $cache -Recurse -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -in @('chrome', 'chrome.exe') } |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($candidate) { return $candidate.FullName }
    }
    return $null
}

function ConvertTo-PageUri {
    param([string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    $trimmed = $Text.Trim()
    if ($trimmed -notmatch '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        $trimmed = "https://$trimmed"
    }
    $uri = [uri]$trimmed
    if (-not $uri.IsAbsoluteUri) {
        throw "Keine absolute Seitenadresse: $Text"
    }
    return $uri
}

function Get-InstalledPackageVersion {
    param([string]$PackageJson)
    if (-not (Test-Path -LiteralPath $PackageJson)) { return $null }
    return (Get-Content -Raw -LiteralPath $PackageJson | ConvertFrom-Json).version
}

function Install-AxeCore {
    param([string]$Directory)
    $axeVersion = '4.13.0'
    $puppeteerVersion = '25.11.0'
    $axePackage = Join-Path $Directory 'node_modules/axe-core/package.json'
    $puppeteerPackage = Join-Path $Directory 'node_modules/puppeteer-core/package.json'
    if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
        throw 'npm is missing. axe-core cannot be installed.'
    }
    $installedAxe = Get-InstalledPackageVersion -PackageJson $axePackage
    $installedPuppeteer = Get-InstalledPackageVersion -PackageJson $puppeteerPackage
    if ($installedAxe -eq $axeVersion -and $installedPuppeteer -eq $puppeteerVersion) {
        return $axeVersion
    }
    New-Item -ItemType Directory -Force -Path $Directory | Out-Null
    Write-Host "Installing axe-core@$axeVersion and puppeteer-core@$puppeteerVersion under $Directory"
    & npm install --prefix $Directory --no-fund --no-audit --silent "axe-core@$axeVersion" "puppeteer-core@$puppeteerVersion" | Out-Host
    $installedAxe = Get-InstalledPackageVersion -PackageJson $axePackage
    if ($LASTEXITCODE -ne 0 -or $installedAxe -ne $axeVersion) {
        throw "Installing axe-core@$axeVersion failed (installed: $installedAxe)."
    }
    return $axeVersion
}

if (-not $PSBoundParameters.ContainsKey('Url')) {
    $entered = [System.Collections.Generic.List[string]]::new()
    $index = 0
    while ($true) {
        $line = Read-Host -Prompt "Url[$index]"
        if ([string]::IsNullOrWhiteSpace($line)) { break }
        $entered.Add($line.Trim())
        $index++
    }
    $Url = $entered.ToArray()
}

$targets = foreach ($entry in @($Url)) {
    if ([string]::IsNullOrWhiteSpace($entry)) { continue }
    foreach ($part in ($entry -split ',')) {
        $uri = ConvertTo-PageUri $part
        if ($uri) { $uri }
    }
}
if (-not $targets) {
    throw 'Keine Seitenadresse. Beispiel: -Url https://<forms-host>/,https://<web-host>/'
}

$repoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))
$installDir = $PSScriptRoot
$axeVersion = Install-AxeCore -Directory $installDir
$chrome = Find-Chrome -Explicit $ChromePath
if (-not $chrome) {
    throw 'No Chrome or Chromium found. Pass the path with -ChromePath.'
}
$runner = Join-Path $installDir 'run-axe.mjs'
$env:NODE_PATH = Join-Path $installDir 'node_modules'

if (-not $OutFile) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')
    $directory = Join-Path $repoRoot 'artifacts/wcag-abfrage'
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
    $OutFile = Join-Path $directory "wcag-$stamp.txt"
}
else {
    $parent = Split-Path -Parent $OutFile
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
}

function Format-AxeSelector {
    param($Value)
    if ($null -eq $Value) { return '' }
    if ($Value -is [string]) { return $Value.Trim() }
    $parts = foreach ($entry in @($Value)) {
        if ($null -eq $entry) { continue }
        if ($entry -is [string]) { $entry.Trim() }
        else { ((@($entry) | ForEach-Object { ([string]$_).Trim() }) -join ' / ') }
    }
    return (($parts | Where-Object { $_ }) -join ' >> ')
}

function Limit-SingleLine {
    param([string]$Text, [int]$Max = 240)
    if ([string]::IsNullOrWhiteSpace($Text)) { return '' }
    $flat = ($Text -replace '\s+', ' ').Trim()
    if ($flat.Length -gt $Max) { return $flat.Substring(0, $Max) + '...' }
    return $flat
}

function Add-AxeFinding {
    param($Builder, $Item)
    $tagList = (@($Item.tags) -join ', ')
    $nodes = @($Item.nodes)
    [void]$Builder.AppendLine("- [$($Item.impact)] $($Item.help) ($($Item.id)), $($nodes.Count) Stellen")
    if ($tagList) { [void]$Builder.AppendLine("  Tags: $tagList") }
    if ($Item.helpUrl) { [void]$Builder.AppendLine("  $($Item.helpUrl)") }
    foreach ($node in $nodes) {
        if ($null -eq $node) { continue }
        $selector = Format-AxeSelector $node.target
        $html = Limit-SingleLine $node.html
        if ($selector) { [void]$Builder.AppendLine("  Selektor: $selector") }
        if ($html) { [void]$Builder.AppendLine("  HTML: $html") }
        $wording = [string]$node.failureSummary
        if (-not [string]::IsNullOrWhiteSpace($wording)) {
            $first = $true
            foreach ($line in ($wording -split '\r?\n')) {
                $trimmed = $line.Trim()
                if (-not $trimmed) { continue }
                if ($first) {
                    [void]$Builder.AppendLine("  Wortlaut des Werkzeugs: $trimmed")
                    $first = $false
                }
                else {
                    [void]$Builder.AppendLine("  $trimmed")
                }
            }
        }
    }
}

$builder = [System.Text.StringBuilder]::new()
[void]$builder.AppendLine('WCAG-Werkzeugbericht (axe-core)')
[void]$builder.AppendLine("Erzeugt (UTC): $([DateTime]::UtcNow.ToString('yyyy-MM-dd HH:mm:ss'))")
[void]$builder.AppendLine('Skript: src/tools/wcag/Invoke-WcagAbfrage.ps1')
[void]$builder.AppendLine("axe-core: $axeVersion")
[void]$builder.AppendLine("Tags: $Tags")
[void]$builder.AppendLine("Browser: $chrome")
[void]$builder.AppendLine('Hinweis: Das ist eine automatische Werkzeugauskunft. Sie ist kein WCAG-Konformitaetsnachweis und ersetzt keine Pruefung mit Tastatur und Screenreader.')
[void]$builder.AppendLine('Grenze des Laufs: Gewartet wird nur, bis das Dokument geladen ist. axe-core prueft nur das Hauptdokument, nicht Frames. Geschlossene Dialoge und verdeckte Bereiche werden nicht geoeffnet.')
[void]$builder.AppendLine('Fundstellen: Selektor, HTML-Ausschnitt und Wortlaut stammen von axe-core. Sie sind keine Bewertung der Arbeit.')
[void]$builder.AppendLine()

foreach ($target in $targets) {
    [void]$builder.AppendLine("==== Seite: $($target.AbsoluteUri)")
    $jsonText = & node $runner $target.AbsoluteUri $chrome $Tags ([string]($TimeoutSec * 1000)) 2>&1
    $exit = $LASTEXITCODE
    $raw = ($jsonText | Out-String).Trim()
    $parsed = $null
    try { $parsed = $raw | ConvertFrom-Json } catch { $parsed = $null }
    if ($exit -ne 0 -or -not $parsed -or -not $parsed.testEngine) {
        [void]$builder.AppendLine("Ergebnis: axe-core ist fehlgeschlagen (Exit $exit).")
        $excerpt = $raw
        if ($excerpt.Length -gt 800) { $excerpt = $excerpt.Substring(0, 800) + '...' }
        if ($excerpt) { [void]$builder.AppendLine($excerpt) }
        [void]$builder.AppendLine()
        continue
    }
    $violations = @($parsed.violations)
    $incomplete = @($parsed.incomplete)
    $passes = @($parsed.passes)
    [void]$builder.AppendLine("Werkzeug: axe-core $($parsed.testEngine.version)")
    [void]$builder.AppendLine("Seite nach Laden: $($parsed.url)")
    [void]$builder.AppendLine("Verstoesse: $($violations.Count). Unvollstaendig, manuell pruefen: $($incomplete.Count). Bestanden: $($passes.Count).")
    if ($violations.Count -eq 0) {
        [void]$builder.AppendLine('Keine automatischen Verstoesse fuer die gewaehlten Tags.')
    }
    foreach ($item in ($violations | Sort-Object impact, id)) {
        Add-AxeFinding -Builder $builder -Item $item
    }
    if ($incomplete.Count -gt 0) {
        [void]$builder.AppendLine('Unvollstaendig:')
        foreach ($item in ($incomplete | Sort-Object impact, id)) {
            Add-AxeFinding -Builder $builder -Item $item
        }
    }
    [void]$builder.AppendLine()
}

[System.IO.File]::WriteAllText($OutFile, $builder.ToString(), [Text.UTF8Encoding]::new($false))
Write-Output $OutFile
