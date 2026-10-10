#!/bin/zsh
# Copies a few desktop review captures into docs/media as small JPEGs, so the README can show the current build.
# Run after a capture pass (SANKOFA_CAPTURE=1, see unity/README.md). Desktop renders only, not headset footage.
set -e
cd "$(dirname "$0")/.."
captures=artifacts/captures
out=docs/media
mkdir -p $out
still() { ffmpeg -loglevel error -y -i "$captures/$1.png" -vf "scale=1280:-2" -q:v 4 "$out/$2.jpg"; }
still 01-welcome hearth
still 05-water dusk-1850s
still 08-bead bead-offered
still 13-granddaughters railway-1899
still 10-today river-today
still 09-keepsake keepsake
# The entry through the flame as one strip of frames.
ffmpeg -loglevel error -y -framerate 1 -i "$captures/m0-entering-%02d.png" -vf "scale=320:-2,tile=9x1:padding=2" -frames:v 1 -q:v 4 "$out/entry-strip.jpg"
ls -la $out
