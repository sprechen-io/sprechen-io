#!/usr/bin/env bash
# Publishes the microphone harness against MicroSharp packages and runs it
# in headless Chromium with a synthetic capture file. Pass the reference package directory and
# an empty work directory. PLAYWRIGHT_BROWSERS_PATH must already point at Chromium.
# The harness project keeps the prebuilt WebAssembly runtime. The test compares
# the served capture module with the hash read from the package. It uses the
# synthetic file only: no physical microphone and no speech service.
set -euo pipefail

if [[ $# -lt 2 || $# -gt 3 || -z "${1:-}" || -z "${2:-}" || ( $# -eq 3 && "$3" != "--nuget-org" ) ]]; then
  echo "usage: scripts/run-microphone-browser-harness.sh <reference-package-directory> <work-directory> [--nuget-org]" >&2
  exit 2
fi

if [[ -z "${PLAYWRIGHT_BROWSERS_PATH:-}" ]]; then
  echo "PLAYWRIGHT_BROWSERS_PATH is required." >&2
  exit 2
fi

SOURCE=$1
WORK=$2
if [[ ! -d "$SOURCE" ]]; then
  echo "feed directory is missing: $SOURCE" >&2
  exit 2
fi

ROOT=$(cd "$(dirname "$0")/.." && pwd)
mkdir -p "$WORK/nuget-packages" "$WORK/publish" "$WORK/wav"
export NUGET_PACKAGES="$WORK/nuget-packages"
export HARNESS_PUBLISH_DIR="$WORK/publish/wwwroot"
export HARNESS_WAV_DIR="$WORK/wav"
export MIC_CAPTURE_JS_SHA256
MIC_CAPTURE_JS_SHA256=$(unzip -p "$SOURCE/MicroSharp.Browser.0.1.0.nupkg" staticwebassets/microsharp-capture.js | sha256sum | awk '{print $1}')
if [[ ! "$MIC_CAPTURE_JS_SHA256" =~ ^[0-9a-f]{64}$ ]]; then
  echo "capture module hash was not read from the package." >&2
  exit 1
fi

config="$ROOT/nuget.config"
if [[ "${3:-}" != "--nuget-org" ]]; then
  bash "$ROOT/scripts/restore-microsharp-local-feed.sh" "$SOURCE"
  config="$ROOT/nuget.microsharp-local.config"
fi

dotnet publish \
  "$ROOT/src/speech/Sprechen.Speech.MicrophoneBrowserHarness/Sprechen.Speech.MicrophoneBrowserHarness.csproj" \
  --configuration Release \
  --output "$WORK/publish" \
  --configfile "$config" \
  --nologo

if [[ ! -f "$HARNESS_PUBLISH_DIR/_content/MicroSharp.Browser/microsharp-capture.js" ]]; then
  echo "published capture module is missing." >&2
  exit 1
fi

tests="$ROOT/src/speech/Sprechen.Speech.MicrophoneBrowserHarness.Tests/Sprechen.Speech.MicrophoneBrowserHarness.Tests.csproj"
dotnet restore "$tests" --configfile "$config" --nologo
dotnet test "$tests" --no-restore \
  --configuration Release \
  --nologo \
  --filter FullyQualifiedName~CaptureHarnessBrowserTests

echo "harness-js-sha256=$MIC_CAPTURE_JS_SHA256"
