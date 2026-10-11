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
# A fixed-step motion study after the complete actor pose; review cadence is not a device frame-rate claim.
if [[ -f "$captures/performance-0263.png" ]]; then
  ffmpeg -loglevel error -y -framerate 12 -i "$captures/performance-%04d.png" -frames:v 264 -c:v libx264 -crf 21 -pix_fmt yuv420p -movflags +faststart "$out/exchange-performance.mp4"
  still performance-0090 exchange-performance
fi
still 14-seed figures-after
# Optional baseline copied before changing the scene. Both panels use the same journey framing.
baseline=artifacts/polish-round12-baseline/14-seed.png
if [[ -f "$baseline" ]]; then
  ffmpeg -loglevel error -y -i "$baseline" -vf "scale=1280:-2" -q:v 4 "$out/figures-before.jpg"
  ffmpeg -loglevel error -y -i "$out/figures-before.jpg" -i "$out/figures-after.jpg" -filter_complex "[0:v][1:v]hstack=inputs=2" -q:v 4 "$out/figures-comparison.jpg"
fi
# Environment pass: preserve the same seated journey view from the previous source revision.
for pair in "01-welcome:hearth" "05-water:stream"; do
  frame=${pair%%:*}
  label=${pair##*:}
  baseline=artifacts/polish-round13-baseline/$frame.png
  if [[ -f "$baseline" ]]; then
    ffmpeg -loglevel error -y -i "$baseline" -i "$captures/$frame.png" -filter_complex "[0:v]scale=1280:-2[a];[1:v]scale=1280:-2[b];[a][b]hstack=inputs=2" -q:v 4 "$out/environment-$label-comparison.jpg"
  fi
done
still environment-story.water.1899-left camp-stores
# The entry through the flame as one strip of frames.
ffmpeg -loglevel error -y -framerate 1 -i "$captures/m0-entering-%02d.png" -vf "scale=420:-2,tile=3x3:padding=3" -frames:v 1 -q:v 4 "$out/entry-strip.jpg"
# What the visitor lingered on, rising from the fire at home.
ffmpeg -loglevel error -y -i "$captures/m6-echoes-03.png" -vf "scale=1280:-2" -q:v 4 "$out/echoes.jpg"
ls -la $out
