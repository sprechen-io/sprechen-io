#!/usr/bin/env bash
# Publishes the microphone harness against the local MicroSharp feed and runs it
# in headless Chromium with a synthetic capture file. Pass the feed directory and
# an empty work directory. PLAYWRIGHT_BROWSERS_PATH must already point at Chromium.
# The harness project keeps the prebuilt WebAssembly runtime. The test compares
# the served capture module with the hash read from the package. It uses the
# synthetic file only: no physical microphone and no speech service.
set -euo pipefail

if [[ $# -ne 2 || -z "${1:-}" || -z "${2:-}" ]]; then
  echo "usage: scripts/run-microphone-browser-harness.sh <feed-directory> <work-directory>" >&2
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

bash "$ROOT/scripts/restore-microsharp-local-feed.sh" "$SOURCE"

dotnet publish \
  "$ROOT/src/speech/Sprechen.Speech.MicrophoneBrowserHarness/Sprechen.Speech.MicrophoneBrowserHarness.csproj" \
  --configuration Release \
  --output "$WORK/publish" \
  --configfile "$ROOT/nuget.config" \
  --nologo

if [[ ! -f "$HARNESS_PUBLISH_DIR/_content/MicroSharp.Browser/microsharp-capture.js" ]]; then
  echo "published capture module is missing." >&2
  exit 1
fi

dotnet test \
  "$ROOT/src/speech/Sprechen.Speech.MicrophoneBrowserHarness.Tests/Sprechen.Speech.MicrophoneBrowserHarness.Tests.csproj" \
  --configuration Release \
  --nologo \
  --filter FullyQualifiedName~CaptureHarnessBrowserTests

echo "harness-js-sha256=$MIC_CAPTURE_JS_SHA256"
