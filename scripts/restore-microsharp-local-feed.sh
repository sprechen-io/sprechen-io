#!/usr/bin/env bash
# Copies a locally packed MicroSharp feed into the repo-relative local-feed directory.
# Pass the feed directory as the only argument. No home directory is embedded here.
# Set NUGET_PACKAGES to a fresh empty directory before restoring, so an older 0.1.0
# cache cannot satisfy MicroSharp. Source mapping then reads only this local feed.
set -euo pipefail

if [[ $# -ne 1 || -z "${1:-}" ]]; then
  echo "usage: scripts/restore-microsharp-local-feed.sh <feed-directory>" >&2
  exit 2
fi

SOURCE=$1
if [[ ! -d "$SOURCE" ]]; then
  echo "feed directory is missing: $SOURCE" >&2
  exit 2
fi

ROOT=$(cd "$(dirname "$0")/.." && pwd)
DEST="$ROOT/local-feed"
mkdir -p "$DEST"
find "$DEST" -maxdepth 1 -type f \( -name 'MicroSharp*.nupkg' -o -name 'MicroSharp*.snupkg' \) -delete
cp -f "$SOURCE"/MicroSharp*.nupkg "$SOURCE"/MicroSharp*.snupkg "$DEST"/

if [[ -n "${NUGET_PACKAGES:-}" ]]; then
  mkdir -p "$NUGET_PACKAGES"
  dotnet restore "$ROOT/src/speech/Sprechen.Speech.App.MicrophoneRecording/Sprechen.Speech.App.MicrophoneRecording.csproj" \
    --configfile "$ROOT/nuget.config" \
    --nologo
fi
