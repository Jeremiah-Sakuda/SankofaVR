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
still figure-study-standing figure-standing
still figure-study-kneeling figure-kneeling
still figure-study-sharing-03 figure-sharing
still 14-seed figures-after
# Optional baseline copied before changing the scene. Both panels use the same journey framing.
baseline=artifacts/polish-round12-baseline/14-seed.png
if [[ -f "$baseline" ]]; then
  ffmpeg -loglevel error -y -i "$baseline" -vf "scale=1280:-2" -q:v 4 "$out/figures-before.jpg"
  ffmpeg -loglevel error -y -i "$out/figures-before.jpg" -i "$out/figures-after.jpg" -filter_complex "[0:v][1:v]hstack=inputs=2" -q:v 4 "$out/figures-comparison.jpg"
fi
# The entry through the flame as one strip of frames.
ffmpeg -loglevel error -y -framerate 1 -i "$captures/m0-entering-%02d.png" -vf "scale=420:-2,tile=3x3:padding=3" -frames:v 1 -q:v 4 "$out/entry-strip.jpg"
# What the visitor lingered on, rising from the fire at home.
ffmpeg -loglevel error -y -i "$captures/m6-echoes-03.png" -vf "scale=1280:-2" -q:v 4 "$out/echoes.jpg"
ls -la $out
